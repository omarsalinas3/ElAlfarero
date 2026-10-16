# El Alfarero de Dolores

Videojuego 2D de acción *run and gun* de desplazamiento lateral (al estilo Metal Slug) ambientado en un Guanajuato sobrenatural y tétrico. Proyecto de la materia Creación de Videojuegos (10.º cuatrimestre), desarrollado en **Unity 6.6** para **PC**.

> Estado actual: **Sprint 1 de 6** completado (base jugable). Todavía no es un juego completo; ver [Hasta dónde llega el proyecto](#hasta-dónde-llega-el-proyecto).

## ¿De qué trata?

Mateo es un alfarero de Dolores Hidalgo. En una noche de luna roja, **El Charro Negro** emerge del inframundo y secuestra a su esposa Carmen. Mateo toma sus piezas de alfarería consagradas como armas arrojadizas y viaja por pueblos fantasma, camposantos y túneles coloniales para enfrentar a las leyendas del Bajío, rescatar a Carmen y traer de vuelta el amanecer.

El juego mezcla acción arcade frenética con folclore y leyendas de Guanajuato: La Llorona, el Jinete sin Cabeza, la Bruja de Fuego y el Charro Negro, todo con arte pixel art y objetos de la cultura local (Talavera, barro negro, la guacamaya, la caguama).

Público objetivo: jugadores de 12 años en adelante y adultos nostálgicos de la acción arcade con temática mexicana.

## Cómo se juega

- **Armas de alfarería:** en lugar de balas, Mateo lanza cerámica. Cada tipo funciona distinto:
  - **Talavera de Dolores:** ráfaga rápida y precisa.
  - **Barro Negro:** granada de arco pesado que estalla en fuego y metralla.
  - **Barro Vidriado:** disparo en abanico tipo escopeta.
  - **Maceta Escudo:** se equipa como escudo y bloquea proyectiles hasta romperse (5 golpes).
- **Consumibles:** La Guacamaya (restaura salud) y La Caguama (invencibilidad temporal de 5 segundos).
- **Enemigos y jefes:** mineros esqueleto en patrullas, la Bruja de Fuego (ataca desde el aire), el Jinete sin Cabeza (minijefe de carga), La Llorona (jefa) y El Charro Negro (jefe final).
- **Niveles:** Dolores Hidalgo de noche, Mineral de Pozos (pueblo fantasma), el Panteón de Santa Paula, la Presa de la Olla y los Túneles de Guanajuato. Termina con una escena de reencuentro al amanecer.

### Controles actuales

| Acción | Tecla |
|---|---|
| Mover | A / D o flechas |
| Saltar | Espacio o W |
| Evadir (invulnerable un instante) | Shift izquierdo |
| Apuntar | Mouse |
| Lanzar cerámica | Clic izquierdo (mantener) |
| Beber caguama (invencible 5 s) | E o Q |
| Pausa | Esc o P |
| Menús | Mouse o flechas + Enter |

## Hasta dónde llega el proyecto

El plan son **6 sprints de 2 semanas** (12 semanas). Estado:

| Sprint | Enfoque | Estado |
|---|---|---|
| 1 | Núcleo: movimiento, salto, evasión, disparo de Talavera, vida y daño, escena de pruebas | **Hecho** |
| 2 | Arte y Nivel 1: Dolores Hidalgo, animación de Mateo, patrullas de mineros esqueleto | En curso (fondo del Nivel 1 ya integrado) |
| 3 | Armamento y drops: Barro Negro, Barro Vidriado, Maceta Escudo, Guacamaya, Caguama | Pendiente |
| 4 | IA de leyendas: Bruja de Fuego, Jinete sin Cabeza y escenario de Pozos | Pendiente |
| 5 | Jefes: La Llorona y El Charro Negro | Pendiente |
| 6 | Integración: balance, HUD de barro, pantalla de título, cinemática final y empaquetado | Pendiente |

**Meta final:** publicar el juego en [itch.io](https://itch.io) con una página temática (Día de Muertos y alfarería), con un nivel jugable por zona y los jefes de leyenda.

Lo que **ya existe**:
- **Flujo completo de juego:** pantalla de título con "presiona cualquier tecla", menú principal (Jugar, Controles, Opciones de volumen, Salir), intro de la historia con viñetas y texto, y el Nivel 1 jugable de principio a fin.
- **Nivel 1 (Dolores Hidalgo):** mapa continuo de cinco segmentos con letrero "MISIÓN 1 · ¡A LA CARGA!", huacales y hornos de barro que se rompen y sueltan monedas, Guacamayas y Caguamas, macetas que caen de lo alto, brasas que queman, mineros esqueleto que lastiman al tocarlos, y la meta frente a la parroquia.
- **HUD rústico:** jarrito de barro con barra de vida, caguamas guardadas con temporizador dorado de invencibilidad, munición de Talavera y monedas.
- **Pausa, game over y misión cumplida** con resumen (enemigos, monedas, tiempo), reintentar y volver al menú.
- **Sonido:** música de menú, intro y nivel, más efectos (salto, lanzar, cerámica rota, golpes, pickups, menús, jingles).
- **Pruebas automáticas** del movimiento, el disparo, la pausa, el game over, la caguama, los pickups, los huacales, la meta y el menú.

Lo que **todavía no**: IA de enemigos (por ahora están quietos), oleadas, jefes, Barro Negro / Barro Vidriado / Maceta Escudo y los demás niveles.

## Cómo ejecutarlo

Requisitos: **Unity 6.6 (6000.6.0f1)**, con soporte para 2D/URP.

1. Clona el repositorio y ábrelo desde Unity Hub (*Add project from disk*).
2. Espera a que Unity importe los assets.
3. Abre `Assets/Scenes/MenuPrincipal.unity` y presiona Play (o `Nivel1_Dolores.unity` para ir directo al nivel).

Si cambias sprites, sonidos o scripts, el menú **Alfarero > Construir juego completo** regenera prefabs, la biblioteca de audio y las escenas (menú, intro, Nivel 1 y Sandbox). `Sandbox.unity` sigue siendo la escena de pruebas del Sprint 1.

Los archivos de audio (`.wav`) se suben con **Git LFS**: corre `git lfs install` una vez en cada computadora antes de clonar o hacer commit.

Pruebas automáticas: *Window > General > Test Runner > PlayMode*.

## Estructura del proyecto

```
Assets/
  Art/Sprites/        Personajes, enemigos, proyectiles, items, obstáculos, HUD, intro y fondos
  Art/Fonts/          Fuente pixel Press Start 2P (licencia OFL)
  Art/Animations/     Animaciones y Animator de Mateo
  Audio/              Música (Music) y efectos (SFX)
  Prefabs/            Alfarero, proyectiles, obstáculos, pickups y efectos
  Resources/          BibliotecaAudio (qué archivo suena para cada efecto/música)
  ScriptableObjects/  Datos de las cerámicas (daño, velocidad, cadencia)
  Scenes/             MenuPrincipal, Intro, Nivel1_Dolores y Sandbox (pruebas)
  Scripts/            Core, Player, Combat, Enemies, UI y herramientas de editor
  Tests/PlayMode/     Pruebas automáticas
Docs/ARQUITECTURA.md  Cómo está organizado el código y el flujo de datos
```

Más detalle en [Docs/ARQUITECTURA.md](Docs/ARQUITECTURA.md) y en [SETUP.md](SETUP.md).

## Equipo

- **Omar Salinas** ([@omarsalinas3](https://github.com/omarsalinas3))
- **Noé Chavero Martínez** ([@SekiroK5](https://github.com/SekiroK5))

## Nota sobre el arte y el sonido

- Los sprites y fondos se generaron con herramientas de inteligencia artificial (Google Flow) y se procesaron para el juego (recorte, fondo transparente y unión de segmentos). Las viñetas de la intro combinan esos fondos y personajes.
- La música (menú, intro y Nivel 1) se generó con Google Flow Music.
- Los efectos de sonido y jingles se sintetizaron por código para este proyecto.
- Fuente: [Press Start 2P](https://fonts.google.com/specimen/Press+Start+2P) de CodeMan38, licencia SIL Open Font License (ver `Assets/Art/Fonts/PressStart2P-OFL.txt`).

Es material provisional pensado para el proyecto académico.
