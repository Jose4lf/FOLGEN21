using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Folgen21.UI
{
    /// <summary>
    /// Controlador del HUD de juego. Todo el contenido se actualiza por API publica,
    /// no depende de tu gameplay: llamalo desde tus sistemas de vida, arma, misiones, etc.
    ///
    ///     hud.SetVida(0.75f);
    ///     hud.SetMunicion(12, 48);
    ///     hud.SetMision("TUTORIAL: DISPARO", "Mantén clic izquierdo para disparar");
    /// </summary>
    public class GameplayHUDController : UIScreen
    {
        [Header("Marcador de objetivo en el mundo (opcional)")]
        [Tooltip("Transform del objetivo; si esta asignado, el diamante dorado se posiciona automaticamente sobre el.")]
        public Transform objetivoMundo;

        [Tooltip("Altura (en metros) a la que flota el marcador sobre el objetivo.")]
        public float alturaMarcador = 3.5f;

        // --- Referencias de UI ---
        private Label _lblNombre, _lblNivel;
        private VisualElement _fillVida, _fillEnergia;
        private Label _lblMisionTitulo, _lblMisionObjetivo;
        private VisualElement _grupoMovimiento, _grupoDisparo;
        private VisualElement _cajaTip;
        private Label _lblTip;
        private VisualElement _mira;
        private Image _imgMinimapa;
        private VisualElement _flechaJugador, _marcadorMinimapa;
        private VisualElement _marcadorMundo;
        private Label _lblDistancia;
        private VisualElement[] _skillEnf;
        private VisualElement[] _skillIconos;
        private Label[] _skillCantidades;
        private Label _lblMunicionActual, _lblMunicionReserva;

        private IVisualElementScheduledItem _tipOcultador;
        private Texture _texturaMinimapa;
        private Camera _camaraPrincipal;

        protected override void OnRaizPreparada(VisualElement raiz)
        {
            // El HUD no debe interceptar ningun clic: todo en Ignore.
            raiz.Query<VisualElement>().ForEach(e => e.pickingMode = PickingMode.Ignore);

            _lblNombre = Q<Label>("lblNombre");
            _lblNivel = Q<Label>("lblNivel");
            _fillVida = Q<VisualElement>("fillVida");
            _fillEnergia = Q<VisualElement>("fillEnergia");
            _lblMisionTitulo = Q<Label>("lblMisionTitulo");
            _lblMisionObjetivo = Q<Label>("lblMisionObjetivo");
            _grupoMovimiento = Q<VisualElement>("grupoMovimiento");
            _grupoDisparo = Q<VisualElement>("grupoDisparo");
            _cajaTip = Q<VisualElement>("cajaTip");
            _lblTip = Q<Label>("lblTip");
            _mira = Q<VisualElement>("mira");
            _imgMinimapa = Q<Image>("imgMinimapa");
            _flechaJugador = Q<VisualElement>("flechaJugador");
            _marcadorMinimapa = Q<VisualElement>("marcadorMinimapa");
            _marcadorMundo = Q<VisualElement>("marcadorMundo");
            _lblDistancia = Q<Label>("lblDistancia");
            _lblMunicionActual = Q<Label>("lblMunicionActual");
            _lblMunicionReserva = Q<Label>("lblMunicionReserva");

            _skillEnf = new VisualElement[4];
            _skillIconos = new VisualElement[4];
            _skillCantidades = new Label[4];
            for (int i = 0; i < 4; i++)
            {
                _skillEnf[i] = Q<VisualElement>($"skill{i}Enf");
                _skillIconos[i] = Q<VisualElement>($"skill{i}Icono");
                _skillCantidades[i] = Q<Label>($"skill{i}Cant");
            }
        }

        private void Update()
        {
            if (objetivoMundo != null && _marcadorMundo != null)
                ActualizarMarcadorMundo();
        }

        // ================= API PUBLICA =================

        /// <summary>Nombre y nivel del jugador.</summary>
        public void SetJugador(string nombre, int nivel)
        {
            _lblNombre.text = nombre;
            _lblNivel.text = nivel.ToString();
        }

        /// <summary>Vida en rango 0..1.</summary>
        public void SetVida(float fraccion)
        {
            _fillVida.style.width = Length.Percent(Mathf.Clamp01(fraccion) * 100f);
        }

        /// <summary>Energia en rango 0..1.</summary>
        public void SetEnergia(float fraccion)
        {
            _fillEnergia.style.width = Length.Percent(Mathf.Clamp01(fraccion) * 100f);
        }

        /// <summary>Municion del cargador y reserva.</summary>
        public void SetMunicion(int actual, int reserva)
        {
            _lblMunicionActual.text = actual.ToString();
            _lblMunicionReserva.text = $"/ {reserva}";
        }

        /// <summary>Texto de la mision mostrada bajo el retrato.</summary>
        public void SetMision(string titulo, string objetivo)
        {
            _lblMisionTitulo.text = titulo;
            _lblMisionObjetivo.text = objetivo;
        }

        /// <summary>Cambia los consejos de control: false = movimiento (WASD), true = disparo (LMB/R).</summary>
        public void SetModoDisparo(bool modoDisparo)
        {
            _grupoMovimiento.style.display = modoDisparo ? DisplayStyle.None : DisplayStyle.Flex;
            _grupoDisparo.style.display = modoDisparo ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>Muestra un consejo temporal en la caja derecha.</summary>
        public void MostrarTip(string texto, float segundos = 4f)
        {
            _lblTip.text = texto;
            _cajaTip.style.opacity = 1f;
            _tipOcultador?.Pause();
            _tipOcultador = _cajaTip.schedule.Execute(() => _cajaTip.style.opacity = 0f)
                .StartingIn(Mathf.RoundToInt(Mathf.Max(0.1f, segundos) * 1000));
        }

        /// <summary>Textura del minimapa (RenderTexture de tu camara de minimapa).</summary>
        public void SetTexturaMinimapa(Texture textura)
        {
            _texturaMinimapa = textura;
            _imgMinimapa.image = textura;
        }

        /// <summary>
        /// Posicion del marcador dorado sobre el minimapa, en pixeles respecto al centro
        /// (positivo X derecha, positivo Y abajo). Se recorta automaticamente al borde del circulo.
        /// </summary>
        public void SetMarcadorMinimapa(Vector2 offsetRespectoAlCentro)
        {
            float radio = 88f;
            Vector2 v = offsetRespectoAlCentro;
            float modulo = v.magnitude;
            if (modulo > radio) v = v / modulo * radio;
            _marcadorMinimapa.style.translate = new Translate(v.x, v.y, 0f);
        }

        /// <summary>Rotacion de la flecha del jugador en el minimapa (grados, 0 = arriba/Norte).</summary>
        public void SetRotacionFlecha(float grados)
        {
            _flechaJugador.style.rotate = new Rotate(new Angle(grados));
        }

        /// <summary>Muestra u oculta la mira central.</summary>
        public void SetMiraVisible(bool visible)
        {
            _mira.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>Cambia el icono de un slot de habilidad (0=Q, 1=E, 2=R, 3=I).</summary>

        public void SetIconoSkill(int indice, Texture textura)
        {
            if (indice < 0 || indice > 3) return;

            if (textura == null)
            {
                _skillIconos[indice].style.backgroundImage =
                    StyleKeyword.None;
                return;
            }

            if (textura is Texture2D textura2D)
            {
                _skillIconos[indice].style.backgroundImage =
                    new StyleBackground(Background.FromTexture2D(textura2D));
            }
            else if (textura is RenderTexture renderTexture)
            {
                _skillIconos[indice].style.backgroundImage =
                    new StyleBackground(Background.FromRenderTexture(renderTexture));
            }
            else
            {
                Debug.LogWarning(
                    "Tipo de textura no compatible para el icono de habilidad."
                );
            }
        }
        /// <summary>Enfriamiento visual de un slot (0 = listo, 1 = totalmente oscurecido).</summary>
        public void SetEnfriamiento(int indice, float fraccion)
        {
            if (indice < 0 || indice > 3) return;
            float f = Mathf.Clamp01(fraccion);
            _skillEnf[indice].style.height = Length.Percent(f * 54f); // 54% del marco = altura del icono (40px)
            _skillIconos[indice].style.opacity = 1f - 0.55f * f;
        }

        /// <summary>Cantidad visible en el slot de items (0 oculta el contador).</summary>
        public void SetCantidadItem(int indice, int cantidad)
        {
            if (indice < 0 || indice > 3) return;
            _skillCantidades[indice].text = cantidad > 0 ? cantidad.ToString() : string.Empty;
        }

        // ================= Marcador de mundo =================


        private void ActualizarMarcadorMundo()
        {
            if (_camaraPrincipal == null)
                _camaraPrincipal = Camera.main;

            if (_camaraPrincipal == null || Documento == null)
                return;

            VisualElement raiz = Documento.rootVisualElement;

            if (raiz == null || raiz.panel == null)
                return;

            Vector3 mundo = objetivoMundo.position +
                            Vector3.up * alturaMarcador;

            Vector3 pantalla =
                _camaraPrincipal.WorldToScreenPoint(mundo);

            if (pantalla.z <= 0f)
            {
                _marcadorMundo.style.display = DisplayStyle.None;
                return;
            }

            Vector2 posicionPanel =
                RuntimePanelUtils.ScreenToPanel(
                    raiz.panel,
                    new Vector2(pantalla.x, pantalla.y)
                );

            float anchoMarcador = 36f;

            _marcadorMundo.style.display = DisplayStyle.Flex;
            _marcadorMundo.style.left =
                posicionPanel.x - anchoMarcador * 0.5f;

            _marcadorMundo.style.top =
                posicionPanel.y - 62f;

            float distancia = Vector3.Distance(
                _camaraPrincipal.transform.position,
                objetivoMundo.position
            );

            _lblDistancia.text =
                $"{Mathf.RoundToInt(distancia)} m";
        }


        /// <summary>Distancia del marcador al objetivo en el minimapa (lo llama tu sistema o la demo).</summary>
        public Vector2 CalcularOffsetMinimapa(Vector3 posicionJugador, Vector3 posicionObjetivo, float escalaPixelesPorMetro = 0.9f, float rotacionJugadorGrados = 0f)
        {
            Vector3 delta = posicionObjetivo - posicionJugador;
            // gira el delta al espacio relativo de la flecha (Norte arriba = -Z hacia arriba)
            float rad = -rotacionJugadorGrados * Mathf.Deg2Rad;
            Vector2 plano = new Vector2(delta.x, delta.z);
            Vector2 rotado = new Vector2(
                plano.x * Mathf.Cos(rad) - plano.y * Mathf.Sin(rad),
                plano.x * Mathf.Sin(rad) + plano.y * Mathf.Cos(rad));
            // Y del minimapa crece hacia abajo => invertir Z
            return new Vector2(rotado.x * escalaPixelesPorMetro, -rotado.y * escalaPixelesPorMetro);
        }
    }
}
