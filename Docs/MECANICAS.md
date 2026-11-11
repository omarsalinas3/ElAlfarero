# Mecánicas de la Unidad 1 y modificaciones justificadas

> Documento de la issue #34 (punto 6 del avance: *mecánicas de la Unidad 1 implementadas fielmente o modificaciones justificadas por escrito*).
> Compara el **GDD de la Unidad 1** (*GDD_El_Alfarero_De_Dolores*, septiembre de 2026) con lo que hay en el juego al **3 de noviembre de 2026** (Sprint 3).
> Equipo: Omar Salinas (@omarsalinas3) y Noé Chavero (@SekiroK5).

**Leyenda:** ✅ Implementada tal cual · 🔁 Implementada con cambios (ver justificación) · ⏳ Pendiente (con el sprint en que se planea) · ➕ Agregada (no venía en el GDD)

---

## 1. Resumen

| Estado | Cantidad |
|---|---|
| ✅ Implementadas tal cual | 14 |
| 🔁 Implementadas con cambios | 14 |
| ⏳ Pendientes | 4 |
| ➕ Agregadas | 10 |

Lo esencial del GDD ya funciona: movimiento con salto y evasión, las cuatro cerámicas (Talavera, Barro Vidriado, Barro Negro y Maceta Escudo), cajas de armas, emboscadas en oleadas, estampida, bandidos y calaveras con IA, tres jefes de leyenda (Alebrije, Jinete sin Cabeza y La Llorona), la interfaz de barro y las cerámicas definidas con *ScriptableObjects*. Lo pendiente es el jefe del Nivel 4 (El Charro Negro), el derrumbe de mina y el *parallax*.

---

## 2. Mecánicas del GDD

### 2.1 Movimiento y combate

| Mecánica del GDD | Estado | En el juego | Dónde |
|---|---|---|---|
| Movimiento lateral rápido | ✅ | A/D o flechas; la cámara se adelanta hacia donde mira Mateo | `PlayerController`, `CameraFollow` |
| Saltos precisos | ✅ | Salto de altura variable (soltar corta el salto), *coyote time* y *buffer* de salto | `PlayerController` |
| Evasión | ✅ | Shift: impulso corto con invulnerabilidad; las balas pasan de largo | `PlayerController`, `BalaEnemiga` |
| Disparos *run and gun* al estilo Metal Slug | 🔁 | Mantener Espacio o clic: ráfaga continua. Se apunta con el mouse **o** con el teclado (al frente, o hacia arriba con Espacio + arriba) | `PlayerShooter` (#52) |

### 2.2 Sistema de cerámicas (armas)

| Mecánica del GDD | Estado | En el juego | Dónde |
|---|---|---|---|
| Talavera: ráfagas rápidas y precisas | ✅ | 8 lanzamientos por segundo, poca dispersión, **munición infinita** | `Ceramica_Talavera` |
| Barro Negro: explosivos pesados de corto alcance | 🔁 | **Granada en arco** que explota al tocar suelo o enemigo (o al acabarse la mecha): 45 de daño en un radio de 2.2 u, rompe huacales y sacude la cámara | `Projectile`, `Ceramica_BarroNegro` (#33) |
| Barro Vidriado: fragmentos expansivos | ✅ | Abanico de 5 jarritos en ~40°, de poco alcance | `PlayerShooter`, `Ceramica_BarroVidriado` (#33) |
| Las cerámicas funcionan como distintos tipos de munición | 🔁 | Cada arma especial tiene **munición limitada** (Vidriado 20 por caja, Barro Negro 5) y al acabarse se vuelve a la Talavera. Cambio con teclas 1-4, rueda del mouse o bumpers; el HUD muestra el arma y su munición | `ArsenalJugador`, `HudJuego` (#33) |
| Se obtienen en hornos de barro o cajas de suministros | 🔁 | Las **cajas de arma** salen de **huacales** rompibles; los **hornos** sueltan monedas y caguamas. Recoger una caja desbloquea el arma en el guardado | `Recogible` (tipo Arma), `Destructible` (#33) |
| Maceta como escudo frontal hasta romperse | 🔁 | Se equipa como el arma 4: Mateo carga la maceta al frente y **detiene balas, bolas de fuego y calaveras que llegan de frente**; se cuartea y al **5.º golpe** se rompe. Mientras la carga no lanza cerámica | `EscudoMaceta` (#33) |
| Estadísticas en *ScriptableObjects* (daño, velocidad, durabilidad del escudo) | ✅ | `CeramicaData`: daño, velocidad, cadencia, dispersión, abanico, gravedad, explosión y munición (en la maceta, la munición es su durabilidad) | `CeramicaData` |
| Scripts modulares para el cambio de armas | ✅ | `ArsenalJugador` cambia de `CeramicaData`; `PlayerShooter` solo lanza la que tenga equipada | `ArsenalJugador`, `PlayerShooter` |

### 2.3 Eventos especiales

| Mecánica del GDD | Estado | En el juego | Dónde |
|---|---|---|---|
| Emboscadas que obligan a sobrevivir oleadas | ✅ | La cámara se fija y entran bandidos por ambos lados: 2 oleadas en Dolores y 3 en Pozos | `Emboscada` |
| Estampidas de animales | 🔁 | **Estampida de nahuales** en la cañada de Pozos: la cámara se fija y cruzan manadas a toda velocidad, con aviso antes de cada una; se esquivan saltando o evadiendo | `Estampida`, `Nahual` (#53) |
| Derrumbes de minas | ⏳ | Evento del Nivel 4 (Túneles de Guanajuato), Sprint 4 | — |

### 2.4 Personajes y enemigos

| Mecánica del GDD | Estado | En el juego | Dónde |
|---|---|---|---|
| El Alfarero (jugable) | ✅ | **Mateo**, alfarero de Dolores Hidalgo | Prefab `Alfarero` |
| Su esposa | 🔁 | **Carmen**: aparece en la intro y en el mensaje de derrota; su rescate al amanecer será el final del Nivel 4 | `SecuenciaIntro` |
| Bandidos del desierto | ✅ | Cuatro tipos con IA: revólver, rifle y escopeta disparan con aviso; el de machete pelea cuerpo a cuerpo | `IABandido` |
| Calaveras animadas | 🔁 | **Mineros esqueleto** con pico: patrullan, persiguen, brincan obstáculos bajos, embisten y regresan a su zona | `IAEsqueleto` (#51) |
| Nahuales menores | 🔁 | No son enemigos comunes: forman la **estampida** del Nivel 2 | `Estampida` (#53) |
| Jefe: Alebrijes Gigantes (ataques físicos y transformaciones) | 🔁 | **Alebrije Gigante** del Nivel 1: acecho, zarpazo, salto con onda de choque y transformación al 50 % de vida. Al vencerlo **se libera del hechizo y se vuelve la montura de Mateo** | `JefeAlebrije`, `MonturaAlebrije` (#50) |
| Jefe: La Llorona (ataques sónicos y de agua) | ✅ | Jefa del Nivel 3: onda sónica y chorros de agua; se desvanece si Mateo se le pega y se enfurece a media vida. Furiosa grita dos veces seguidas, sus chorros persiguen a Mateo con una segunda tanda y tras cada ataque de agua reaparece en el otro extremo. Se endureció tras la retroalimentación del Avance 3 (el doble de vida que el Jinete y más daño). Al liberarla se completa el nivel | `JefeLlorona`, `ChorroAgua`, `EncuentroLlorona` |
| Jefe: El Charro Negro (montado, ráfagas de fuego infernal) | ⏳ | Jefe final del Nivel 4, Sprint 4 | — |

### 2.5 Desarrollo, arte y lanzamiento

| Punto del GDD | Estado | En el juego | Dónde |
|---|---|---|---|
| *Vertical slice*: nivel completo con bandidos, cambio de cerámica, emboscada y alebrije | ✅ | Nivel 1 (Dolores Hidalgo) completo, con tutorial, armas, emboscada y Alebrije | `Nivel1_Dolores` |
| Escenarios multicapa con *parallax* | 🔁 | Fondos de **una sola capa por tramo**, unidos con degradado (5 tramos por nivel); sin *parallax* por ahora | `ConfigurarSprint1.CrearFondos` |
| Interfaz rústica de barro | ✅ | HUD con jarrito de vida que se cuartea, barra de barro, marco de azulejo y botones de barro | `HudJuego` (#29) |
| Sprites y animaciones del alfarero y los jefes | 🔁 | Generados con Google Flow y procesados (recorte, alineado por los pies, animación por código); el fuego del Charro y el aura de la Llorona quedan para sus niveles | `Docs/Arte/Flow`, `ConstructorAnimaciones` |
| Efectos de sonido y música | ✅ | Música por pantalla y nivel (Flow Music) y efectos propios para cada acción | `BibliotecaAudio` |
| Playtesting semanal desde el Sprint 3 y tablero Kanban de errores | 🔁 | Tablero de GitHub Projects con issues, sprints y estimación; **139 pruebas automáticas** de PlayMode (incluye recorrer el Nivel 2 completo) en lugar de sesiones semanales con externos, que quedan para el Sprint 6 | Tablero del proyecto, `Assets/Tests/PlayMode` (#40) |
| Publicación en itch.io | ⏳ | Página en borrador con build WebGL; se publica en el Sprint 6 | `ConstruirWeb` (#38) |
| Kit de prensa y tráiler de un minuto | ⏳ | Sprint 6 | — |
| Plataforma PC | 🔁 | PC (Windows) **y navegador** (WebGL para itch.io) | `ConstruirWeb` (#38) |

---

## 3. Justificación de los cambios

**Apuntado con teclado además del mouse (#52).** El GDD habla de *run and gun* al estilo Metal Slug, que se juega sin mouse. Con el teclado se lanza al frente y Espacio + arriba lanza hacia arriba, como en Metal Slug. El mouse sigue sirviendo para apuntar en cualquier ángulo. Gana el último control que se usó, y el mouse solo toma el control si se mueve de verdad, para que un roce no le quite el apuntado al teclado.

**Barro Negro en arco.** "Explosivo de corto alcance" chocaba con el Barro Vidriado, que ya es el arma de corto alcance. Como granada en arco, el Barro Negro alcanza a los bandidos que se cubren detrás de hornos y huacales y castiga a los grupos. Así cada cerámica tiene un uso distinto: la Talavera es precisa, el Vidriado abarca un área cercana y el Barro Negro llega por encima de los obstáculos.

**Munición limitada y Talavera infinita.** Si todas las armas se gastaran, el jugador podría quedarse sin con qué pelear a mitad de un nivel. La Talavera infinita evita ese callejón sin salida; las especiales se racionan y se rellenan con sus cajas, lo que premia explorar y romper huacales.

**Cajas dentro de huacales, no en hornos.** Los hornos ya daban monedas y caguamas, y tienen el doble de vida. Poner las armas en huacales, que se rompen rápido, hace que conseguirlas sea un premio inmediato. Además se distinguen a simple vista: caja de arma o botín del horno.

**Maceta: detiene proyectiles de frente.** El GDD dice "escudo frontal". Que solo cubra lo que llega de frente obliga a voltear hacia el peligro y deja vulnerable la espalda. Mientras la carga Mateo no lanza cerámica, así que hay que decidir entre defenderse o atacar. Detiene proyectiles, pero no los golpes cuerpo a cuerpo (machete, pico): si bloqueara también el contacto, cubrirse sería siempre la mejor opción.

**Calaveras animadas como mineros esqueleto.** Los mineros muertos de Mineral de Pozos encajan con la historia (Pozos es un pueblo minero abandonado) y con el arte del pico. Siguen siendo "calaveras animadas" del GDD, ahora con una IA propia.

**Nahuales como estampida.** El GDD pedía a la vez "estampidas de animales" y "nahuales menores". Juntarlos da un evento especial con identidad propia en el Nivel 2 y evita otro enemigo común que se pareciera a los bandidos.

**Alebrije que se vuelve montura (#50).** Es la "transformación" que pedía el GDD, llevada a la historia: la Talavera consagrada rompe el hechizo del Charro y el Alebrije se une a Mateo. Da una mecánica nueva (montar: más velocidad, más salto y zarpazos) y explica el viaje de Dolores a Pozos (#57).

**4 niveles con un jefe cada uno.** El GDD da los jefes como ejemplos y habla de cementerios, desiertos y pueblos coloniales. Con 12 semanas cabe un jefe por nivel: Dolores (Alebrije), Pozos (Jinete sin Cabeza), Panteón junto al río (La Llorona) y Túneles de Guanajuato (El Charro Negro). El Jinete sin Cabeza se agregó como jefe del Nivel 2 porque la leyenda encaja con el pueblo fantasma. El detalle está en el [README](../README.md#modificaciones-respecto-al-gdd).

**Fondos sin *parallax*.** El arte de los escenarios se generó con Google Flow como ilustraciones completas de una pantalla, no por capas. Separarlas en capas no salía limpio, así que cada nivel se arma con 5 tramos unidos con degradado. El *parallax* queda como mejora para el Sprint 6 si da tiempo.

**Pruebas automáticas en lugar de playtesting semanal.** Con dos integrantes y PRs frecuentes, las 139 pruebas de PlayMode detectan antes que se rompió algo al juntar el trabajo de los dos; por ejemplo, avisaron de referencias del HUD perdidas al regenerar las escenas. El playtesting con jugadores externos se mantiene para el Sprint 6, antes de publicar.

**PC y navegador.** Publicar en itch.io con una versión que se juega en el navegador hace que más gente lo pruebe sin descargar nada. El build de Windows se mantiene.

### Cambios al plan de trabajo del GDD

**IA adelantada.** El GDD dejaba la IA de enemigos para el Sprint 4. Se adelantó al Sprint 2 porque sin enemigos que patrullen, persigan y disparen el *vertical slice* del Nivel 1 no se podía jugar ni balancear. Hacerla temprano también permitió crear las emboscadas y al Alebrije sobre una IA ya probada.

**HUD.** El GDD solo pedía una "interfaz rústica de barro". El requisito del curso pide vida, puntaje, tiempo y objetivo, así que se agregó todo con el mismo estilo: jarrito de vida, puntaje, cronómetro, objetivo con barra de avance y el arma con su munición (#29).

**Reparto por sprints.** El GDD alternaba un responsable por sprint (Omar en los impares y Noé en los pares). En la práctica se repartió **por historias** en cada sprint (Sprint 3: Omar 23 puntos y Noé 24), con una rama y un PR por historia. Así los dos aportan PRs en todos los sprints, que es otro requisito del curso.

---

## 4. Mecánicas agregadas (no venían en el GDD)

| Mecánica | Para qué | Dónde |
|---|---|---|
| ➕ **Consumibles:** la Guacamaya (cura) y la Caguama (5 s de invencibilidad) | Curación y una salida de emergencia con objetos de la cultura local; si ya no caben, se cambian por monedas | `Recogible`, `InventarioJugador` |
| ➕ **Monedas, puntaje y récords** | Rejugabilidad: récord y mejor tiempo por nivel, con aviso de "¡NUEVO RÉCORD!" | `GestorNivel`, `GestorGuardado` (#22, #29) |
| ➕ **Guardado** | Continuar la partida: nivel desbloqueado, récords, monedas, armas y exvotos | `GestorGuardado` (#22) |
| ➕ **Combate cuerpo a cuerpo** (combo de 3 golpes) | Como en Metal Slug: con un enemigo pegado, el clic golpea en vez de lanzar; el tercer golpe empuja | `CombateCuerpo` (#62) |
| ➕ **Montura del Alebrije** | Mecánica de movimiento y ataque distinta, nacida de vencer al primer jefe | `MonturaAlebrije` (#50) |
| ➕ **Bruja de Fuego** | Enemigo volador que obliga a apuntar hacia arriba: hace la dificultad progresiva en el Nivel 2 | `IABruja` (#54) |
| ➕ **Lugares secretos y exvotos** | Explorar vale la pena: muros agrietados, vigas altas de un solo sentido, el tiro de mina al cuarto secreto y 3 exvotos por nivel (`SECRETOS n/3`) | `Exvoto`, `PasoSecreto`, `Cofre` (#63) |
| ➕ **Plataformas** | El GDD no las mencionaba. Huacales y hornos se pueden pisar, y las vigas permiten llegar a zonas altas: dan rutas de exploración y escape durante las emboscadas | `ConstructorSecretos` (#63) |
| ➕ **Tutorial con carteles e intro con viñetas** | Enseñar los controles sin interrumpir; contar la historia antes del primer nivel | `CartelTutorial` (#28), `SecuenciaIntro` |
| ➕ **Opciones** (audio, gráficos y controles) | Ajustar el juego al equipo: volumen, calidad Baja/Media/Alta, pantalla completa y resolución, que se guardan entre sesiones; también desde la pausa | `OpcionesAudio`, `OpcionesGraficos` (#26) |

---

## 5. Pendientes y cuándo se hacen

| Pendiente | Sprint | Issue o responsable |
|---|---|---|
| Nivel 4 (Túneles de Guanajuato), **El Charro Negro** y **derrumbe de mina** | 4 | Por repartir |
| Rescate de Carmen al amanecer (final) | 4 | Por repartir |
| Build para móvil y pruebas en celular | 5 | Por repartir |
| *Parallax* en los fondos | 6 | Mejora si da tiempo |
| Playtesting con jugadores externos, kit de prensa, tráiler y publicación en itch.io | 6 | Equipo |
