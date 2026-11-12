# Manual de usuario — El Alfarero de Dolores

*Versión del Sprint 3 (noviembre de 2026) · Omar Salinas y Noé Chavero · Creación de Videojuegos, UTNG*

---

## 1. ¿Qué es?

**El Alfarero de Dolores** es un juego de acción *run and gun* de desplazamiento lateral, en pixel art, ambientado en un Guanajuato sobrenatural. Juegas como **Mateo**, un alfarero de Dolores Hidalgo, que lanza piezas de cerámica consagrada para rescatar a su esposa **Carmen** de las garras del **Charro Negro**.

En cada nivel avanzas de izquierda a derecha, rompes huacales y hornos para conseguir curación, monedas y armas, sobrevives a eventos especiales y al final enfrentas a una leyenda mexicana.

---

## 2. Instalación y requisitos

### Jugar en el navegador (recomendado)

1. Abre la página del juego en **itch.io** con Chrome, Edge o Firefox actualizados.
2. Presiona **Run game** y espera a que cargue (la primera vez descarga unos 60 MB).
3. Para pantalla completa usa el botón de la esquina inferior derecha del reproductor.

Funciona con teclado y mouse, con gamepad (conéctalo y presiona cualquier botón para que el navegador lo detecte) y en celulares o tabletas con pantalla táctil (gira el teléfono en horizontal).

### Windows

1. Descarga el `.zip` de Windows desde itch.io y descomprímelo en una carpeta.
2. Ejecuta `El Alfarero de Dolores.exe`. Si Windows SmartScreen avisa, elige *Más información > Ejecutar de todas formas* (el juego no está firmado).

### Requisitos mínimos

| | Mínimo |
|---|---|
| Sistema | Windows 10 de 64 bits, o un navegador moderno con WebGL 2 |
| Procesador | Doble núcleo a 2 GHz |
| Memoria | 4 GB de RAM |
| Video | Gráficos integrados compatibles con DirectX 11 / WebGL 2 |
| Pantalla | Cualquier resolución; el juego se diseñó en 16:9 y en otras proporciones agrega barras negras |

### Partida guardada

El juego guarda solo al ganar cada nivel (nivel desbloqueado, puntaje máximo, mejor tiempo, armas y exvotos encontrados). En Windows se guarda en la carpeta de datos de la aplicación; en el navegador, en el almacenamiento del sitio (si borras los datos del navegador se pierde). Para empezar de cero usa *Opciones > Borrar partida*.

---

## 3. Controles

El juego detecta solo el último dispositivo que usaste y cambia los textos de ayuda (carteles del tutorial y avisos) para mostrar las teclas o botones correctos.

| Acción | Teclado y mouse | Gamepad (Xbox / PlayStation) | Pantalla táctil |
|---|---|---|---|
| Mover | A / D o flechas ← → | Stick izquierdo o cruceta | Joystick de la izquierda |
| Saltar | W o flecha ↑ | A / ✕ | Botón **SALTO** |
| Evadir (invencible un instante) | Shift izquierdo | B / ○ | Botón **EVADE** |
| Lanzar cerámica / golpear de cerca | Espacio o clic izquierdo (mantener) | X / □ o gatillo derecho | Botón **LANZA** (mantener) |
| Apuntar | Mouse (o al frente con el teclado) | Stick derecho | Al frente (hacia arriba con el joystick arriba) |
| Apuntar hacia arriba | Espacio + flecha ↑ | Stick derecho arriba | Joystick arriba + **LANZA** |
| Cambiar de arma | 1 – 4 o rueda del mouse | LB / RB (L1 / R1) | Botón **ARMA** |
| Beber caguama (5 s invencible) | E o Q | Y / △ | Botón **CAGUAMA** |
| Montar / desmontar al alebrije | F | LB (L1) | Botón **MONTA** |
| Zarpazo (montado) | Espacio o clic | Gatillo derecho | Botón **LANZA** |
| Bajar por un tiro de mina / subir escalera | Flecha ↓ / ↑ | Cruceta ↓ / ↑ | Joystick abajo / arriba |
| Avanzar un diálogo | Enter | Select / View | Se avanza solo |
| Saltar un diálogo | Retroceso | Mantener Select / View | Se avanza solo |
| Pausa | Esc o P | Start / Options | Botón **II** |
| Menús | Flechas + Enter, o mouse | Cruceta + A | Tocar |

---

## 4. Menús

### Pantalla de título
Presiona cualquier tecla (o Start, o toca la pantalla) para abrir el menú principal.

### Menú principal
- **Jugar:** empieza una partida nueva con la intro de la historia. Si ya tienes una partida guardada, pregunta antes de borrarla.
- **Continuar:** lleva al último nivel que desbloqueaste. Está desactivado si no hay partida.
- **Opciones:**
  - *Audio:* volumen de la música y de los efectos.
  - *Gráficos:* calidad (Baja, Media, Alta), pantalla completa o ventana y resolución.
  - *Controles:* tabla con las teclas y botones.
  - *Borrar partida.*
- **Créditos:** autores, herramientas y licencias (la lista sube sola; abajo la acelera y arriba la regresa).
- **Salir.**

### Historia y diálogos
La partida empieza con una **intro** ilustrada. Al iniciar cada nivel hablan los personajes (Don Toribio, Carmen, el Charro Negro) en un cuadro con su retrato que no detiene el juego. Entre el Nivel 1 y el 2 hay una **cinemática** de viñetas. Todo se puede adelantar o saltar (ver *Controles*).

### Menú de pausa
Continuar, Reiniciar el nivel, Opciones y volver al Menú principal.

### Pantallas de fin
- **Game Over:** cuando la vida de Mateo llega a cero. Puedes reintentar el nivel o volver al menú.
- **¡Misión cumplida!:** al vencer al jefe. Muestra puntos, enemigos, monedas, tiempo, récord, mejor tiempo y exvotos encontrados, y permite seguir al siguiente nivel.

---

## 5. La pantalla de juego (HUD)

| Zona | Qué muestra |
|---|---|
| Arriba a la izquierda | El **jarrito de vida** (se cuartea con el daño), la barra de vida y las **caguamas** guardadas (máximo 3); al beber una aparece la barra de invencibilidad |
| Arriba al centro | **Puntos**, **tiempo** y el **objetivo** actual con su barra de avance |
| Arriba a la derecha | El **arma** equipada con su **munición** (∞ en la Talavera) y tus **monedas** |
| Arriba, debajo del objetivo | Los **diálogos** de los personajes, con su retrato (no detienen la acción) |
| Durante un jefe | La **barra de vida del jefe** |

---

## 6. Cómo se juega

### Objetivo
Avanza por el nivel hasta el lugar del jefe y véncelo. El objetivo del HUD te dice qué hacer en cada momento (*Llega a la parroquia*, *Sobrevive a la emboscada*, *Vence al jefe*...).

### Armas de alfarería
| Arma | Cómo funciona | Munición |
|---|---|---|
| **Talavera de Dolores** | Ráfaga rápida y precisa | Infinita |
| **Barro Vidriado** | Abanico de 5 jarritos de corto alcance, tipo escopeta | 20 por caja |
| **Barro Negro** | Granada en arco que explota con fuego y metralla | 5 por caja |
| **Maceta Escudo** | Se carga al frente y detiene balas y bolas de fuego; se rompe al quinto golpe | 5 golpes |

Las cajas de armas salen de los **huacales**. Cuando se acaba la munición de un arma especial vuelves a la Talavera. Con un enemigo pegado enfrente, el botón de lanzar hace un **combo de tres golpes**.

### Objetos
- **Guacamaya:** recupera vida.
- **Caguama:** se guarda (hasta 3); al beberla eres invencible 5 segundos.
- **Monedas:** suman puntos. Si recoges curación con la vida llena o una caguama con las tres guardadas, se convierte en monedas.
- **Exvotos:** hay 3 escondidos en cada nivel (muros agrietados, vigas altas, el tiro de mina...). Suman 500 puntos y quedan guardados.

### Niveles
1. **Dolores Hidalgo.** Calles y parroquia de noche. Bandidos y mineros esqueleto, una emboscada en oleadas y el **Alebrije Gigante**. Al vencerlo se libera del hechizo y se vuelve tu montura.
2. **Mineral de Pozos.** Pueblo fantasma y ruinas mineras. La **Bruja de Fuego** ataca desde el aire, hay una **estampida de nahuales** en la cañada y espera el **Jinete sin Cabeza** en la capilla.
3. **Panteón junto al río** *(en desarrollo: ya se puede recorrer hasta la orilla del río; la pelea con la **Llorona** llega en el Sprint 4).*
4. **Túneles de Guanajuato.** Bajo la ciudad, de la boca del túnel a la guarida del **Charro Negro**. En la galería de vigas el techo se viene abajo (**derrumbe**) y en los túneles bajo la luna roja espera la emboscada más grande. *(La pelea final con el Charro está en desarrollo.)*

---

## 7. Consejos

- **Evade a tiempo:** durante la evasión las balas te atraviesan. Úsala cuando un bandido brille antes de disparar.
- **Rompe todo:** los huacales y hornos traen curación, monedas y armas. El Barro Negro rompe varios de un golpe.
- **Guarda una caguama para el jefe:** 5 segundos de invencibilidad cambian la pelea.
- **Salta a los nahuales:** en la estampida hay un aviso antes de cada manada; salta o evade cuando lo veas.
- **Mira el suelo en el derrumbe:** cada roca marca su sombra antes de caer y algunas caen justo donde estás; no te quedes quieto.
- **Mira hacia arriba:** la Bruja de Fuego vuela; apunta con el mouse, el stick derecho o Espacio + ↑.
- **Montado eres más fuerte:** el alebrije corre más, salta más y tiene su propia vida; si se la acaban te bajas y sigues a pie.
- **Busca los exvotos:** golpea los muros agrietados y sube a las vigas altas.

---

## 8. Problemas comunes

| Problema | Solución |
|---|---|
| El navegador no detecta el gamepad | Presiona un botón del gamepad con la pestaña del juego enfocada |
| No se oye nada en el navegador | Haz clic dentro del juego: los navegadores no reproducen audio hasta que interactúas |
| Se ve con barras negras | Es normal en pantallas que no son 16:9: el juego conserva su encuadre |
| Va lento | En *Opciones > Gráficos* baja la calidad a *Baja* o la resolución |
| Quiero empezar de cero | *Opciones > Borrar partida* |
