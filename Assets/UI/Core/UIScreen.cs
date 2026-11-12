using UnityEngine;
using UnityEngine.UIElements;

namespace Folgen21.UI
{
    /// <summary>
    /// Clase base para todos los controladores de pantalla del paquete.
    /// Cada pantalla es un GameObject con UIDocument + controlador.
    /// Show/Hide solo conmuta el estilo display de la raiz (el GameObject permanece activo).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public abstract class UIScreen : MonoBehaviour
    {
        [Tooltip("Si esta activo, la pantalla se muestra automaticamente al iniciar la escena.")]
        public bool mostrarAlIniciar = false;

        protected UIDocument Documento { get; private set; }
        protected VisualElement Raiz { get; private set; }

        /// <summary>Indica si la pantalla esta visible actualmente.</summary>
        public bool Visible { get; private set; }

        protected virtual void Awake()
        {
            Documento = GetComponent<UIDocument>();
            if (Documento == null || Documento.rootVisualElement == null)
            {
                Debug.LogError($"[Folgen21] {name}: falta el UIDocument o su PanelSettings no esta asignado.");
                enabled = false;
                return;
            }

            Raiz = Documento.rootVisualElement;
            Raiz.AddToClassList("fscreen");
            // La raiz no debe bloquear clics hacia el juego; los elementos interactivos
            // de cada pantalla siguen recibiendo eventos individualmente.
            Raiz.pickingMode = PickingMode.Ignore;

            OnRaizPreparada(Raiz);
            Mostrar(mostrarAlIniciar);
        }

        /// <summary>Se llama una vez cuando la raiz esta lista. Haz aqui tus consultas Q().</summary>
        protected abstract void OnRaizPreparada(VisualElement raiz);

        /// <summary>Muestra u oculta la pantalla.</summary>
        public void Mostrar(bool visible)
        {
            Visible = visible;
            if (Raiz != null)
                Raiz.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

            if (visible)
                OnMostrado();
        }

        /// <summary>Se llama cada vez que la pantalla pasa a visible.</summary>
        protected virtual void OnMostrado() { }

        /// <summary>Atajo de consulta por nombre.</summary>
        protected T Q<T>(string nombre) where T : VisualElement => Raiz.Q<T>(nombre);
    }
}
