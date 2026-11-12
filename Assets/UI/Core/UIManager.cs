using System;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Folgen21.UI
{
    /// <summary>
    /// Coordinador central de la interfaz de Folgen 21.
    /// Colocar en un GameObject raiz "UIRoot" junto a los GameObjects de cada pantalla.
    /// Gestiona: Menu Principal -> Carga -> HUD -> Pausa -> Ajustes.
    /// Compatible con New Input System y Legacy Input Manager de forma automatica.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instancia { get; private set; }

        [Header("Configuracion de flujo")]
        [Tooltip("Nombre de la escena de juego a la que se entra tras la pantalla de carga.")]
        public string escenaJuego = "Gameplay";

        [Tooltip("Nombre de la escena del menu principal (para volver al menu desde el juego).")]
        public string escenaMenu = "MainMenu";

        [Tooltip("Modo demostracion: carga simulada sin cambiar de escena y HUD con datos de prueba. Desactivalo en tu juego real.")]
        public bool modoDemo = true;

        [Tooltip("Congelar el tiempo del juego (Time.timeScale) mientras la pausa esta abierta.")]
        public bool congelarTiempoEnPausa = true;

        /// <summary>Estado actual de la interfaz.</summary>
        public enum EstadoUI { MenuPrincipal, Jugando, Pausado, Cargando, Ajustes }

        public EstadoUI Estado { get; private set; } = EstadoUI.MenuPrincipal;

        /// <summary>Notifica cambios de estado a otros sistemas del juego.</summary>
        public event Action<EstadoUI> OnCambioEstado;

        private MainMenuController _mainMenu;
        private GameplayHUDController _hud;
        private PauseMenuController _pausa;
        private SettingsMenuController _ajustes;
        private LoadingScreenController _carga;
        private EstadoUI _estadoPrevioAjustes = EstadoUI.MenuPrincipal;

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }
            Instancia = this;
        }

        private void Start()
        {
            _mainMenu = GetComponentInChildren<MainMenuController>(true);
            _hud = GetComponentInChildren<GameplayHUDController>(true);
            _pausa = GetComponentInChildren<PauseMenuController>(true);
            _ajustes = GetComponentInChildren<SettingsMenuController>(true);
            _carga = GetComponentInChildren<LoadingScreenController>(true);

            if (_mainMenu == null || _hud == null || _pausa == null || _ajustes == null || _carga == null)
            {
                Debug.LogError("[Folgen21] UIManager necesita los 5 controladores de pantalla como hijos.");
                enabled = false;
                return;
            }

            // Conexión de eventos de botones
            _mainMenu.NuevaPartidaClicked += OnNuevaPartida;
            _mainMenu.ContinuarClicked += OnContinuar;
            _mainMenu.AjustesClicked += () => AbrirAjustes();
            _mainMenu.SalirClicked += SalirDelJuego;

            _pausa.ContinuarClicked += Reanudar;
            _pausa.AjustesClicked += () => AbrirAjustes();
            _pausa.MenuPrincipalClicked += VolverAlMenuPrincipal;
            _pausa.SalirClicked += SalirDelJuego;

            _ajustes.Cerrado += CerrarAjustes;
            _carga.CargaCompletada += OnCargaCompletada;

            // Estado inicial: solo el menu principal visible
            _hud.Mostrar(false);
            _pausa.Mostrar(false);
            _ajustes.Mostrar(false);
            _carga.Mostrar(false);
            _mainMenu.Mostrar(true);
            CambiarEstado(EstadoUI.MenuPrincipal);
        }

        private void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
        }

        // ---------------- ESC / entrada ----------------

        private void Update()
        {
            if (PresionoEscape())
            {
                switch (Estado)
                {
                    case EstadoUI.Jugando: Pausar(); break;
                    case EstadoUI.Pausado: Reanudar(); break;
                    case EstadoUI.Ajustes: CerrarAjustes(); break;
                }
            }
        }

        private static bool PresionoEscape()
        {
#if ENABLE_INPUT_SYSTEM
            var teclado = UnityEngine.InputSystem.Keyboard.current;
            if (teclado != null && teclado.escapeKey.wasPressedThisFrame)
                return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape))
                return true;
#endif
            return false;
        }

        // ---------------- Flujo principal ----------------

        private void OnNuevaPartida()
        {
            _mainMenu.Mostrar(false);
            _hud.Mostrar(false);
            _pausa.Mostrar(false);

            _carga.Mostrar(true);

            CambiarEstado(EstadoUI.Cargando);

            if (modoDemo)
                _carga.IniciarCargaSimulada(4.5f);
            else
                _carga.IniciarCargaEscena(escenaJuego);
        }

        private void OnContinuar()
        {
            // En tu juego real: carga aqui tu partida guardada antes de continuar.
            OnNuevaPartida();
        }

        private void OnCargaCompletada()
        {
            _carga.Mostrar(false);
            _hud.Mostrar(true);
            CambiarEstado(EstadoUI.Jugando);
        }

        public void Pausar()
        {
            if (Estado != EstadoUI.Jugando) return;
            _pausa.Mostrar(true);
            CambiarEstado(EstadoUI.Pausado);
            if (congelarTiempoEnPausa) Time.timeScale = 0f;
        }

        public void Reanudar()
        {
            if (Estado != EstadoUI.Pausado) return;
            _pausa.Mostrar(false);
            CambiarEstado(EstadoUI.Jugando);
            Time.timeScale = 1f;
        }

        public void AbrirAjustes()
        {
            _estadoPrevioAjustes = Estado;
            if (Estado == EstadoUI.Pausado) _pausa.Mostrar(false);
            _ajustes.Mostrar(true);
            CambiarEstado(EstadoUI.Ajustes);
        }

        public void CerrarAjustes()
        {
            if (Estado != EstadoUI.Ajustes) return;
            _ajustes.Mostrar(false);

            if (_estadoPrevioAjustes == EstadoUI.Pausado)
            {
                _pausa.Mostrar(true);
                CambiarEstado(EstadoUI.Pausado);
            }
            else
            {
                CambiarEstado(_estadoPrevioAjustes);
            }
        }

        public void VolverAlMenuPrincipal()
        {
            Time.timeScale = 1f;
            _pausa.Mostrar(false);
            _hud.Mostrar(false);

            if (!modoDemo && SceneManager.GetActiveScene().name != escenaMenu)
            {
                CambiarEstado(EstadoUI.Cargando);
                _carga.IniciarCargaEscena(escenaMenu);
                return;
            }

            _mainMenu.Mostrar(true);
            CambiarEstado(EstadoUI.MenuPrincipal);
        }

        public void SalirDelJuego()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void CambiarEstado(EstadoUI nuevo)
        {
            if (Estado == nuevo) return;
            Estado = nuevo;
            OnCambioEstado?.Invoke(nuevo);
        }
    }
}
