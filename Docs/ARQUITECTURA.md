# Arquitectura — El Alfarero de Dolores (Sprint 1)

Unity 6.6 (6000.6.0f1) · 2D · Input System (teclado + mouse) · escena de pruebas: `Assets/Scenes/Sandbox.unity`

## 1. Cómo regenerar el Sandbox
Menú **Alfarero > Sprint 1 > Construir Sandbox** (o `Unity -batchmode -quit -executeMethod ConfigurarSprint1.Construir`).
Crea capas, material sin fricción, animaciones de Mateo, prefabs, `Ceramica_Talavera` y la escena. Se puede repetir sin miedo cuando cambien los sprites.
Para probar: abrir `Sandbox` y darle Play. Pruebas automáticas: Window > General > Test Runner > PlayMode.

## 2. Carpetas
```
Assets/
  Art/Sprites/{Player,Enemies,Allies,Projectiles,Items,Environment,UI,Bosses}   PNG ya recortados, sin fondo magenta
  Art/Animations/                  Clips y Animator de Mateo (generados)
  Prefabs/{Player,Enemies,Projectiles,...}
  ScriptableObjects/Ceramicas/     Una cerámica = un asset (daño, velocidad, cadencia...)
  Scenes/                          Sandbox (Sprint 1); niveles reales después
  Scripts/Core                     Cámara, HUD de depuración
  Scripts/Player                   Movimiento, disparo, animación
  Scripts/Combat                   Health, Projectile, CeramicaData, efectos
  Scripts/Enemies                  (Sprint 2/4)
  Scripts/Editor                   Herramientas de editor (solo Editor)
  Settings/                        Materiales de física, etc.
  Tests/PlayMode/                  Pruebas automáticas
_ArtSource/Modelos/                Imágenes originales del zip (fuera de Assets, Unity no las importa)
Docs/                              Este documento
```

## 3. Ensamblados (asmdef)
| Ensamblado | Contenido | Referencias |
|---|---|---|
| `Alfarero.Runtime` | Todo `Assets/Scripts` salvo `Editor` | Unity.InputSystem |
| `Alfarero.Editor` | `Assets/Scripts/Editor` (solo Editor) | Runtime |
| `Alfarero.Tests.PlayMode` | `Assets/Tests/PlayMode` | Runtime, TestRunner |

Los scripts están en el espacio de nombres global y con nombres en español para campos/métodos (convención del equipo); los tipos base conservan nombres en inglés (`PlayerController`, `Health`, `Projectile`).

## 4. Capas
| # | Capa | Para qué |
|---|---|---|
| 6 | Ground | Suelo, plataformas, muros. Lo usa el chequeo de suelo y el impacto de proyectiles |
| 7 | Player | Mateo |
| 8 | Enemy | Enemigos y dummies |
| 9 | Projectile | Proyectiles (no chocan entre sí ni con Player) |

## 5. Flujo de datos
```
Input System (código)         ┌────────────────┐
 A/D · Espacio · Shift ─────► │PlayerController│── EstaEnSuelo / EstaEvadiendo / DireccionMirada ─► PlayerAnimator ─► Animator
 Mouse (posición, clic)       └───────▲────────┘
        │                             │ DireccionApuntado
        └──────────────────────► PlayerShooter ──Instantiate──► Projectile ──OnTriggerEnter2D──► Health.RecibirDano
                                      │ lee                          ▲ datos                          │ eventos
                                      ▼                              │                              ▼
                               CeramicaData (ScriptableObject) ──────┘                    ParpadeoDano / HUD / IA
                               (daño, velocidad, cadencia, capas, prefab proyectil, prefab impacto)
```
Reglas:
- **Health no sabe quién lo golpea**: solo expone `OnVidaCambiada` y `OnMuerte`. HUD, parpadeo, animaciones y IA se suscriben.
- **Nada de números mágicos de armas en código**: todo sale de `CeramicaData`. Barro Negro y Barro Vidriado (Sprint 3) son nuevos assets + prefab, no nuevas clases (Barro Vidriado usa `proyectilesPorDisparo` y `dispersionGrados`).
- **El Animator solo lee estado**: `PlayerAnimator` copia `Velocidad`, `EnSuelo`, `Evadiendo`; no decide nada.
- **Input por código** (`InputAction` en `Awake`): no hay `.inputactions`; para cambiar teclas se edita `PlayerController`/`PlayerShooter`.

## 6. Escala del mundo y arte
- Cámara ortográfica de **6** (12 unidades de alto, ~21.3 de ancho a 16:9).
- Mateo ≈ 2.6 unidades. El suelo (adoquín del fondo) está en **y = -4.5**.
- `ImportadorSprites` aplica solo: Sprite, filtro Point, sin compresión, PPU por carpeta (personajes 280, proyectiles 400, impacto 250, items 300, fondos 128). Pivote en los pies para Player/Enemies/Allies/Bosses.
- El nivel del Sandbox son 4 pantallas del fondo de Dolores de noche (la 2.ª y 4.ª espejadas para ocultar la costura). Cuando haya parallax real se sustituye `Fondo` por capas.

## 7. Valores de juego (Sprint 1)
| Parámetro | Valor |
|---|---|
| Velocidad máxima / aceleración / frenado | 8 / 80 / 100 |
| Salto | 14 u/s, gravedad ×3, caída ×1.8, corte al soltar 0.5, coyote 0.1 s, buffer 0.12 s |
| Evasión | 14 u/s, 0.2 s, cooldown 0.6 s, invulnerable |
| Talavera | 10 de daño, 18 u/s, 8 disparos/s, alcance 1.5 s, dispersión ±2° |

## 8. Pendiente / decisiones abiertas
- El GDD pide disparo en **8 direcciones**; el andamiaje ya apunta en 360° con el mouse. Se mantiene el mouse (Sprint 1) y se puede discretizar a 8 direcciones si se prefiere un control más "arcade".
- Mateo tiene 5 cuadros de carrera en el arte actual (el catálogo dice 6) y un solo cuadro de idle/salto/evasión reciclado del ciclo de carrera. Pendiente de arte.
- Object pooling para proyectiles (Sprint 4/6, ya anotado en `Projectile`).
- Pixel Perfect Camera: requiere URP 2D; el arte actual es de alta resolución (no pixel exacto), por eso no se usa todavía.
