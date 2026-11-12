using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Folgen21.UI
{
    /// <summary>
    /// Controlador del menu de ajustes: musica, efectos, sensibilidad,
    /// calidad grafica, resolucion y pantalla completa.
    /// Guarda todo en PlayerPrefs bajo claves "folgen_*" y aplica al iniciar la app.
    /// </summary>
    public class SettingsMenuController : UIScreen
    {
        public event Action Cerrado;

        // Claves de guardado
        public const string KeyMusica = "folgen_musica";
        public const string KeyEfectos = "folgen_efectos";
        public const string KeySensibilidad = "folgen_sensibilidad";
        public const string KeyCalidad = "folgen_calidad";
        public const string KeyResolucion = "folgen_resolucion";
        public const string KeyPantalla = "folgen_pantalla_completa";

        private Slider _sMusica, _sEfectos, _sSensibilidad;
        private DropdownField _dCalidad, _dResolucion;
        private Toggle _tPantallaCompleta;

        private int[] _resAncho, _resAlto;

        private void Start() => AplicarGuardadoAlIniciar();

        protected override void OnRaizPreparada(VisualElement raiz)
        {
            _sMusica = Q<Slider>("sMusica");
            _sEfectos = Q<Slider>("sEfectos");
            _sSensibilidad = Q<Slider>("sSensibilidad");
            _dCalidad = Q<DropdownField>("dCalidad");
            _dResolucion = Q<DropdownField>("dResolucion");
            _tPantallaCompleta = Q<Toggle>("tPantallaCompleta");

            // Opciones de calidad
            _dCalidad.choices = new System.Collections.Generic.List<string>(QualitySettings.names);
            _dCalidad.index = QualitySettings.GetQualityLevel();

            // Resoluciones disponibles
            var opciones = new System.Collections.Generic.List<string>();
            var anchos = new System.Collections.Generic.List<int>();
            var altos = new System.Collections.Generic.List<int>();

            Resolution[] resoluciones = Screen.resolutions;

            foreach (var r in resoluciones)
            {
                string etiqueta = $"{r.width} x {r.height}";

                if (!opciones.Contains(etiqueta))
                {
                    opciones.Add(etiqueta);
                    anchos.Add(r.width);
                    altos.Add(r.height);
                }
            }

            // Fallback si Unity no devuelve resoluciones
            if (opciones.Count == 0)
            {
                opciones.Add($"{Screen.width} x {Screen.height}");
                anchos.Add(Screen.width);
                altos.Add(Screen.height);
            }

            _dResolucion.choices = opciones;
            _resAncho = anchos.ToArray();
            _resAlto = altos.ToArray();

            // Seleccionar resolución actual
            int indiceActual = opciones.IndexOf(
                $"{Screen.width} x {Screen.height}"
            );

            _dResolucion.index = Mathf.Max(0, indiceActual);

            // Valores guardados
            _sMusica.value = PlayerPrefs.GetFloat(KeyMusica, 0.8f);
            _sEfectos.value = PlayerPrefs.GetFloat(KeyEfectos, 0.8f);
            _sSensibilidad.value = PlayerPrefs.GetFloat(KeySensibilidad, 1f);
            _tPantallaCompleta.value = Screen.fullScreenMode != FullScreenMode.Windowed;

            Q<Button>("btnRestaurar").clicked += RestaurarValores;
            Q<Button>("btnAplicar").clicked += AplicarYGuardar;
            Q<Button>("btnVolver").clicked += () => Cerrado?.Invoke();
        }

        private void RestaurarValores()
        {
            _sMusica.value = 0.8f;
            _sEfectos.value = 0.8f;
            _sSensibilidad.value = 1f;
            _dCalidad.index = Mathf.Max(0, QualitySettings.names.Length - 1);
            _tPantallaCompleta.value = true;
        }

        private void AplicarYGuardar()
        {
            PlayerPrefs.SetFloat(KeyMusica, _sMusica.value);
            PlayerPrefs.SetFloat(KeyEfectos, _sEfectos.value);
            PlayerPrefs.SetFloat(KeySensibilidad, _sSensibilidad.value);
            PlayerPrefs.SetInt(KeyCalidad, _dCalidad.index);

            bool pantallaCompleta = _tPantallaCompleta.value;
            PlayerPrefs.SetInt(KeyPantalla, pantallaCompleta ? 1 : 0);

            // Calidad grafica
            QualitySettings.SetQualityLevel(_dCalidad.index, true);

            // Resolucion y modo de pantalla
            if (_resAncho == null || _resAlto == null ||_resAncho.Length == 0 || _resAlto.Length == 0)
            {
                Debug.LogWarning("[Folgen21] No hay resoluciones disponibles.");
                return;
            }

            int i = Mathf.Clamp(_dResolucion.index, 0, _resAncho.Length - 1);
            var modo = pantallaCompleta
                ? (Screen.currentResolution.width == _resAncho[i] && Screen.currentResolution.height == _resAlto[i]
                    ? FullScreenMode.FullScreenWindow
                    : FullScreenMode.ExclusiveFullScreen)
                : FullScreenMode.Windowed;
            Screen.SetResolution(_resAncho[i], _resAlto[i], modo);

            // En la demo (sin AudioMixer) el volumen general sigue la musica;
            // en tu juego real conecta estos valores a tu AudioMixer.
            AudioListener.volume = _sMusica.value * 0.5f + _sEfectos.value * 0.5f;

            PlayerPrefs.Save();
        }

        // ---------------- Acceso estatico para tu gameplay ----------------

        /// <summary>Sensibilidad del raton guardada (multiplicador, 0.1 - 3).</summary>
        public static float SensibilidadActual => PlayerPrefs.GetFloat(KeySensibilidad, 1f);

        /// <summary>Volumen de musica guardado (0 - 1).</summary>
        public static float VolumenMusica => PlayerPrefs.GetFloat(KeyMusica, 0.8f);

        /// <summary>Volumen de efectos guardado (0 - 1).</summary>
        public static float VolumenEfectos => PlayerPrefs.GetFloat(KeyEfectos, 0.8f);

        /// <summary>Aplica los ajustes guardados al arrancar la aplicacion.</summary>
        public static void AplicarGuardadoAlIniciar()
        {
            int calidad = PlayerPrefs.GetInt(KeyCalidad, -1);
            if (calidad >= 0 && calidad < QualitySettings.names.Length)
                QualitySettings.SetQualityLevel(calidad, true);

            bool pantalla = PlayerPrefs.GetInt(KeyPantalla, 1) == 1;
            Screen.fullScreenMode = pantalla ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        }
    }
}
