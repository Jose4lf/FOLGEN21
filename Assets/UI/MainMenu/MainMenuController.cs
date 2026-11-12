using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Folgen21.UI
{
    /// <summary>
    /// Controlador del menu principal de Folgen 21.
    /// Expone eventos C# para que tu gameplay se conecte sin acoplamiento:
    ///     ui.NuevaPartidaClicked += TuMetodo;
    /// UIManager ya conecta el flujo basico automaticamente.
    /// </summary>
    public class MainMenuController : UIScreen
    {
        public event Action NuevaPartidaClicked;
        public event Action ContinuarClicked;
        public event Action AjustesClicked;
        public event Action SalirClicked;

        private const string KeyGuardado = "folgen_tiene_guardado";

        private Button _btnNuevaPartida;
        private Button _btnContinuar;
        private Button _btnAjustes;
        private Button _btnSalir;

        /// <summary>Marca si existe partida guardada (activa/desactiva CONTINUAR).</summary>
        public static void MarcarGuardadoExistente(bool existe)
        {
            if (existe) PlayerPrefs.SetInt(KeyGuardado, 1);
            else PlayerPrefs.DeleteKey(KeyGuardado);
        }

        protected override void OnRaizPreparada(VisualElement raiz)
        {
            _btnNuevaPartida = Q<Button>("btnNuevaPartida");
            _btnContinuar    = Q<Button>("btnContinuar");
            _btnAjustes      = Q<Button>("btnAjustes");
            _btnSalir        = Q<Button>("btnSalir");

            _btnNuevaPartida.clicked += () => NuevaPartidaClicked?.Invoke();
            _btnContinuar.clicked    += () => ContinuarClicked?.Invoke();
            _btnAjustes.clicked      += () => AjustesClicked?.Invoke();
            _btnSalir.clicked        += () => SalirClicked?.Invoke();
        }

        protected override void OnMostrado()
        {
            // CONTINUAR solo disponible si hay partida guardada
            _btnContinuar.SetEnabled(PlayerPrefs.HasKey(KeyGuardado));
        }
    }
}
