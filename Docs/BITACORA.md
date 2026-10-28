# Bitácora de avances — El Alfarero de Dolores

Registro por fecha de qué se hizo, quién lo hizo, qué problemas salieron y qué se decidió. Se actualiza al cierre de cada sprint.

- **Equipo:** Omar Salinas (@omarsalinas3) y Noé Chavero (@SekiroK5)
- **Tablero:** https://github.com/users/omarsalinas3/projects/3
- **Sprints:** 2 semanas cada uno. Sprint 1: 1–14 oct · Sprint 2: 15–29 oct · Sprint 3: 29 oct–12 nov

---

## Sprint 1 (1–14 de octubre de 2026)

**Meta:** núcleo jugable (moverse, saltar, evadir, lanzar cerámica, vida y daño) y primer nivel visible.

### 14 de octubre — Base del proyecto y avance 1
| Quién | Qué se hizo |
|---|---|
| Omar | Proyecto en Unity 6.6 (URP 2D, Input System por código); sistema de combate (`Health`, `CeramicaData`, `Projectile`); movimiento del Alfarero, disparo, animador y cámara que sigue; importador de sprites y constructor del Sandbox; pruebas de PlayMode del Sprint 1; README con descripción y hoja de ruta |
| Noé | Sprites de Mateo (ciclo de carrera), aliados (Carmen y Don Toribio), Talavera, cajas de cerámica y minero esqueleto; fondo del Nivel 1 en 5 tramos de Dolores unidos con degradado; animaciones, prefabs, cerámica Talavera y escena Sandbox; documento de arquitectura |

**Avance del 14 de octubre (20 puntos):** se presentó la base jugable en el Sandbox y el fondo del Nivel 1. Se revisó la lista de requisitos del curso (splash, menú, opciones, tutorial, HUD, guardado, IA, narrativa, documentación, etc.) y se repartió en el tablero por sprint.

**Problemas:** Unity 6.6 agrega la plataforma `WindowsStoreApps` a los `.meta` de los sprites al abrir el proyecto en otra computadora, y eso ensucia los commits.

**Decisiones:**
- Todo el arte se genera en Google Flow usando los sprites ya hechos como referencia, para conservar el estilo.
- Las escenas y prefabs se generan por código (constructores del editor) para que los dos podamos trabajar sin conflictos en archivos `.unity`.
- Audio por Git LFS.

---

## Sprint 2 (15–29 de octubre de 2026)

**Meta:** Nivel 1 completo con enemigos y jefe, guardado, HUD, menús y Nivel 2.

### 15 de octubre — Juego completo de principio a fin
| Quién | Qué se hizo |
|---|---|
| Omar | Sistema de audio (música de menú, intro y nivel; efectos; biblioteca de sonidos); sprites de HUD, pickups, obstáculos e intro; obstáculos, recogibles, caguama y reacción al daño; menú principal, intro, HUD, pausa, game over y victoria; generación de escenas por código; pruebas del juego completo |
| Noé | `.meta` de sprites con `WindowsStoreApps` en un commit aparte |

**Decisión:** los `.meta` que solo cambian por la plataforma `WindowsStoreApps` van en commits separados para no mezclarlos con trabajo real.

### 16–22 de octubre — IA y jefe del Nivel 1 (PR #61)
| Quién | Qué se hizo |
|---|---|
| Noé | IA de los mineros esqueleto (patrulla, persecución, salto de obstáculos y embestida); reposo y muerte de Mateo en Flow; bandidos del Charro con IA (revólver, rifle, escopeta y machete); emboscada en oleadas; jefe Alebrije Gigante con cinemática entre las torres de la parroquia; ajuste de controles (Espacio lanza, flecha arriba salta); documento de contexto para asistentes de IA |

**Problemas:**
- Con *Disable Domain Reload* activado, las variables `static` de `GestorAudio` no se reiniciaban entre un Play y otro y la música se cortaba. Se corrigió reiniciándolas con `RuntimeInitializeOnLoadMethod(SubsystemRegistration)`; desde entonces es regla para todo `static`.
- El operador `?.` de C# no detecta objetos de Unity destruidos; se usa `obj != null ? obj : null`.
- Apuntar dependía demasiado del mouse; quedó como pendiente para la #52.

### 22 de octubre — Historia de 4 niveles
**Decisiones (modificaciones justificadas al GDD):**
- El juego queda en **4 niveles**: Dolores Hidalgo (Alebrije), Mineral de Pozos (Jinete sin Cabeza), Panteón junto al río (La Llorona) y Túneles de Guanajuato (Charro Negro).
- El **Charro Negro** es el líder de los bandidos y hechiza a las leyendas con la luna roja; la Talavera consagrada rompe los hechizos.
- El **Alebrije** liberado se vuelve la montura de Mateo (#50) y lo lleva a Pozos (#57).
- El **Jinete sin Cabeza** pasa de minijefe a jefe del Nivel 2; la **estampida de nahuales** es el evento especial del Nivel 2.

### 23–27 de octubre — Sistemas y Nivel 2 (PR #64 a #72)
| Quién | Qué se hizo |
|---|---|
| Omar | HUD con puntaje, cronómetro y objetivo (#29); guardado JSON con récords, nivel desbloqueado, monedas y armas (#22); apuntado con teclado con umbral del mouse y Espacio + arriba (#52); minero esqueleto animado (#51); menú con Continuar, Opciones, Créditos y confirmación (#23); splash con el logo "OMAR & NOE" (#25); **Nivel 2: Mineral de Pozos** (#32); reciclaje de objetos y builds de Windows y WebGL (#38); Bruja de Fuego (#54) |

**Problemas:**
- Crear y destruir proyectiles a cada rato provocaba tirones; se agregó `Reciclaje` (object pooling) y ahora todo proyectil, bala o efecto sale de `Reciclaje.Sacar` y vuelve con `Reciclaje.Guardar`.
- Las cerámicas (cinemáticas) no detectaban a la Bruja (también cinemática) hasta activar `useFullKinematicContacts`.
- `ConstructorJuego.cs` creció demasiado: se volvió `partial` y cada nivel o enemigo nuevo va en su propio archivo (`ConstructorNivel2.cs`, `ConstructorBruja.cs`).

**Decisiones:**
- Las pruebas usan un archivo de guardado aparte y nunca tocan la partida real.
- En los enums `Sfx` y `Musica` siempre se agrega al final, para no mover los valores ya guardados en las escenas.
- 58 pruebas de PlayMode pasan sobre `develop` (28-oct).

### 28 de octubre — Trabajo de Noé del Nivel 2 y documentación
| Quién | Qué se hizo |
|---|---|
| Noé | README con la historia de 4 niveles y las modificaciones al GDD (#60); esta bitácora (#31); tutorial con carteles al inicio del Nivel 1 (#28); estampida de nahuales en la cañada de Pozos (#53); el Alebrije vencido se vuelve montura: F monta, corre y salta más, zarpazo y vida propia (#50); Jinete sin Cabeza: IA (carga, sablazo, calavera en llamas, segunda fase) y cinemática de entrada con arte provisional (#56) |

**Problemas:**
- En modo batch, las pruebas con teclado virtual no reciben entrada si no se pide `BackgroundBehavior.IgnoreFocus` (como en `JuegoTests`).
- Google Flow rechazó la generación automática desde el navegador ("actividad inusual"); la hoja del Jinete (#55) se pide a mano con el prompt de `Docs/Arte/PROMPT_JINETE.md` y se recorta con `Docs/Arte/recortar_jinete.py`.
- Si Unity compila mientras se escriben scripts nuevos, el batch falla con tipos que "no existen": no tocar `Assets` mientras corre el constructor o las pruebas.

**Decisiones:**
- Montura: tecla F; montado +40 % velocidad y +20 % salto; Espacio o clic hace zarpazo en lugar de lanzar; 60 de vida propia; al agotarse Mateo cae y sigue a pie. Llegar montado al final de la plaza completa el Nivel 1 (el viaje a Pozos es la #57 de Omar).
- Los nahuales no se pueden matar (las cerámicas los atraviesan): son un obstáculo del evento, se esquivan saltando o evadiendo.
- El Jinete es cinemático con collider trigger de 2.4 u de alto, para que su carga se pueda esquivar saltando o evadiendo.
- Tutorial: el primer minero pasa de x 7 a 9 y los dos primeros bandidos se recorren 3.5 y 1 u, para que la zona de carteles sea segura.

---

## Plantilla para la siguiente entrada

```
### <fecha> — <tema>
| Quién | Qué se hizo |
|---|---|
| ... | ... |

**Problemas:** ...
**Decisiones:** ...
```
