using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using Folgen21.UI;

namespace Folgen21.EditorTools
{
    /// <summary>
    /// Instalador de la interfaz Folgen 21.
    /// Menu: Tools > Folgen 21 UI
    /// "Instalar todo" crea el PanelSettings (Assets/UI/Settings.asset), el tema TSS,
    /// configura los sprites 9-slice y genera la escena de demostracion lista para Play.
    /// </summary>
    public static class FolgenUIInstaller
    {
        private const string RaizUI = "Assets/UI";
        private const string RutaPanelSettings = RaizUI + "/Settings.asset";
        private const string RutaTemaTss = RaizUI + "/Core/FolgenTheme.tss";
        private const string RutaEscenaDemo = RaizUI + "/Demo/Demo_FolgenUI.unity";
        private const string CarpetaArt = RaizUI + "/Art";

        // ------------------------------------------------------------------
        // 1. INSTALAR TODO
        // ------------------------------------------------------------------
        [MenuItem("Tools/Folgen 21 UI/1. Instalar todo (1 clic)", priority = 0)]
        public static void InstalarTodo()
        {
            CrearPanelSettings();
            ConfigurarSprites();
            CrearEscenaDemo();
            Debug.Log("[Folgen21] Instalacion completa. Abre 'Assets/UI/Demo/Demo_FolgenUI' y pulsa Play. ESC abre la pausa.");
        }

        // ------------------------------------------------------------------
        // 2. PANEL SETTINGS + TEMA
        // ------------------------------------------------------------------
        [MenuItem("Tools/Folgen 21 UI/2. Crear PanelSettings (Settings.asset)", priority = 1)]
        public static void CrearPanelSettings()
        {
            var existente = AssetDatabase.LoadAssetAtPath<PanelSettings>(RutaPanelSettings);
            if (existente != null)
            {
                Debug.Log("[Folgen21] PanelSettings ya existe en " + RutaPanelSettings);
                return;
            }

            var ps = ScriptableObject.CreateInstance<PanelSettings>();
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ps.referenceResolution = new Vector2Int(1920, 1080);
            ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            ps.match = 0.5f;

            var tema = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(RutaTemaTss);
            if (tema == null)
            {
                File.WriteAllText(RutaTemaTss, "@import url(\"unity-theme://default\");\n");
                AssetDatabase.ImportAsset(RutaTemaTss);
                tema = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(RutaTemaTss);
            }

            if (tema != null) ps.themeStyleSheet = tema;
            else Debug.LogWarning("[Folgen21] No se pudo crear el tema TSS; asigna un ThemeStyleSheet manualmente.");

            AssetDatabase.CreateAsset(ps, RutaPanelSettings);
            AssetDatabase.SaveAssets();
            Debug.Log("[Folgen21] PanelSettings creado en " + RutaPanelSettings);
        }

        // ------------------------------------------------------------------
        // 3. SPRITES 9-SLICE
        // ------------------------------------------------------------------
        [MenuItem("Tools/Folgen 21 UI/3. Configurar sprites 9-slice", priority = 2)]
        public static void ConfigurarSprites()
        {
            // Marcos de boton: sprite con bordes de 9-slice (izq, abajo, der, arriba)
            MarcarSprite(CarpetaArt + "/btn_frame_idle.png", new Vector4(48, 30, 48, 30));
            MarcarSprite(CarpetaArt + "/btn_frame_hover.png", new Vector4(48, 30, 48, 30));

            // Resto del arte como Sprite simple
            foreach (string ruta in new[]
            {
                CarpetaArt + "/portrait_frame.png",
                CarpetaArt + "/diamond_gold.png",
                CarpetaArt + "/minimap_marker.png",
                CarpetaArt + "/player_arrow.png",
                CarpetaArt + "/skill_frame_q.png",
                CarpetaArt + "/skill_frame_e.png",
                CarpetaArt + "/skill_frame_r.png",
                CarpetaArt + "/skill_frame_i.png",
                CarpetaArt + "/icon_gear_button.png",
            })
            {
                MarcarSprite(ruta, Vector4.zero);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Folgen21] Sprites configurados (9-slice en marcos de botones).");
        }

        private static void MarcarSprite(string ruta, Vector4 bordes)
        {
            var importador = AssetImporter.GetAtPath(ruta) as TextureImporter;
            if (importador == null)
            {
                Debug.LogWarning("[Folgen21] No se encontro " + ruta);
                return;
            }

            bool hayCambios = false;
            if (importador.textureType != TextureImporterType.Sprite)
            {
                importador.textureType = TextureImporterType.Sprite;
                importador.spriteImportMode = SpriteImportMode.Single;
                hayCambios = true;
            }
            if (bordes != Vector4.zero && importador.spriteBorder != bordes)
            {
                importador.spriteBorder = bordes;
                hayCambios = true;
            }
            if (importador.mipmapEnabled)
            {
                importador.mipmapEnabled = false;
                hayCambios = true;
            }
            if (hayCambios) importador.SaveAndReimport();
        }

        // ------------------------------------------------------------------
        // 4. ESCENA DE DEMOSTRACION
        // ------------------------------------------------------------------
        [MenuItem("Tools/Folgen 21 UI/4. Crear escena de demo", priority = 3)]
        public static void CrearEscenaDemo()
        {
            var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(RutaPanelSettings);
            if (ps == null)
            {
                CrearPanelSettings();
                ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(RutaPanelSettings);
            }

            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Raiz de UI
            var raiz = new GameObject("UIRoot");
            var uiManager = raiz.AddComponent<UIManager>();
            uiManager.modoDemo = true;
            uiManager.escenaJuego = "Gameplay";
            uiManager.escenaMenu = "Demo_FolgenUI";

            GameplayHUDController hudCtrl = null;

            // Las 5 pantallas (el orden define el orden de render: overlays al final)
            CrearPantalla<MainMenuController>(raiz, ps, "MainMenu", out _);
            CrearPantalla<GameplayHUDController>(raiz, ps, "GameplayHUD", out hudCtrl);
            CrearPantalla<PauseMenuController>(raiz, ps, "PauseMenu", out _);
            CrearPantalla<SettingsMenuController>(raiz, ps, "SettingsMenu", out _);
            CrearPantalla<LoadingScreenController>(raiz, ps, "LoadingScreen", out _);

            // Driver de demostracion
            var goDemo = new GameObject("DemoDriver");
            var demo = goDemo.AddComponent<Folgen21.Demo.DemoDriver>();
            demo.hud = hudCtrl;

            // Guardar
            Directory.CreateDirectory(RaizUI + "/Demo");
            EditorSceneManager.SaveScene(escena, RutaEscenaDemo);

            // Agregar a Build Settings
            var escenas = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!escenas.Exists(s => s.path == RutaEscenaDemo))
            {
                escenas.Add(new EditorBuildSettingsScene(RutaEscenaDemo, true));
                EditorBuildSettings.scenes = escenas.ToArray();
            }

            Debug.Log("[Folgen21] Escena demo creada: " + RutaEscenaDemo);
        }

        private static void CrearPantalla<T>(GameObject raiz, PanelSettings ps, string nombre, out T controlador)
            where T : UIScreen
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(raiz.transform, false);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = ps;
            doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{RaizUI}/{nombre}/{nombre}.uxml");
            if (doc.visualTreeAsset == null)
                Debug.LogError("[Folgen21] No se encontro " + RaizUI + "/" + nombre + "/" + nombre + ".uxml");
            controlador = go.AddComponent<T>();
        }

        // ------------------------------------------------------------------
        // 5. ABRIR README
        // ------------------------------------------------------------------
        [MenuItem("Tools/Folgen 21 UI/5. Abrir README", priority = 4)]
        public static void AbrirReadme()
        {
            EditorUtility.OpenWithDefaultApp(RaizUI + "/README.md");
        }
    }
}
