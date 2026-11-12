using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Folgen21.UI
{
    /// <summary>
    /// Controlador de la pantalla de carga.
    /// - IniciarCargaEscena(nombre): carga una escena real mostrando el progreso real.
    /// - IniciarCargaSimulada(segundos): progreso falso (para demo o precarga propia).
    /// Muestra consejos rotativos mientras carga. Al terminar dispara CargaCompletada.
    /// </summary>
    public class LoadingScreenController : UIScreen
    {
        [Header("Consejos que rotan durante la carga")]
        [TextArea] public string[] consejos = new string[]
        {
            "Consejo: Explora el mundo para encontrar mejores recursos y desbloquear habilidades.",
            "Consejo: Usa el mapa para localizar tus objetivos más rápido.",
            "Consejo: Mejora tu brazo cibernético para desbloquear nuevas habilidades.",
            "Consejo: Guarda tu progreso con frecuencia en los puntos de control.",
        };

        [Tooltip("Segundos entre cambio de consejo.")]
        public float segundosEntreConsejos = 3.5f;

        /// <summary>Se dispara cuando la carga finaliza.</summary>
        public event Action CargaCompletada;

        private VisualElement _fill;
        private Label _lblPorcentaje, _lblConsejo;
        private bool _cargando;

        protected override void OnRaizPreparada(VisualElement raiz)
        {
            _fill = Q<VisualElement>("lsFill");
            _lblPorcentaje = Q<Label>("lblPorcentaje");
            _lblConsejo = Q<Label>("lblConsejo");
        }

        /// <summary>Carga una escena real por nombre mostrando el progreso.</summary>
        public void IniciarCargaEscena(string nombreEscena)
        {
            if (_cargando) return;
            _cargando = true;
            StartCoroutine(RutinaCargaEscena(nombreEscena));
        }

        /// <summary>Progreso simulado (demo). No cambia de escena.</summary>
        public void IniciarCargaSimulada(float segundos)
        {
            if (_cargando) return;
            _cargando = true;
            StartCoroutine(RutinaCargaSimulada(Mathf.Max(0.5f, segundos)));
        }

        private IEnumerator RutinaCargaEscena(string nombreEscena)
        {
            MostrarConsejoAleatorio();
            yield return new WaitForSeconds(0.35f);

            var rotador = StartCoroutine(RotarConsejos());
            AsyncOperation op = SceneManager.LoadSceneAsync(nombreEscena);
            if (op == null)
            {
                Debug.LogError($"[Folgen21] No existe la escena '{nombreEscena}'. Agregala a Build Settings.");
                _cargando = false;
                yield break;
            }

            op.allowSceneActivation = false;
            while (op.progress < 0.9f)
            {
                ActualizarBarra(Mathf.Lerp(0f, 0.9f, op.progress / 0.9f));
                yield return null;
            }

            ActualizarBarra(1f);
            yield return new WaitForSeconds(0.4f);
            StopCoroutine(rotador);
            op.allowSceneActivation = true;
            _cargando = false;
            CargaCompletada?.Invoke();
        }

        private IEnumerator RutinaCargaSimulada(float segundos)
        {
            MostrarConsejoAleatorio();
            yield return new WaitForSeconds(0.35f);

            var rotador = StartCoroutine(RotarConsejos());
            float t = 0f;
            while (t < segundos)
            {
                t += Time.unscaledDeltaTime;
                // curva con ligera aceleracion para que se sienta real
                float f = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / segundos));
                ActualizarBarra(f);
                yield return null;
            }

            StopCoroutine(rotador);
            _cargando = false;
            CargaCompletada?.Invoke();
        }

        private IEnumerator RotarConsejos()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(segundosEntreConsejos);
                MostrarConsejoAleatorio();
            }
        }

        private void MostrarConsejoAleatorio()
        {
            if (consejos == null || consejos.Length == 0 || _lblConsejo == null) return;
            _lblConsejo.text = consejos[UnityEngine.Random.Range(0, consejos.Length)];
        }

        private void ActualizarBarra(float fraccion)
        {
            float f = Mathf.Clamp01(fraccion);
            _fill.style.width = Length.Percent(f * 100f);
            _lblPorcentaje.text = $"{Mathf.RoundToInt(f * 100f)}%";
        }
    }
}
