# Folgen 21 - Paquete de Interfaz (Unity 6 / UI Toolkit)

Interfaz completa lista para **Unity 6000.3.28f1 (Unity 6)**, construida 100% con **UI Toolkit (UXML + USS + C#)**, fiel al diseño cyberpunk del juego: menú principal, HUD de juego, menú de pausa, ajustes y pantalla de carga.

---

## 1. Instalación

### Opción A — .unitypackage (recomendada)
1. Doble clic en `Folgen21_UI.unitypackage` (o en Unity: **Assets → Import Package → Custom Package...**).
2. En la ventana de importación deja todo marcado y pulsa **Import**.

### Opción B — ZIP
1. Descomprime `Folgen21_UI.zip`.
2. Copia la carpeta `Assets/UI` dentro de la carpeta `Assets` de tu proyecto.
3. Vuelve a Unity y deja que refresque.

> Nota: si tu proyecto ya tenía archivos con los mismos nombres (por ejemplo `MainMenuController.cs`), **serán reemplazados** por los de este paquete.

## 2. Configuración en 1 clic

Tras importar, en la barra de Unity:

```
Tools → Folgen 21 UI → 1. Instalar todo (1 clic)
```

Esto hace automáticamente:
- **Crea `Assets/UI/Settings.asset`** (PanelSettings, escala 1920×1080 adaptativa) — el archivo que ya estaba reservado en tu estructura.
- Crea el tema por defecto (`FolgenTheme.tss`).
- Configura los **sprites 9-slice** de los marcos de botones.
- Genera la **escena de demostración** `Assets/UI/Demo/Demo_FolgenUI.unity` con las 5 pantallas montadas y la agrega a Build Settings.

## 3. Probar la demo

1. Abre `Assets/UI/Demo/Demo_FolgenUI.unity`.
2. Pulsa **Play**. Verás el menú principal.
3. **NUEVA PARTIDA** → pantalla de carga con consejos → HUD de juego con tutorial animado (misión MOVIMIENTO → DISPARO).
4. En el HUD: **clic izquierdo** dispara, **R** recarga, **Q/E/R** muestran enfriamientos, **H** baja la vida, **ESC** abre la pausa.
5. En la pausa: CONTINUAR, AJUSTES (música, efectos, sensibilidad, calidad, resolución, pantalla completa), MENÚ PRINCIPAL y SALIR.

La escena demo usa `UIManager.modoDemo = true` (carga simulada, sin cambiar de escena). Desactiva esa casilla en tu juego real.

## 4. Integrar en TU juego

### Estructura recomendada
Crea (o reutiliza) un GameObject `UIRoot` en tu escena de menú con el script `UIManager`, y debajo un hijo por pantalla con `UIDocument` + su controlador, todos con `PanelSettings = Assets/UI/Settings.asset`:

```
UIRoot                ← UIManager (quita la casilla modoDemo)
├── MainMenu          ← UIDocument(MainMenu.uxml) + MainMenuController
├── GameplayHUD       ← UIDocument(GameplayHUD.uxml) + GameplayHUDController
├── PauseMenu         ← UIDocument(PauseMenu.uxml) + PauseMenuController
├── SettingsMenu      ← UIDocument(SettingsMenu.uxml) + SettingsMenuController
└── LoadingScreen     ← UIDocument(LoadingScreen.uxml) + LoadingScreenController
```

- En `UIManager` pon `escenaJuego = "TuEscenaDeJuego"` (debe estar en Build Settings) y `escenaMenu = "TuEscenaDeMenu"`.
- La forma más simple para un proyecto pequeño: deja las 5 pantallas en una única escena persistente y usa `modoDemo = false` (la carga sí cambia de escena; el HUD se muestra al terminar).

### Conectar tus sistemas al HUD (sin acoplamiento)

```csharp
var hud = FindObjectOfType<GameplayHUDController>();

hud.SetJugador("JHOEL", 7);          // nombre y nivel
hud.SetVida(0.75f);                   // vida 0..1
hud.SetEnergia(0.5f);                 // energia 0..1
hud.SetMunicion(12, 48);              // cargador / reserva
hud.SetMision("TUTORIAL: MOVIMIENTO", "Dirígete al punto de control (45 m)");
hud.SetModoDisparo(true);             // cambia los consejos de control
hud.MostrarTip("Usa el mapa para encontrar tu objetivo.", 4f);
hud.SetMiraVisible(false);            // mira central (al apuntar con armas la ocultas)
hud.SetIconoSkill(0, miTextura);      // iconos por codigo (0=Q,1=E,2=R,3=I)
hud.SetEnfriamiento(0, 0.6f);         // enfriamiento visual 0..1
hud.SetCantidadItem(3, 3);            // contador del slot de items

// Marcador dorado sobre un objetivo del mundo:
hud.objetivoMundo = miTransformDeObjetivo;   // se posiciona y mide solo

// Minimapa: pásale el RenderTexture de tu cámara cenital:
hud.SetTexturaMinimapa(miRenderTexture);
// y cada frame el desplazamiento del marcador:
hud.SetMarcadorMinimapa(hud.CalcularOffsetMinimapa(posJugador, posObjetivo, pixelesPorMetro, rotacionGrados));
hud.SetRotacionFlecha(anguloGrados);
```

### Conectar tus sistemas a los menús

```csharp
var menu = FindObjectOfType<MainMenuController>();
menu.NuevaPartidaClicked += TuLogicaDeNuevaPartida;
menu.ContinuarClicked    += TuLogicaDeCargarPartida;

// Para activar el botón CONTINUAR cuando exista guardado:
MainMenuController.MarcarGuardadoExistente(true);

var pausa = FindObjectOfType<PauseMenuController>();
pausa.MenuPrincipalClicked += TuLogicaDeSalirAlMenu;
```

### Ajustes guardados

Todo se guarda en `PlayerPrefs` con claves `folgen_*` y se aplica al arrancar. Lee los valores desde tu gameplay:

```csharp
float sensibilidad = SettingsMenuController.SensibilidadActual;  // multiplicador 0.1-3
float musica  = SettingsMenuController.VolumenMusica;            // 0-1
float efectos = SettingsMenuController.VolumenEfectos;           // 0-1
```

Conéctalo a tu **AudioMixer**: `mixer.SetFloat("Musica", Mathf.Log10(musica) * 20);`

### Flujo de estado (UIManager)

`EstadoUI`: `MenuPrincipal → Cargando → Jugando ⇄ Pausado → Ajustes`. Notifica cambios:

```csharp
UIManager.Instancia.OnCambioEstado += estado => { /* pausa tu audio, desactiva controles, etc. */ };
```

## 5. Herramienta para VER y EDITAR el diseño: Unity UI Builder

Tu Unity 6 trae el editor visual oficial de UI Toolkit:

1. **Window → UI Toolkit → UI Builder**.
2. Arrastra `Assets/UI/MainMenu/MainMenu.uxml` (o cualquiera de las 5 pantallas) al panel izquierdo.
3. Verás la pantalla renderizada en vivo; puedes mover elementos, cambiar estilos USS y guardar — se escribe directamente en los `.uxml/.uss` del paquete.
4. Consejo: mantén abierto `FolgenTheme.uss` para los tokens compartidos (colores, fuentes, botones).

Adicionalmente el paquete incluye **`preview.html`** (en la raíz del ZIP): ábrelo en cualquier navegador para ver las 5 pantallas con el arte final sin abrir Unity.

## 6. Personalización rápida

| Quiero cambiar... | Edita... |
|---|---|
| Colores del tema | `Core/FolgenTheme.uss` (variables `--c-*`) |
| Fondo del menú / carga | `Art/mainmenu_bg.png`, `Art/loading_bg.png` |
| Retrato del jugador | `Art/portrait_jhoel.png` |
| Iconos de habilidades | `Art/icon_skill_q/e/r.png`, `icon_item.png` |
| Tipografías | `Fonts/*.ttf` (referenciadas en cada .uss) |
| Consejos de la carga | `LoadingScreenController.consejos` (Inspector) |
| Controles de control (WASD etc.) | `GameplayHUD.uxml` (grupos `grupoMovimiento`/`grupoDisparo`) |

## 7. Solución de problemas

- **Pantalla negra o "falta el UIDocument"**: ejecuta `Tools → Folgen 21 UI → 1. Instalar todo` y verifica que cada UIDocument tenga `PanelSettings = Settings.asset`.
- **Texto en fuente por defecto**: Unity importa los `.ttf` como Font legado; los `.uss` los referencian con `-unity-font`. Si sustituyes fuentes, conserva los mismos nombres de archivo o actualiza las rutas `../Fonts/...` en los `.uss`.
- **Input System**: el código es dual (`#if ENABLE_INPUT_SYSTEM` / `ENABLE_LEGACY_INPUT_MANAGER`); funciona sin tocar nada en ambos modos de *Active Input Handling*.
- **ESC no abre la pausa**: `UIManager` debe estar en la escena y el estado debe ser `Jugando`.
- **El HUD bloquea clics**: no debería (todo el HUD está en `PickingMode.Ignore`); si agregas elementos nuevos al HUD, márcalos así también.
- **Escena "Gameplay" no encontrada**: agrega tus escenas a *File → Build Settings*.

## 8. Contenido del paquete

```
Assets/UI/
├── Settings.asset            ← PanelSettings (lo crea el instalador)
├── Core/
│   ├── UIManager.cs          ← coordinador de flujo y pausa (ESC)
│   ├── UIScreen.cs           ← base de todos los controladores
│   ├── FolgenTheme.uss       ← tokens de diseño compartidos
│   └── FolgenTheme.tss       ← tema (lo crea el instalador)
├── MainMenu/                 ← .uxml + .uss + MainMenuController.cs
├── GameplayHUD/              ← .uxml + .uss + GameplayHUDController.cs
├── PauseMenu/                ← .uxml + .uss + PauseMenuController.cs
├── SettingsMenu/             ← .uxml + .uss + SettingsMenuController.cs
├── LoadingScreen/            ← .uxml + .uss + LoadingScreenController.cs
├── Demo/
│   ├── DemoDriver.cs         ← demo jugable del HUD
│   └── Demo_FolgenUI.unity   ← escena (la crea el instalador)
├── Editor/
│   └── FolgenUIInstaller.cs  ← menú Tools → Folgen 21 UI
├── Art/                      ← fondos, retrato, iconos, marcos 9-slice
├── Fonts/                    ← Orbitron (títulos) y Rajdhani (texto), licencia OFL
└── README.md
```
