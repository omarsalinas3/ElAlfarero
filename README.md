# El Alfarero de Dolores

Videojuego 2D de acción *run and gun* de desplazamiento lateral (al estilo Metal Slug) ambientado en un Guanajuato sobrenatural y tétrico. Proyecto de la materia Creación de Videojuegos (10.º cuatrimestre), desarrollado en **Unity 6.6** para **PC** (Windows y navegador).

> Estado actual: **Sprint 3 de 6** (niveles 1, 2 y 3 jugables). Todavía no es un juego completo; ver [Hasta dónde llega el proyecto](#hasta-dónde-llega-el-proyecto). Manual: [Docs/MANUAL_USUARIO.md](Docs/MANUAL_USUARIO.md) · GDD: [Docs/GDD.md](Docs/GDD.md).

## ¿De qué trata?

Mateo es un alfarero de Dolores Hidalgo. En una noche de luna roja, **El Charro Negro**, líder de una banda de bandidos sobrenaturales, emerge del inframundo y **secuestra a Carmen**, la esposa de Mateo. Con la luna roja el Charro hechiza a las leyendas del Bajío para que le cierren el paso.

Mateo toma sus piezas de alfarería **consagradas** como armas arrojadizas: la Talavera bendita rompe los hechizos. Cruza Guanajuato enfrentando a los bandidos del Charro y a las leyendas hechizadas para rescatar a Carmen y traer de vuelta el amanecer.

El juego mezcla acción arcade frenética con folclore y leyendas de Guanajuato (el Alebrije, la Bruja de Fuego, los nahuales, el Jinete sin Cabeza, La Llorona y el Charro Negro), todo en pixel art y con objetos de la cultura local (Talavera, barro negro, la guacamaya, la caguama).

Público objetivo: jugadores de 12 años en adelante y adultos nostálgicos de la acción arcade con temática mexicana.

## Niveles y jefes

| # | Nivel | Enemigos | Evento especial | Jefe |
|---|---|---|---|---|
| 1 | **Dolores Hidalgo** (calles y parroquia de noche) | Mineros esqueleto, bandidos del Charro (revólver, rifle, escopeta y machete) | Emboscada de bandidos en oleadas | **Alebrije Gigante**: sale de entre las torres de la parroquia. Al vencerlo se rompe el hechizo y se vuelve la **montura** de Mateo |
| 2 | **Mineral de Pozos** (pueblo fantasma y ruinas mineras) | Bandidos, mineros esqueleto, **Bruja de Fuego** | **Estampida de nahuales** en la cañada y emboscada de 3 oleadas | **Jinete sin Cabeza**, en el patio de la capilla en ruinas |
| 3 | **Panteón junto al río** | Esqueletos, bandidos y Brujas de Fuego | Emboscada de 3 oleadas en las capillas | **La Llorona** (ataques sónicos y de agua), en la orilla del río |
| 4 | **Túneles de Guanajuato** (bajo la ciudad, de la boca del túnel a la guarida del Charro) | Bandidos, mineros esqueleto y Brujas de Fuego | **Derrumbe de mina** en la galería de vigas y emboscada de 4 oleadas | **El Charro Negro** (jefe final); rescate de Carmen al amanecer |

La dificultad sube de nivel en nivel: más enemigos y más resistentes, ataques desde el aire y menos curación.

### Modificaciones respecto al GDD

El GDD original solo daba ejemplos de jefes ("figuras icónicas como La Llorona, El Charro Negro y Alebrijes Gigantes") y proponía cinco zonas. Durante el Sprint 2 (22 de octubre) el equipo acordó estos cambios (la tabla completa de mecánicas, con la justificación de cada cambio, está en [Docs/MECANICAS.md](Docs/MECANICAS.md)):

| Cambio | Antes (GDD) | Ahora | Por qué |
|---|---|---|---|
| Número de niveles | 5 zonas (Dolores, Pozos, Panteón de Santa Paula, Presa de la Olla, Túneles) | 4 niveles | El alcance cabe en 12 semanas con un jefe por nivel; el panteón y la presa se unen en "Panteón junto al río" |
| Jinete sin Cabeza | Minijefe de carga | Jefe del Nivel 2 | Cada nivel necesita un jefe y la leyenda encaja con el pueblo fantasma de Pozos |
| Alebrije Gigante | Jefe que se destruye | Jefe que, liberado del hechizo, se vuelve montura | Da una mecánica nueva (montar) y explica el viaje de Dolores a Pozos |
| Charro Negro | Villano que secuestra a Carmen | Además, líder de los bandidos y quien hechiza a las leyendas | Une a todos los enemigos en una sola historia |
| Nahuales | Sin uso definido | Estampida como evento especial del Nivel 2 | Da el "evento especial" del GDD sin ser otro jefe |

## Cómo se juega

- **Armas de alfarería:** en lugar de balas, Mateo lanza cerámica. Cada tipo funciona distinto:
  - **Talavera de Dolores:** ráfaga rápida y precisa (munición infinita).
  - **Barro Negro:** granada de arco pesado que estalla en fuego y metralla.
  - **Barro Vidriado:** disparo en abanico tipo escopeta.
  - **Maceta Escudo:** se equipa como escudo y bloquea proyectiles hasta romperse.
- **Consumibles:** La Guacamaya (restaura salud) y La Caguama (invencibilidad temporal de 5 segundos).
- **Montura:** tras vencer al Alebrije, Mateo puede montarlo: corre más rápido, salta más alto y ataca con zarpazos. La montura tiene su propia vida.

### Controles actuales

| Acción | Tecla |
|---|---|
| Mover | A / D o flechas |
| Saltar | Flecha arriba o W |
| Evadir (invulnerable un instante) | Shift izquierdo |
| Lanzar cerámica | Espacio o clic izquierdo (mantener) |
| Apuntar | Mouse, o flechas para lanzar al frente |
| Apuntar hacia arriba | Espacio + flecha arriba |
| Montar / desmontar al Alebrije | F |
| Beber caguama (invencible 5 s) | E o Q |
| Pausa | Esc o P |
| Diálogos | Enter adelanta, Retroceso salta |
| Menús | Mouse o flechas + Enter |

Con **gamepad**: stick izquierdo mueve, A salta, B evade, X o RT lanza, stick derecho apunta, Y bebe caguama, LB/RB cambia de arma, Start pausa. En **pantallas táctiles** aparecen un joystick y botones. Detalle en el [manual](Docs/MANUAL_USUARIO.md#3-controles).

## Hasta dónde llega el proyecto

El proyecto se divide en **6 sprints**, cada uno cierra con una entrega del curso. La entrega final es el **viernes 27 de noviembre**. Estado:

| Sprint | Fechas | Enfoque | Estado |
|---|---|---|---|
| 1 | 1–14 oct | Núcleo: movimiento, salto, evasión, disparo de Talavera, vida y daño, menú, intro y Nivel 1 base | **Hecho** |
| 2 | 15–29 oct | Nivel 1 completo (IA, emboscada, Alebrije), guardado, HUD, splash, Nivel 2 (Mineral de Pozos), tutorial | **Hecho** |
| 3 | 29 oct–6 nov | Armas nuevas, combo, secretos, Bruja de Fuego, opciones, créditos, efectos, narrativa, gamepad y táctil, resoluciones, Nivel 3 con La Llorona, GDD y manual | **Cerrando** (Avance 3) |
| 4 | 6–13 nov | Nivel 4, El Charro Negro, derrumbe de mina, final y narrativa entre niveles | **En curso** (Avance 4) |
| 5 | 13–20 nov | Builds de Web y Móvil, pruebas y corrección de errores | Pendiente (Avance 5) |
| 6 | 20–27 nov | Balance, juego terminado en las 3 plataformas, documentación y presentación | Pendiente (Final) |

**Meta final:** publicar el juego en [itch.io](https://itch.io) con un nivel jugable por zona y los jefes de leyenda.

Lo que **ya existe**:
- **Flujo completo:** splash con el logo del equipo, menú principal (Jugar, Continuar, Opciones, Créditos, Salir), intro con viñetas, niveles 1, 2 y 3 jugables, pausa, game over y misión cumplida con récord.
- **Nivel 1 (Dolores Hidalgo):** tutorial al inicio, mineros esqueleto animados, bandidos con IA (revólver, rifle, escopeta y machete), emboscada en oleadas y el Alebrije Gigante con su cinemática.
- **Nivel 2 (Mineral de Pozos):** pueblo fantasma de noche, Brujas de Fuego, emboscada de 3 oleadas y estampida de nahuales.
- **Nivel 3 (Panteón junto al río):** 5 tramos con neblina, emboscada de 3 oleadas, Brujas de Fuego, secretos y La Llorona con su propia música de pelea.
- **Nivel 4 (Túneles de Guanajuato):** 5 tramos de túneles, el recorrido más difícil (emboscada de 4 oleadas y poca curación), **derrumbe de mina** con rocas que avisan con su sombra, 3 exvotos y la guarida libre para la pelea final.
- **Guardado:** nivel desbloqueado, récord y mejor tiempo por nivel, monedas y armas.
- **HUD de barro:** vida, caguamas, puntaje, tiempo y objetivo con barra de progreso.
- **Sonido:** música por pantalla y nivel, y efectos para cada acción.
- **Pruebas automáticas** de PlayMode del movimiento, el disparo, la IA, los niveles, el guardado, los menús y el rendimiento.

- **Narrativa:** diálogos con retrato (Don Toribio, Carmen y el Charro Negro) al empezar cada nivel y cinemáticas de viñetas entre todos los niveles (al liberar a La Llorona el río baja y aparece la entrada a los túneles).
- **Controles:** teclado y mouse, gamepad completo y controles táctiles en pantalla; las ayudas muestran el botón del dispositivo que se esté usando.
- **Interfaz adaptable** a 16:9, 16:10, 4:3, 21:9 y celulares (Safe Area), con partículas, *hit stop* y créditos con licencias ([Docs/CREDITOS.md](Docs/CREDITOS.md)).

Lo que **todavía no**: la pelea con El Charro Negro (el Nivel 4 ya se recorre completo y por ahora termina al llegar a su guarida), el final y el build para móvil. La comparación completa con el GDD (qué está implementado, qué cambió y por qué, y qué falta) está en [Docs/MECANICAS.md](Docs/MECANICAS.md).

## Cómo ejecutarlo

Requisitos: **Unity 6.6 (6000.6.0f1)**, con soporte para 2D/URP.

1. Instala Git LFS una vez (`git lfs install`), clona el repositorio y corre `git lfs pull` (si no, los audios llegan vacíos).
2. Ábrelo desde Unity Hub (*Add project from disk*) y espera a que importe los assets.
3. Abre `Assets/Scenes/Splash.unity` y presiona Play (o `Nivel1_Dolores.unity` / `Nivel2_Pozos.unity` para ir directo a un nivel).

Las escenas y prefabs **se generan por código**: si cambias sprites, sonidos o scripts, el menú **Alfarero > Construir juego completo** regenera prefabs, la biblioteca de audio y todas las escenas. No edites las escenas a mano: el constructor las sobrescribe. `Sandbox.unity` es la escena de pruebas del Sprint 1.

Pruebas automáticas: *Window > General > Test Runner > PlayMode*.

## Estructura del proyecto

```
Assets/
  Art/Sprites/        Personajes, enemigos, jefes, proyectiles, items, obstáculos, HUD, intro y fondos
  Art/Fonts/          Fuente pixel Press Start 2P (licencia OFL)
  Art/Animations/     Animaciones y Animators (generados por código)
  Audio/              Música (Music) y efectos (SFX), por Git LFS
  Prefabs/            Alfarero, enemigos, jefes, proyectiles, obstáculos, pickups y efectos
  Resources/          BibliotecaAudio (qué archivo suena para cada efecto/música)
  ScriptableObjects/  Datos de las cerámicas (daño, velocidad, cadencia)
  Scenes/             Splash, MenuPrincipal, Intro, Nivel1_Dolores, Nivel2_Pozos y Sandbox (pruebas)
  Scripts/            Core, Player, Combat, Enemies, UI y Editor (constructores)
  Tests/PlayMode/     Pruebas automáticas
Docs/                 ARQUITECTURA.md, BITACORA.md, contexto del proyecto y originales del arte
```

Más detalle en [Docs/ARQUITECTURA.md](Docs/ARQUITECTURA.md), [Docs/BITACORA.md](Docs/BITACORA.md), [Docs/MECANICAS.md](Docs/MECANICAS.md) (mecánicas del GDD y modificaciones justificadas) y [SETUP.md](SETUP.md).

## Equipo

- **Omar Salinas** ([@omarsalinas3](https://github.com/omarsalinas3))
- **Noé Chavero Martínez** ([@SekiroK5](https://github.com/SekiroK5))

## Nota sobre el arte y el sonido

- Los sprites y fondos se generaron con herramientas de inteligencia artificial (Google Flow) y se procesaron para el juego (recorte, fondo transparente y unión de segmentos). Las viñetas de la intro combinan esos fondos y personajes. Los originales están en `Docs/Arte/Flow/`.
- La música se generó con Google Flow Music.
- Los efectos de sonido y jingles se sintetizaron por código para este proyecto.
- Fuente: [Press Start 2P](https://fonts.google.com/specimen/Press+Start+2P) de CodeMan38, licencia SIL Open Font License (ver `Assets/Art/Fonts/PressStart2P-OFL.txt`).

Es material provisional pensado para el proyecto académico.
