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

Lo que **ya existe**: Mateo se mueve, salta (con coyote time y salto variable), evade y lanza Talavera con el mouse; hay enemigos de prueba que reciben daño; el Nivel 1 tiene un mapa continuo de cinco segmentos con su fondo; hay pruebas automáticas del movimiento y el disparo.

Lo que **todavía no**: oleadas y jefes, el resto de armas y consumibles, HUD, música y sonido, pantalla de título y los demás niveles.

## Cómo ejecutarlo

Requisitos: **Unity 6.6 (6000.6.0f1)**, con soporte para 2D/URP.

1. Clona el repositorio y ábrelo desde Unity Hub (*Add project from disk*).
2. Espera a que Unity importe los assets.
3. Abre `Assets/Scenes/Sandbox.unity` y presiona Play.

Si cambias sprites o scripts del Sprint 1, el menú **Alfarero > Sprint 1 > Construir Sandbox** regenera los prefabs, animaciones y la escena.

Pruebas automáticas: *Window > General > Test Runner > PlayMode*.

## Estructura del proyecto

```
Assets/
  Art/Sprites/        Personajes, enemigos, proyectiles, items y fondos
  Art/Animations/     Animaciones y Animator de Mateo
  Prefabs/            Alfarero, proyectil, impacto y enemigo de prueba
  ScriptableObjects/  Datos de las cerámicas (daño, velocidad, cadencia)
  Scenes/             Sandbox (escena de pruebas del Nivel 1)
  Scripts/            Player, Combat, Core y herramientas de editor
  Tests/PlayMode/     Pruebas automáticas
Docs/ARQUITECTURA.md  Cómo está organizado el código y el flujo de datos
```

Más detalle en [Docs/ARQUITECTURA.md](Docs/ARQUITECTURA.md) y en [SETUP.md](SETUP.md).

## Equipo

- **Omar Salinas** ([@omarsalinas3](https://github.com/omarsalinas3))
- **Noé Chavero Martínez** ([@SekiroK5](https://github.com/SekiroK5))

## Nota sobre el arte

Los sprites y fondos se generaron con herramientas de inteligencia artificial (Google Flow) y se procesaron para el juego (recorte, fondo transparente y unión de segmentos). Es arte provisional pensado para el proyecto académico.
