# Contexto del proyecto "El Alfarero de Dolores" (para asistentes de IA)

> Documento para pegar en un asistente de IA (Claude, ChatGPT, etc.) antes de pedirle trabajo sobre este repositorio.
> Actualizado al **22 de octubre de 2026**. Lo mantiene el equipo: Omar Salinas (@omarsalinas3) y Noé Chavero (@SekiroK5).

---

## 1. Qué es

Videojuego 2D **run and gun** de desplazamiento lateral (estilo Metal Slug), pixel art, para la materia *Creación de Videojuegos* (UTNG, grupo GIDS6102, profesor Gabriel Barrón). Proyecto de 12 semanas en 6 sprints de 2 semanas.

- **Protagonista:** Mateo, alfarero de Dolores Hidalgo. Lanza cerámica en lugar de balas.
- **Historia:** el **Charro Negro**, líder de una banda de bandidos sobrenaturales, secuestra a Carmen (esposa de Mateo) una noche de luna roja. Mateo cruza Guanajuato enfrentando leyendas para rescatarla.
- **Público:** 12+ y adultos nostálgicos de arcade con temática mexicana.
- **Meta final:** publicar en itch.io.

### Los 4 niveles (acordado el 22-oct; reemplaza lo que diga el README viejo)

| Nivel | Zona | Enemigos | Evento especial | Jefe |
|---|---|---|---|---|
| 1 | **Dolores Hidalgo** (hecho) | Mineros esqueleto, bandidos (revólver, rifle, escopeta, machete) | Emboscada de bandidos en oleadas | **Alebrije Gigante**: sale de entre las torres de la parroquia. **Cambio pendiente:** al vencerlo se libera del hechizo y se vuelve la **montura** de Mateo |
| 2 | **Mineral de Pozos** (pueblo fantasma / ruinas mineras del desierto) | Bandidos, esqueletos, **Bruja de Fuego** (voladora) | **Estampida de nahuales** | **Jinete sin Cabeza** |
| 3 | **Panteón junto al río** | Esqueletos y otros | (por definir) | **La Llorona** (ataques sónicos y de agua) |
| 4 | **Túneles de Guanajuato** | Bandidos, esqueletos | Derrumbe de mina | **El Charro Negro** (jefe final); rescate de Carmen al amanecer |

El GDD original solo daba ejemplos de jefes ("figuras icónicas como La Llorona, El Charro Negro y Alebrijes Gigantes"); el Jinete y la montura son **modificaciones justificadas** (requisito del curso, issue #34).

### Requisitos del curso (avance del 14 de octubre, 20 puntos)
Splash con logo del equipo · menú (Jugar, Continuar, Opciones, Créditos, Salir) · opciones (volumen, controles, calidad) · tutorial · HUD (vida, puntaje, tiempo, objetivo) · mecánicas de la Unidad 1 fieles (o modificaciones justificadas por escrito) · victoria y derrota claras · dificultad progresiva · enemigos con IA · física coherente · sonido con créditos/licencias · retroalimentación visual · narrativa · guardado · controles por plataforma (teclado/gamepad, táctil) · UI adaptable · créditos · rendimiento estable · Git con **PR frecuentes de ambos** · documentación (GDD, manual, bitácora).

---

## 2. Stack y configuración (importante)

- **Unity 6.6 (6000.6.0f1)**, URP 2D, **Input System** (las acciones se crean por código, no hay `.inputactions`).
- **Git LFS** para audio (`.wav`). Antes de clonar o hacer pull: `git lfs install` y `git lfs pull`; si no, los audios llegan vacíos.
- **Enter Play Mode Options con "Disable Domain Reload" activado**: las variables `static` **no se reinician** entre un Play y otro. Todo `static` debe reiniciarse con `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` (ver `GestorAudio.ReiniciarEstaticos`). Además, `?.` de C# **no** detecta objetos destruidos de Unity: usar `obj != null ? obj : null`.
- **Repositorio:** https://github.com/omarsalinas3/ElAlfarero
- **Ramas:** `main` (entregas), `develop` (integración), `sprint-1`/`sprint-2` (cortes por sprint), `feat/omar`, `feat/noe`. Flujo: rama `feat/*` → **PR a `develop`** (con "Closes #N" en la descripción) → al cerrar sprint, `develop` → rama del sprint / `main`.
- **Estado de `develop`:** Sprint 1 + flujo completo de Omar (#41) + `.meta` de Noé (#42).
- **Trabajo grande sin subir todavía en `feat/noe`** (issues #27, #44–#48): IA de esqueletos y bandidos, emboscada, jefe alebrije, animaciones de Mateo, controles nuevos y arreglos. Noé abre el PR (#49). **Antes de tocar `ConstructorJuego.cs`, `PlayerShooter.cs`, `IAEsqueleto.cs` o el Nivel 1, esperar a que ese PR entre a `develop` y hacer pull**, o habrá conflictos fuertes.
- **Convenciones de commits:** en español, en imperativo ("Agregar…", "Corregir…"), sin atribuciones a IA. No subir ruido de Unity: `ProjectSettings/Packages/com.unity.learn.iet-framework/Settings.json`, el define `SENTIS_ANALYTICS_ENABLED` en `ProjectSettings.asset`, ni animaciones/escenas que solo cambiaron saltos de línea. Los `.meta` de sprites que Unity 6.6 llena con `WindowsStoreApps` van en un commit aparte.

---

## 3. Arquitectura

### Carpetas
```
Assets/
  Art/Sprites/{Player,Enemies,Bosses,Allies,Items,Obstacles,Projectiles,UI,Intro,Environment,Referencias}
  Art/Animations/            Mateo.controller y clips; Bandidos/ y Alebrije/ (generados por código)
  Audio/{Music,SFX}          .wav por LFS
  Prefabs/{Player,Enemies,Bosses,Obstacles,Pickups,Projectiles,Effects}
  Resources/BibliotecaAudio.asset
  ScriptableObjects/Ceramicas/
  Scenes/                    MenuPrincipal, Intro, Nivel1_Dolores, Sandbox (pruebas)
  Scripts/{Core,Player,Combat,Enemies,UI,Editor}   ensamblados Alfarero.Runtime y Alfarero.Editor
  Tests/PlayMode/
Docs/                        ARQUITECTURA.md, Arte/Flow/ (hojas originales de Flow), este archivo
```

### ⚠️ Las escenas y prefabs se generan por código
- **Alfarero > Construir juego completo** (`Editor/ConstructorJuego.cs`) regenera prefabs, biblioteca de audio, animaciones y las escenas `MenuPrincipal`, `Intro`, `Nivel1_Dolores` y `Sandbox`. Llama primero a `ConfigurarSprint1.Construir()` (capas, Mateo, Talavera, Sandbox) y a `ConstructorAnimaciones.Construir()` (bandidos y alebrije).
- **Si se edita una escena a mano y luego alguien corre el constructor, se pierde.** Todo cambio de nivel (posiciones de enemigos, obstáculos, eventos) debe ir en el constructor.
- Al correrlo se regeneran también cosas que no cambiaron (otras escenas, `.anim` con otros saltos de línea): **descartar ese ruido** con `git checkout -- <archivo>` y subir solo lo que de verdad cambió.
- Ejecutar sin abrir Unity (con el editor **cerrado**):
  ```
  "C:/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Unity.exe" -batchmode -quit -projectPath <ruta> -executeMethod ConstructorJuego.Construir -logFile construir.log
  ```
  Si a la primera falla con scripts vacíos (`m_Script: {fileID: 0}`) es porque estaba importando sprites nuevos: restaurar con git y repetir.

### Capas (`ConfigurarSprint1` / `ConstructorJuego`)
`6 Ground` (suelo, muros, huacales, hornos), `7 Player`, `8 Enemy` (Enemy no choca con Enemy), `9 Projectile` (cerámica; no choca con Player ni entre sí), `10 EnemyProjectile` (balas enemigas; solo chocan con Player y Ground).

### Escala de sprites (`Editor/ImportadorSprites.cs`)
Todo lo que entra a `Assets/Art/Sprites` se configura solo (Point, sin compresión, pivote en los pies para personajes). Pixels Per Unit por carpeta (personajes 280, fondos 128, items 300…) y **por nombre** para los cuadros de Flow: `mateo_idle*`/`mateo_muerte*` 148, `bandido_*` 100, `alebrije_*` 55. Mateo mide ~2.5 unidades; la cámara ve 12 de alto y ~21.3 de ancho; el suelo está en y = −4.5.

### Sistemas principales (scripts)
| Script | Qué hace |
|---|---|
| `Combat/Health` | Vida genérica con eventos `OnDano`, `OnVidaCambiada`, `OnMuerte`; `Invulnerable` manual, `HacerInvulnerable(seg)`, `destruirAlMorir` |
| `Combat/DanoPorContacto` | Daña al tocar (capas objetivo) y empuja a Mateo |
| `Combat/Projectile` + `CeramicaData` (ScriptableObject) | Cerámica del jugador: daño, velocidad, cadencia, proyectiles por disparo, dispersión |
| `Combat/BalaEnemiga` | Bala de bandidos; atraviesa a Mateo si está evadiendo |
| `Combat/Recogible` | Guacamaya (cura), Caguama (se guarda), Moneda; si está lleno se cambia por monedas (5 / 10) |
| `Combat/Destructible`, `MacetaQueCae`, `ParpadeoDano`, `SpriteAnimadoTemporal` | Huacales/hornos, macetas que caen, destello de daño, efectos de un solo uso |
| `Core/GestorNivel` | Estados `Jugando, Pausa, GameOver, Victoria, Cinematica`; `IniciarCinematica/TerminarCinematica`, `LimitarJugador(min,max)/LiberarJugador`, `MostrarLetrero(a,b,c)`, `Completar()` (victoria). `GestorNivel.Actual` es la instancia |
| `Core/CameraFollow` | Sigue a Mateo en X; `Bloquear(x)`, `Desbloquear()`, `Sacudir(seg, fuerza)`, `MitadAncho`, `LimiteDerecho` |
| `Core/GestorAudio` + `BibliotecaAudio` | `GestorAudio.Reproducir(Sfx.X)`, `CambiarMusica(Musica.X)`. Para un sonido nuevo: agregar al **final** del enum `Sfx`, poner el `.wav` en `Audio/SFX` y registrarlo en `ConstructorJuego.CrearBibliotecaAudio` |
| `Core/CambioEscena` | Fundido entre escenas; `Escenas.Menu/Intro/Nivel1` |
| `Core/Emboscada` | Zona que fija la cámara y lanza oleadas de bandidos |
| `Core/EncuentroAlebrije` | Cinemática del jefe del Nivel 1 (torres → vuelo → aterrizaje → pelea) |
| `Enemies/EnemigoBasico` | Común a enemigos: sonido de golpe/muerte, efecto, estadística de derrotados |
| `Enemies/IAEsqueleto` | Patrulla, persigue, brinca obstáculos bajos, embiste, regresa a su zona |
| `Enemies/IABandido` | Revólver/rifle/escopeta disparan a distancia con aviso; machete cuerpo a cuerpo; animación de muerte y desvanecer. `SiempreAlerta` para emboscadas |
| `Enemies/JefeAlebrije` | Dormido → Activar(arena); acecho, zarpazo, salto con onda de choque, transformación al 50 %, victoria al morir. El arte mira a la izquierda |
| `Player/PlayerController` | Correr, salto (coyote time, buffer, altura variable), evasión invulnerable, `ControlActivo`, `DireccionMirada` |
| `Player/PlayerShooter` | Apuntar y lanzar. Gana el último control usado: mouse → apunta al mouse; flechas → apunta al frente |
| `Player/PlayerAnimator` | Pasa al Animator: Velocidad, EnSuelo, Evadiendo, Muerto |
| `Player/InventarioJugador`, `ReaccionDanoJugador` | Monedas y caguamas (E/Q bebe, 5 s invencible); invulnerabilidad y empujón tras golpe |
| `UI/*` | HUD de barro, banner de misión, barra del jefe, menú, pausa, pantallas finales, intro con viñetas, opciones de audio |

### Controles actuales
Mover A/D o flechas · **Saltar flecha arriba o W** · Evadir Shift izquierdo · **Lanzar Espacio o clic** (mantener) · Apuntar mouse (o al frente con flechas) · Caguama E/Q · Pausa Esc/P/Start.

---

## 4. Arte con Google Flow

- Todo el arte se genera en **Google Flow** (proyecto "oct 15 - 22:23" en la cuenta de Noé) usando como referencia los sprites existentes para conservar el estilo.
- Pedir **hojas de sprites** (filas = animaciones, columnas = cuadros), personaje idéntico en todos los cuadros, alineado por los pies, mirando a un lado, **fondo de un solo color liso** (verde `#00FF00`), sin sombras ni texto. Descargar en **1K (tamaño original)**; el reescalado a 2K suaviza el pixel art.
- Recorte: se quita el color de fondo (con *despill* de orillas), se separa por celdas, se descartan motas sueltas y todos los cuadros de una hoja se alinean por los pies en un lienzo común. Las hojas originales se guardan en `Docs/Arte/Flow/`.
- Nombres: `<personaje>_<animacion>_NN.png` (ej. `bandido_rifle_correr_00.png`). Las animaciones y Animator se generan con `Editor/ConstructorAnimaciones.cs` (`CrearClip`, transiciones con `Velocidad`, triggers y `Muerto`).
- **Carpeta "Modelos" de Omar** (24 imágenes, fondo **magenta**): personajes (Llorona, Charro, Jinete, Bruja de Fuego, Carmen, Don Toribio, minero con pico, Mateo corriendo), 8 fondos (Dolores día/noche, panteón con agua ×2, desierto/pueblo fantasma, túneles día/noche), armas (maceta escudo, Talavera, Barro Negro, Barro Vidriado) e items (HUD, caguama, guacamaya, hornos, huacales, cofre, monedas).
  - Los jefes y el minero vienen **en una sola pose**: hay que pedir sus animaciones a Flow.
  - La Llorona trae **pegada una pierna de Mateo** abajo a la izquierda: regenerarla.
  - La hoja de cajas trae un **AK-47** (no encaja con la época, no usar).
  - Cada fondo es una sola pantalla (~21.5 u); un nivel necesita 4–5 tramos unidos con degradado (como Dolores, ver `ConfigurarSprint1.Segmentos`).

---

## 5. Pruebas

- PlayMode: `Sprint1Tests` (Sandbox: suelo, correr, salto con flecha arriba, evasión, disparo con mouse y con Espacio, orientación), `JuegoTests` (pausa, game over, caguama, guacamaya, huacales, menú), `IAEsqueletoTests`, `NivelUnoTests` (monedas por sobrantes, muerte de Mateo, bandidos, emboscada, jefe y victoria).
- En Unity: *Window > General > Test Runner > PlayMode*. En consola (Unity cerrado):
  ```
  Unity.exe -batchmode -projectPath <ruta> -runTests -testPlatform PlayMode -testResults resultados.xml -logFile tests.log
  ```
- Las pruebas usan dispositivos virtuales del Input System y buscan objetos por posición/tipo; si se mueven enemigos u obstáculos en el constructor, revisar las pruebas que dependen de ellos (p. ej. el minero de x = 27).

---

## 6. Tablero (GitHub Projects) y reparto

Tablero: https://github.com/users/omarsalinas3/projects/3 — campos: Status, Priority (P0–P2), Size, Estimate (puntos), Start/Target date, **Sprint** (iteración) y Milestone. Vista "Sprint actual" filtra el sprint en curso. Cada issue tiene historia de usuario, criterios de aceptación y el punto del avance que cubre.

- **Sprint 2:** 15 → 29 de octubre de 2026. **Sprint 3:** 29 de octubre → 12 de noviembre.
- Reparto equitativo: Sprint 2 pendiente 25 pts cada uno; Sprint 3 Omar 23, Noé 24.

### Tareas de Omar — Sprint 2
| # | Tarea | Pts | Notas |
|---|---|---|---|
| 22 | Sistema de guardado: progreso, nivel desbloqueado y puntaje máximo | 5 | JSON en `Application.persistentDataPath`; se guarda al ganar un nivel. Base para #23 y #57 |
| 23 | Menú: botones Continuar y Créditos | 3 | Continuar carga el último nivel desbloqueado |
| 25 | Splash con nombre del juego y logo del equipo | 2 | Primera escena en Build Settings (agregarla en `ConstructorJuego`) |
| 29 | HUD: puntaje, tiempo y objetivo | 2 | `HudJuego` + `GestorNivel.TiempoJugado` |
| 32 | **Nivel 2: Mineral de Pozos** — escena y recorrido | 8 | Ver sección 7 |
| 51 | Esqueletos animados: minero con pico | 3 | Pedir a Flow caminar, golpe con pico, muerte; reemplazar el sprite fijo de `Minero_Esqueleto` (hoy se "balancea" un solo cuadro). La IA ya existe |
| 52 | Apuntado con teclado | 2 | En `PlayerShooter.DetectarMouse` subir el umbral para que un movimiento mínimo del mouse no robe el control; apuntar hacia arriba manteniendo flecha arriba al lanzar. Ojo: flecha arriba también salta |

### Tareas de Omar — Sprint 3
#26 Opciones de controles y gráficos (3) · #33 Armas: Barro Negro, Barro Vidriado y Maceta escudo (5; el arte ya está en la carpeta Modelos) · #34 Documento de mecánicas y modificaciones justificadas (2) · #38 Optimización y build sin errores (3) · #40 Pruebas de guardado, IA, opciones y Nivel 2 (2) · #54 Bruja de Fuego: sprites e IA voladora (5) · #57 Transición Nivel 1 → 2 montado en el alebrije (3; depende de #22 y #50).

### Tareas de Noé (para saber qué no tocar)
Sprint 2: #49 PR del trabajo del Nivel 1 · #50 **Alebrije: de jefe a montura** · #53 estampida de nahuales · #55/#56 Jinete sin Cabeza (sprites, IA y cinemática) · #60 README con la historia de 4 niveles · #28 tutorial · #31 bitácora. En curso (sin PR): #27, #44–#48.
Sprint 3: #24 créditos · #30 partículas · #35 narrativa · #36 gamepad y táctil · #37 resoluciones · #39 GDD y manual · #58 fondos del Nivel 3 · #59 sprites de la Llorona.

---

## 7. Guía para el Nivel 2 (Mineral de Pozos, issue #32)

- Fondos: `Escenarios/Desert_mining_ruins…` y `Ghost_town…` de la carpeta Modelos; pedir 2–3 tramos más a Flow para que mida como Dolores (~80 unidades).
- **No meterlo en `CrearEscenaNivel1`.** Conviene un constructor propio (p. ej. `Editor/ConstructorNivel2.cs`, menú *Alfarero > Construir Nivel 2*) que reutilice los helpers de `ConstructorJuego`/`ConfigurarSprint1` (`CrearBaseNivel`, `Poner`, `PonerBandido`, `Minero`, `CrearHud`, `CrearMenuPausa`, `CrearPantallaFinal`, `CrearEmboscada`); varios son `static` privados: cambiarlos a `internal` en lugar de copiarlos. `CrearBaseNivel` hoy usa los segmentos de Dolores: parametrizar la lista de fondos.
- Agregar `Escenas.Nivel2` en `CambioEscena.cs` y la escena a Build Settings. `GestorNivel` ya recibe título, nombre y música por campos serializados.
- Dificultad mayor que el Nivel 1: más enemigos combinados, menos curación.
- Reservar: un tramo para la **estampida de nahuales** (#53, Noé) y la **arena final** para el Jinete (#56, Noé), igual que la plaza de la parroquia en el Nivel 1 (sin obstáculos en los últimos ~21 u).
- Coordinar con Noé antes de tocar archivos compartidos: `ConstructorJuego.cs`, `BibliotecaAudio.cs` (enum `Sfx`, agregar solo al final), `CambioEscena.cs`, `EditorBuildSettings.asset`, `Physics2DSettings.asset`, `TagManager.asset`.

---

## 8. Cómo pedirle trabajo a la IA

1. Pegar este documento completo.
2. Indicar la issue (número y criterios de aceptación del tablero).
3. Pedir que lea primero los scripts relacionados antes de escribir código, que siga el estilo existente (nombres y comentarios en español, `[SerializeField]` privados, comentarios cortos que explican el porqué) y que agregue o ajuste pruebas de PlayMode.
4. Si toca un nivel o prefab, que lo haga en el constructor del editor, no a mano en la escena.
5. Probar en Unity, descartar el ruido de regeneración y abrir PR a `develop` con "Closes #N".
