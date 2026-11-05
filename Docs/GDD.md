# Documento de Diseño de Juego (GDD) — El Alfarero de Dolores

*Versión 3 · actualizada al cierre del Sprint 3 (noviembre de 2026)*

*Equipo: Omar Salinas (@omarsalinas3) y Noé Chavero (@SekiroK5) · Creación de Videojuegos · UTNG · grupo GIDS6102 · profesor Gabriel Barrón*

> La versión 2 (septiembre de 2026) fue la propuesta de la Unidad 1. Esta versión conserva su estructura (concepción, diseño, planificación, desarrollo, pruebas y lanzamiento) y la actualiza con lo que de verdad tiene el juego y con los requisitos del curso. Los cambios respecto a la versión 2 se marcan con **(cambio)** y se justifican en [MECANICAS.md](MECANICAS.md).

---

## 1. Concepción de la idea

| Concepto | Descripción |
|---|---|
| **Título** | El Alfarero de Dolores |
| **Género** | Acción y disparos de desplazamiento lateral (*run and gun*), con un ritmo frenético al estilo Metal Slug |
| **Formato** | 2D, pixel art, un jugador |
| **Plataformas** | PC con Windows y **navegador (WebGL) en itch.io** **(cambio)**, con teclado y mouse, gamepad y pantalla táctil |
| **Público** | Jugadores de 12 años en adelante y adultos nostálgicos de la acción arcade con temática del folclore y las leyendas de México |
| **Equipo** | Omar Salinas y Noé Chavero |
| **Alcance** | 12 semanas (6 sprints de 2 semanas), del prototipo al pulido y la publicación |
| **Motor** | Unity 6.6 con Universal Render Pipeline 2D e Input System |

**Propuesta de valor:** la acción arcade de Metal Slug con identidad guanajuatense: en lugar de balas, piezas de alfarería (Talavera, Barro Negro, Barro Vidriado, una maceta como escudo), y en lugar de jefes genéricos, leyendas mexicanas.

---

## 2. Diseño del juego

### 2.1 Historia

Mateo es un alfarero de Dolores Hidalgo que vive en paz con su esposa Carmen. Una noche de luna roja, **El Charro Negro**, líder de una banda de bandidos sobrenaturales, emerge del inframundo y **secuestra a Carmen**. Con la luna roja el Charro hechiza a las leyendas del Bajío para que le cierren el paso a Mateo **(cambio)**.

Mateo toma sus piezas de alfarería **consagradas** (la Talavera bendita rompe los hechizos) y cruza Guanajuato para rescatarla: las calles de Dolores, el pueblo fantasma de Mineral de Pozos, un panteón junto al río y los túneles de Guanajuato, donde la rescata al amanecer.

**Narrativa en el juego:**
- **Intro** con viñetas ilustradas y texto letra por letra (se puede saltar).
- **Diálogos** con retrato de **Don Toribio** (el viejo alfarero que presenta la misión al inicio del Nivel 1), **Carmen** y **El Charro Negro**, en un cuadro que no detiene la acción y se puede adelantar o saltar.
- **Cinemática de transición** del Nivel 1 al Nivel 2: el alebrije liberado se une a Mateo, el Charro se lleva a Carmen al desierto y Mateo sigue sus huellas de fuego montado en el alebrije.
- **Cinemáticas de jefe** (Alebrije y Jinete) con cámara fija, letreros y sacudidas.

### 2.2 Niveles

| # | Nivel | Enemigos | Evento especial | Jefe | Estado |
|---|---|---|---|---|---|
| 1 | **Dolores Hidalgo** (calles y parroquia de noche) | Mineros esqueleto, bandidos (revólver, rifle, escopeta, machete) | Emboscada de 2 oleadas | **Alebrije Gigante**; al vencerlo se vuelve montura | Jugable |
| 2 | **Mineral de Pozos** (pueblo fantasma y ruinas mineras) | Bandidos, mineros esqueleto, **Bruja de Fuego** (voladora) | **Estampida de nahuales** y emboscada de 3 oleadas | **Jinete sin Cabeza** | Jugable |
| 3 | **Panteón junto al río** | Esqueletos y almas en pena | Por definir | **La Llorona** (ataques sónicos y de agua) | Escena base y fondos (Sprint 3); pelea en el Sprint 4 |
| 4 | **Túneles de Guanajuato** | Bandidos y esqueletos | Derrumbe de mina | **El Charro Negro** (montado, fuego infernal); rescate de Carmen | Sprint 5 |

**(cambio)** La versión 2 proponía cinco zonas; se ajustó a cuatro niveles con un jefe cada uno para que el alcance quepa en 12 semanas.

**Dificultad progresiva:** cada nivel suma enemigos más resistentes y combinados, ataques desde el aire (Bruja), eventos más largos (2 → 3 oleadas) y menos curación.

### 2.3 Mecánicas

#### Movimiento y combate
- Movimiento lateral rápido con aceleración y frenado.
- **Salto** de altura variable (soltar el botón corta el salto), *coyote time* y *buffer*.
- **Evasión:** impulso corto con invulnerabilidad; las balas atraviesan a Mateo.
- **Lanzar cerámica** manteniendo el botón (ráfaga según la cadencia del arma), apuntando con el mouse, el stick derecho o al frente; hacia arriba con Espacio + ↑ **(cambio: apuntado con teclado y gamepad)**.
- **Combo cuerpo a cuerpo** de 3 golpes cuando hay un enemigo pegado enfrente; el tercero empuja y sacude la cámara.

#### Sistema de cerámicas (armas)
| Arma | Comportamiento | Munición |
|---|---|---|
| Talavera de Dolores | Ráfaga rápida y precisa | Infinita **(cambio)** |
| Barro Negro | Granada en arco que explota en un radio **(cambio: en arco en lugar de corto alcance)** | 5 por caja |
| Barro Vidriado | Abanico de 5 fragmentos de corto alcance | 20 por caja |
| Maceta Escudo | Escudo frontal que detiene proyectiles hasta su 5.º golpe | 5 golpes |

Las armas salen de **cajas dentro de huacales** **(cambio)**; los **hornos** sueltan monedas y caguamas. Las estadísticas viven en *ScriptableObjects* (`CeramicaData`).

#### Consumibles y recogibles
- **Guacamaya:** cura. **Caguama:** se guarda (máx. 3) y da 5 s de invencibilidad. **Monedas:** puntos. Lo que sobra (vida llena, 3 caguamas) se cambia por monedas.
- **Exvotos:** 3 escondidos por nivel (muros agrietados, vigas, tiro de mina).

#### Montura
Al vencer al Alebrije, Mateo puede montarlo: +40 % de velocidad, +20 % de salto, zarpazo en lugar de lanzar y 60 de vida propia **(cambio)**.

#### Eventos especiales
- **Emboscadas:** la cámara se fija y entran oleadas de bandidos por ambos lados.
- **Estampida de nahuales:** manadas que cruzan la cañada con aviso; se esquivan saltando o evadiendo.
- **Derrumbe de mina** (Nivel 4).

### 2.4 Personajes y enemigos

| Tipo | Personajes |
|---|---|
| Protagonistas | **Mateo** (jugable) y **Carmen** |
| Aliados | **Don Toribio** (viejo alfarero, da los consejos) y el **Alebrije** liberado |
| Enemigos comunes | Bandidos del Charro (revólver, rifle, escopeta, machete), mineros esqueleto (las "calaveras animadas"), Bruja de Fuego, nahuales (como estampida) **(cambio)** |
| Jefes | Alebrije Gigante, Jinete sin Cabeza **(cambio: nuevo)**, La Llorona, El Charro Negro |

**IA:** los bandidos disparan a distancia con un aviso previo o pelean cuerpo a cuerpo; los esqueletos patrullan, persiguen, brincan obstáculos y embisten; la Bruja vuela, lanza bolas de fuego y se lanza en picada; los jefes tienen fases con ataques distintos.

### 2.5 Interfaz

- **Splash** con el logo del equipo → **título** → **menú principal**: Jugar, Continuar, Opciones (audio, gráficos, controles, borrar partida), Créditos y Salir.
- **HUD de barro:** jarrito de vida que se cuartea, barra de vida, caguamas, puntaje, tiempo, objetivo con barra de avance, arma con munición, monedas y barra del jefe.
- **Pausa** (continuar, reiniciar, opciones, menú), **Game Over** y **¡Misión cumplida!** con récord, mejor tiempo y exvotos.
- **Tutorial** con carteles en el Nivel 1 que muestran teclas o botones según el dispositivo.
- **Créditos** con autores, herramientas y licencias, con desplazamiento automático.
- **Interfaz adaptable:** todo se diseña en 1920×1080; en otras proporciones (16:10, 4:3, 21:9, celular) se conserva la zona de juego 16:9 con barras negras y el HUD queda dentro de la *Safe Area*.

### 2.6 Controles por plataforma

| Acción | Teclado y mouse | Gamepad | Táctil |
|---|---|---|---|
| Mover | A/D o flechas | Stick izquierdo o cruceta | Joystick virtual |
| Saltar | W o ↑ | A | SALTO |
| Evadir | Shift | B | EVADE |
| Lanzar / golpear | Espacio o clic | X o RT | LANZA |
| Apuntar | Mouse | Stick derecho | Al frente / arriba |
| Cambiar arma | 1-4 o rueda | LB / RB | ARMA |
| Caguama | E o Q | Y | CAGUAMA |
| Montar | F | LB | MONTA |
| Pausa | Esc o P | Start | II |

Los controles táctiles solo aparecen en pantallas táctiles. El detalle está en el [manual de usuario](MANUAL_USUARIO.md).

### 2.7 Retroalimentación visual y sonora
- Partículas de pixel: polvo al correr y aterrizar, fragmentos de cerámica, astillas de huacales, brillo al recoger y chispas en golpes fuertes.
- Sacudida de cámara y pausa breve (*hit stop*) en golpes fuertes, explosiones y daño a Mateo.
- Destello de daño en enemigos, fundidos entre escenas y botones de menú que entran escalonados.
- Música por pantalla y nivel, y un efecto de sonido para cada acción.

### 2.8 Guardado
Se guarda al ganar cada nivel en un JSON: nivel desbloqueado, puntaje máximo y mejor tiempo por nivel, monedas, armas desbloqueadas y exvotos encontrados. *Continuar* carga el último nivel desbloqueado.

---

## 3. Planificación (Scrum)

6 sprints de 2 semanas. **(cambio)** La versión 2 asignaba un responsable por sprint; en la práctica se reparte **por historias de usuario** en cada sprint para que los dos aporten PR en todos. El tablero está en GitHub Projects (estado, prioridad, tamaño, puntos, sprint e hito).

| Sprint | Fechas | Contenido | Estado |
|---|---|---|---|
| 1 | 1–14 oct | Movimiento, salto, evasión, disparo de Talavera, vida y daño, Sandbox, fondo de Dolores, arquitectura | Hecho |
| 2 | 15–29 oct | IA de bandidos y esqueletos, emboscada, Alebrije y montura, guardado, HUD, splash, menús, tutorial, Nivel 2, estampida, Jinete | Hecho |
| 3 | 29 oct–12 nov | Armas (Barro Negro, Vidriado, Maceta), combo, secretos, opciones, Bruja de Fuego, viaje al Nivel 2, créditos, efectos visuales, narrativa, gamepad y táctil, resoluciones, base del Nivel 3 y sprites de la Llorona, documentación | En cierre |
| 4 | 12–26 nov | Nivel 3 completo y pelea con La Llorona | Pendiente |
| 5 | 26 nov–10 dic | Nivel 4, derrumbe de mina y El Charro Negro | Pendiente |
| 6 | 10–24 dic | Balance, playtesting externo, cinemática final, empaquetado y publicación | Pendiente |

**(cambio)** La IA de enemigos se adelantó del Sprint 4 al Sprint 2 porque sin ella el *vertical slice* no se podía jugar.

---

## 4. Desarrollo

- **Vertical slice:** el Nivel 1 completo (movimiento, bandidos, cambio de cerámica, emboscada y Alebrije) se terminó en el Sprint 2.
- **Arquitectura:** scripts modulares por carpeta (`Core`, `Player`, `Combat`, `Enemies`, `UI`, `Editor`); *ScriptableObjects* para las cerámicas; un `GestorNivel` con estados (jugando, pausa, cinemática, game over, victoria); reciclaje de objetos (*object pooling*) para proyectiles, efectos y partículas. Ver [ARQUITECTURA.md](ARQUITECTURA.md).
- **Escenas generadas por código:** constructores del editor (*Alfarero > Construir juego completo*) regeneran prefabs, animaciones y escenas, para que los dos trabajen sin conflictos en archivos `.unity`.
- **Arte:** generado con Google Flow a partir de los sprites existentes para conservar el estilo, recortado y alineado con scripts de Python; escenarios en **tramos de una pantalla unidos con degradado** **(cambio: sin parallax)**.
- **Audio:** música con Google Flow Music y efectos sintetizados por código; Git LFS para los `.wav`.

---

## 5. Pruebas

- **Pruebas automáticas de PlayMode** (más de 100) que cubren movimiento, disparo, armas, IA, emboscadas, jefes, niveles completos, guardado, menús, opciones, créditos, resoluciones, controles y rendimiento. Corren en Unity o en consola antes de cada PR.
- **Tablero Kanban** (GitHub Projects) con historias, errores y criterios de aceptación.
- **(cambio)** El playtesting semanal con jugadores externos se mueve al Sprint 6, con el juego completo; antes se usan las pruebas automáticas y sesiones internas del equipo.

---

## 6. Lanzamiento

- **Plataforma:** itch.io, con versión para el navegador (WebGL) y descarga para Windows; página decorada con elementos de Día de Muertos y alfarería.
- **Promoción:** kit de prensa y tráiler de un minuto (bandidos, uso de la maceta y una toma final contra el Charro Negro), en foros indie, redes sociales y comunidades de *gaming* en México.
- **Documentación de entrega:** este GDD, el [manual de usuario](MANUAL_USUARIO.md), la [bitácora](BITACORA.md), los [créditos y licencias](CREDITOS.md) y las [mecánicas y modificaciones justificadas](MECANICAS.md).

---

## 7. Requisitos del curso y dónde se cumplen

| # | Requisito | Dónde |
|---|---|---|
| 1 | Splash con logo del equipo | Escena `Splash` |
| 2 | Menú: Jugar, Continuar, Opciones, Créditos, Salir | `MenuPrincipal` |
| 3 | Opciones: volumen, controles, calidad | Panel de opciones (menú y pausa) |
| 4 | Tutorial | Carteles al inicio del Nivel 1 |
| 5 | HUD: vida, puntaje, tiempo, objetivo | `HudJuego` |
| 6 | Mecánicas de la Unidad 1 o modificaciones justificadas | [MECANICAS.md](MECANICAS.md) |
| 7 | Victoria y derrota claras | Pantallas de *¡Misión cumplida!* y *Game Over* |
| 8 | Dificultad progresiva | Niveles 1 → 4 |
| 9 | Enemigos con IA | Bandidos, esqueletos, Bruja, jefes |
| 10 | Física coherente | Rigidbody 2D, capas de colisión, salto con gravedad variable |
| 11 | Sonido con créditos y licencias | `BibliotecaAudio`, [CREDITOS.md](CREDITOS.md) |
| 12 | Retroalimentación visual | Partículas, sacudida, *hit stop*, fundidos |
| 13 | Narrativa | Intro, diálogos con retrato y cinemática entre niveles |
| 14 | Guardado | `GestorGuardado` |
| 15 | Controles por plataforma | Teclado y mouse, gamepad y táctil |
| 16 | Interfaz adaptable | Zona 16:9, *Safe Area* y escalado *Expand* |
| 17 | Créditos | Pantalla de créditos |
| 18 | Rendimiento estable | Reciclaje de objetos, texturas comprimidas, pruebas de rendimiento |
| 19 | Git con PR frecuentes de ambos | Una rama y un PR por historia |
| 20 | Documentación | Este GDD, manual, bitácora |
