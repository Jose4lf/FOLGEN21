using System;
using UnityEngine.UIElements;

namespace Folgen21.UI
{
    /// <summary>
    /// Controlador del menu de pausa. UIManager gestiona abrir/cerrar con ESC;
    /// los eventos estan disponibles para conectar logica extra de tu juego.
    /// </summary>
    public class PauseMenuController : UIScreen
    {
        public event Action ContinuarClicked;
        public event Action AjustesClicked;
        public event Action MenuPrincipalClicked;
        public event Action SalirClicked;

        protected override void OnRaizPreparada(VisualElement raiz)
        {
            Q<Button>("btnContinuar").clicked    += () => ContinuarClicked?.Invoke();
            Q<Button>("btnAjustes").clicked      += () => AjustesClicked?.Invoke();
            Q<Button>("btnMenuPrincipal").clicked += () => MenuPrincipalClicked?.Invoke();
            Q<Button>("btnSalir").clicked        += () => SalirClicked?.Invoke();
        }
    }
}
