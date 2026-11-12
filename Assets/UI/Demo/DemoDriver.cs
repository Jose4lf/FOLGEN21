using UnityEngine;
using Folgen21.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Folgen21.Demo
{
    /// <summary>
    /// Driver de demostracion: crea camara, luz, suelo y objetivo, y anima el HUD
    /// con datos falsos para que puedas probar todo el paquete sin tu juego.
    /// Controles de la demo:
    ///   - Clic izquierdo mantenido: dispara (reduce municion)
    ///   - R: recarga
    ///   - Q / E / R: activan el enfriamiento visual de cada skill
    ///   - ESC: pausa (gestionado por UIManager)
    /// La mision cambia sola de MOVIMIENTO a DISPARO a los ~9 segundos.
    /// </summary>
    public class DemoDriver : MonoBehaviour
    {
        public GameplayHUDController hud;

        private Transform _objetivo;
        private Camera _camaraPrincipal;
        private RenderTexture _texturaMinimapa;

        private float _cronometro;
        private float _vida = 1f, _energia = 1f;
        private int _municionActual = 12, _municionReserva = 48;
        private const int TamanoCargador = 12;
        private bool _modoDisparo;
        private float _cadenciaDisparo;
        private float _radioOrbita = 45f;
        private float _anguloOrbita;
        private readonly float[] _enfriamientos = new float[4];

        private void Start()
        {
            // --- camara principal orbitante ---
            var goCamara = new GameObject("DemoCamara");
            _camaraPrincipal = goCamara.AddComponent<Camera>();
            _camaraPrincipal.tag = "MainCamera";
            _camaraPrincipal.fieldOfView = 60f;
            _camaraPrincipal.backgroundColor = new Color(0.09f, 0.05f, 0.16f);
            _camaraPrincipal.clearFlags = CameraClearFlags.SolidColor;

            // --- suelo de referencia ---
            var suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            suelo.name = "SueloDemo";
            suelo.transform.localScale = new Vector3(4f, 1f, 4f);

            // --- objetivo de la mision ---
            _objetivo = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            _objetivo.name = "ObjetivoDemo";
            _objetivo.position = new Vector3(27f, 1.5f, 34f);

            // --- luz direccional ---
            var goLuz = new GameObject("LuzDemo");
            goLuz.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            var luz = goLuz.AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.intensity = 1.1f;

            // --- camara del minimapa (cenital, Norte = +Z) ---
            _texturaMinimapa = new RenderTexture(256, 256, 16);
            var goMinimapa = new GameObject("DemoCamaraMinimapa");
            var camMinimapa = goMinimapa.AddComponent<Camera>();
            camMinimapa.orthographic = true;
            camMinimapa.orthographicSize = 30f;
            camMinimapa.clearFlags = CameraClearFlags.SolidColor;
            camMinimapa.backgroundColor = new Color(0.08f, 0.05f, 0.15f);
            camMinimapa.transform.position = new Vector3(0f, 60f, 0f);
            camMinimapa.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            camMinimapa.targetTexture = _texturaMinimapa;

            // --- conectar el HUD ---
            if (hud != null)
            {
                hud.SetTexturaMinimapa(_texturaMinimapa);
                hud.SetJugador("JHOEL", 1);
                hud.SetVida(1f);
                hud.SetEnergia(1f);
                hud.SetMunicion(_municionActual, _municionReserva);
                hud.SetCantidadItem(3, 3);
                hud.SetMision("TUTORIAL: MOVIMIENTO", $"Dirígete al punto de control ({Mathf.RoundToInt(_radioOrbita)} m)");
                hud.MostrarTip("Usa el mapa para encontrar tu objetivo.");
            }

            // hay "partida guardada" en la demo para activar CONTINUAR
            MainMenuController.MarcarGuardadoExistente(true);
        }

        private void Update()
        {
            if (hud == null) return;
            float dt = Time.unscaledDeltaTime;
            _cronometro += dt;

            // --- camara: orbita el origen y se acerca al objetivo durante el tutorial ---
            _anguloOrbita += dt * 9f;
            if (!_modoDisparo) _radioOrbita = Mathf.Lerp(45f, 14f, Mathf.Clamp01(_cronometro / 9f));
            Vector3 posCamara = new Vector3(Mathf.Cos(_anguloOrbita * Mathf.Deg2Rad) * _radioOrbita, 3.5f, Mathf.Sin(_anguloOrbita * Mathf.Deg2Rad) * _radioOrbita);
            _camaraPrincipal.transform.position = posCamara;
            _camaraPrincipal.transform.LookAt(_objetivo.position + Vector3.up * 1.5f);

            // --- vida y energia oscilando ---
            _vida = Mathf.Clamp01(0.62f + 0.38f * Mathf.Sin(_cronometro * 0.9f) - (PresionoTecla(KeyCode.H) ? 0.05f : 0f));
            _energia = Mathf.Clamp01(0.5f + 0.5f * Mathf.Sin(_cronometro * 1.7f + 1f));
            hud.SetVida(_vida);
            hud.SetEnergia(_energia);

            // --- transicion de tutorial a los 9 segundos ---
            if (!_modoDisparo && _cronometro > 9f)
            {
                _modoDisparo = true;
                hud.SetModoDisparo(true);
                hud.SetMision("TUTORIAL: DISPARO", "Mantén clic izquierdo para disparar");
                hud.MostrarTip("Mantén presionado el botón de disparo para atacar.");
            }

            // --- disparo simulado ---
            _cadenciaDisparo -= dt;
            if (_modoDisparo && BotonDisparoPresionado() && _cadenciaDisparo <= 0f && _municionActual > 0)
            {
                _municionActual--;
                _cadenciaDisparo = 0.18f;
                hud.SetMunicion(_municionActual, _municionReserva);
            }

            // --- recarga ---
            if (PresionoTecla(KeyCode.R) && _municionActual < TamanoCargador && _municionReserva > 0)
            {
                int falta = TamanoCargador - _municionActual;
                int tomada = Mathf.Min(falta, _municionReserva);
                _municionActual += tomada;
                _municionReserva -= tomada;
                hud.SetMunicion(_municionActual, _municionReserva);
            }

            // --- enfriamientos de skills con Q / E / R ---
            if (PresionoTecla(KeyCode.Q)) _enfriamientos[0] = 1f;
            if (PresionoTecla(KeyCode.E)) _enfriamientos[1] = 1f;
            if (PresionoTecla(KeyCode.R)) _enfriamientos[2] = 0.5f;
            for (int i = 0; i < 4; i++)
            {
                if (_enfriamientos[i] > 0f)
                {
                    _enfriamientos[i] = Mathf.Max(0f, _enfriamientos[i] - dt / 3f);
                    hud.SetEnfriamiento(i, _enfriamientos[i]);
                }
            }

            // --- minimapa y marcador de mundo ---
            float rotacionHaciaObjetivo = Mathf.Atan2(_objetivo.position.x, _objetivo.position.z) * Mathf.Rad2Deg;
            hud.SetRotacionFlecha(rotacionHaciaObjetivo);
            hud.SetMarcadorMinimapa(hud.CalcularOffsetMinimapa(Vector3.zero, _objetivo.position, 100f / 30f, 0f));

            // la distancia de la mision se actualiza en el texto del objetivo
            if (!_modoDisparo)
            {
                float distancia = Vector3.Distance(Vector3.zero, _objetivo.position);
                hud.SetMision("TUTORIAL: MOVIMIENTO", $"Dirígete al punto de control ({Mathf.RoundToInt(distancia)} m)");
            }
        }

        // ---------- entrada dual (New Input System o Legacy) ----------

        private static bool BotonDisparoPresionado()
        {
#if ENABLE_INPUT_SYSTEM
            var raton = Mouse.current;
            if (raton != null && raton.leftButton.isPressed) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButton(0)) return true;
#endif
            return false;
        }

        private static bool PresionoTecla(KeyCode tecla)
        {
#if ENABLE_INPUT_SYSTEM
            var teclado = Keyboard.current;
            if (teclado != null)
            {
                switch (tecla)
                {
                    case KeyCode.Q: return teclado.qKey.wasPressedThisFrame;
                    case KeyCode.E: return teclado.eKey.wasPressedThisFrame;
                    case KeyCode.R: return teclado.rKey.wasPressedThisFrame;
                    case KeyCode.H: return teclado.hKey.wasPressedThisFrame;
                }
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(tecla);
#endif
#pragma warning disable CS0162
            return false;
#pragma warning restore CS0162
        }
    }
}
