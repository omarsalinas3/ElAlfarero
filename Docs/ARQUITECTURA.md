# Arquitectura — El Alfarero de Dolores

Unity 6.6 (6000.6.0f1) · 2D con URP · Input System (teclado + mouse) · UI con uGUI (Text) y fuente Press Start 2P.

## 1. Escenas y cómo regenerarlas
| Escena | Qué es |
|---|---|
| `MenuPrincipal` | Portada con "presiona cualquier tecla", menú (Jugar, Controles, Opciones, Salir) |
| `Intro` | Viñetas de la historia con texto letra por letra (Enter avanza, Esc salta) |
| `Nivel1_Dolores` | Nivel jugable: obstáculos, pickups, enemigos, HUD, pausa, game over, victoria |
| `Sandbox` | Escena de pruebas del Sprint 1 (la usan las pruebas automáticas) |

Todo se genera con **Alfarero > Construir juego completo** (o `Unity -batchmode -quit -executeMethod ConstructorJuego.Construir`).
`ConstructorJuego` llama primero a `ConfigurarSprint1` (capas, Mateo, Talavera, Sandbox) y luego arma biblioteca de audio, prefabs de obstáculos/pickups, las tres escenas del juego y el Build Settings. Se puede repetir: sobrescribe lo generado.
Pruebas: Window > General > Test Runner > PlayMode (`Sprint1Tests` y `JuegoTests`).

## 2. Carpetas
```
Assets/
  Art/Sprites/{Player,Enemies,Allies,Projectiles,Items,Obstacles,Environment,UI,Intro}
  Art/Fonts/                 Press Start 2P (OFL)
  Art/Animations/            Clips y Animator de Mateo (generados)
  Audio/Music, Audio/SFX     menu/intro/nivel1.wav y efectos sintetizados
  Resources/BibliotecaAudio  Tabla Sfx/Musica -> archivo (la carga GestorAudio)
  Prefabs/{Player,Enemies,Projectiles,Obstacles,Pickups,Effects}
  ScriptableObjects/Ceramicas/
  Scenes/
  Scripts/Core               GestorAudio, CambioEscena, GestorNivel, MetaNivel, cámara
  Scripts/Player             Movimiento, disparo, animación, inventario (caguamas/monedas), reacción al daño
  Scripts/Combat             Health, Projectile, CeramicaData, DanoPorContacto, Destructible, Recogible, MacetaQueCae
  Scripts/Enemies            EnemigoBasico (sin IA todavía)
  Scripts/UI                 HudJuego, MenuPausa, PantallaFinal, BannerMision, MenuPrincipal, SecuenciaIntro, Panel...
  Scripts/Editor             ConstructorJuego, ConfigurarSprint1, ImportadorSprites
  Tests/PlayMode/
Docs/                        Este documento
```

## 3. Ensamblados (asmdef)
| Ensamblado | Contenido | Referencias |
|---|---|---|
| `Alfarero.Runtime` | Todo `Assets/Scripts` salvo `Editor` | Unity.InputSystem |
| `Alfarero.Editor` | `Assets/Scripts/Editor` (solo Editor) | Runtime |
| `Alfarero.Tests.PlayMode` | `Assets/Tests/PlayMode` | Runtime, TestRunner |

Espacio de nombres global; campos y métodos en español (convención del equipo).

## 4. Capas
| # | Capa | Para qué |
|---|---|---|
| 6 | Ground | Suelo, muros, huacales y hornos (se pueden pisar y los proyectiles los golpean) |
| 7 | Player | Mateo |
| 8 | Enemy | Mineros esqueleto |
| 9 | Projectile | Proyectiles (no chocan entre sí ni con Player) |
| 0 | Default | Pickups, brasas, zona de la maceta, meta (triggers que los proyectiles ignoran) |

## 5. Flujo de datos
```
PlayerController ──estado──► PlayerAnimator ──► Animator
PlayerShooter ──► Projectile ──► Health.RecibirDano ──eventos──► ParpadeoDano / ReaccionDanoJugador / HUD / Destructible / EnemigoBasico
DanoPorContacto (brasas, maceta, esqueleto) ──► Health + ReaccionDanoJugador.Empujar
Recogible ──► InventarioJugador (Comer / AgregarCaguamas / AgregarMonedas)
InventarioJugador.BeberCaguama ──► Health.HacerInvulnerable(5 s)
Health(jugador).OnMuerte ──► GestorNivel ──► PantallaFinal (GAME OVER)
MetaNivel ──► GestorNivel.Completar ──► PantallaFinal (MISIÓN CUMPLIDA)
Esc/P ──► GestorNivel.Pausar ──► Time.timeScale = 0 + MenuPausa
Cualquier script ──► GestorAudio.Reproducir(Sfx.X) / CambiarMusica(Musica.X)
```
Reglas:
- **Health no sabe quién lo golpea**: expone `OnDano`, `OnVidaCambiada`, `OnMuerte`. Invulnerabilidad: `Invulnerable` (evasión, mientras dure) y `HacerInvulnerable(seg)` (golpe recibido, caguama).
- **GestorNivel decide, la UI solo muestra**: pausa, game over y victoria cambian `ControlActivo` del jugador; las pantallas llaman `Reintentar()` / `IrAlMenu()`.
- **Sonido por nombre**: el código pide `Sfx`/`Musica`; qué archivo suena se define en `BibliotecaAudio` (regenerada por el constructor según los archivos de `Audio/`).
- **Escenas con fundido**: `CambioEscena.Cargar(Escenas.X)` (siempre deja `Time.timeScale = 1`).
- **Input por código** (`InputAction` en `Awake`), sin `.inputactions`. La UI usa `InputSystemUIInputModule`.

## 6. Escala del mundo y arte
- Cámara ortográfica de **6** (12 unidades de alto). Mateo ≈ 2.6 u. Suelo en **y = -4.5**.
- `ImportadorSprites`: Sprite, filtro Point, sin compresión. PPU: personajes/obstáculos 280, proyectiles 400, impacto 250, items 300, fondos 128, UI/intro 100. Pivote en los pies para personajes y obstáculos. Botones y paneles de UI con 9-slice.
- Fondo del Nivel 1: 5 segmentos (2752×1536) traslapados con degradado en el borde izquierdo (`Segmentos` en `ConfigurarSprint1`).
- UI: Canvas 1920×1080 (escala con la pantalla). HUD orden 10, pausa 30, game over 40, victoria 41, fundido de escena 1000.

## 7. Valores de juego
| Parámetro | Valor |
|---|---|
| Movimiento | 8 u/s, aceleración 80, frenado 100 |
| Salto | 14 u/s, gravedad ×3, caída ×1.8, corte 0.5, coyote 0.1 s, buffer 0.12 s |
| Evasión | 14 u/s, 0.2 s, cooldown 0.6 s, invulnerable |
| Talavera | 10 de daño, 18 u/s, 8 disparos/s, alcance 1.5 s, dispersión ±2° |
| Vida de Mateo | 100; tras un golpe 1 s invulnerable con parpadeo y empujón |
| Caguama | máx. 3, invencible 5 s (E o Q) |
| Guacamaya | +35 de vida (no se recoge con la vida llena) |
| Huacal / Horno | 30 / 60 de vida, sueltan monedas, Guacamaya o Caguama |
| Daño por contacto | Esqueleto 10, brasas 15, maceta 15 |

## 8. Pendiente / decisiones abiertas
- IA de enemigos (Sprint 4): hoy los mineros están quietos y lastiman al tocarlos.
- Disparo en 8 direcciones del GDD: hoy se apunta en 360° con el mouse.
- Arte: 5 cuadros de carrera (el catálogo pide 6); idle/salto/evasión reciclan cuadros.
- Object pooling para proyectiles y efectos (Sprint 6).
- La UI usa `Text` (uGUI clásico) para no depender de los recursos de TextMeshPro; se puede migrar después.
