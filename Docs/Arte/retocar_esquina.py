"""Tapa un objeto que no va con la época en la franja de abajo de un fondo (#86).

Los fondos de túneles de la carpeta Modelos (y uno de Flow) traen llantas de hule abajo a la derecha, algo que no
existía en el Guanajuato del siglo XIX. Se tapan con el rincón izquierdo de la misma imagen reflejado (cajas,
piedras y adoquín con la misma luz y perspectiva), con la orilla difuminada para que no se note el parche.
Trabaja sobre la imagen escalada a 2752 x 1536 (el tamaño de los fondos del juego).

Uso:  python -I Docs/Arte/retocar_esquina.py <entrada> <salida> <x0> <x1> [y0]
      (x0..x1: columnas a tapar; y0: desde qué fila, por omisión 1380)
"""
import sys
import numpy as np
from PIL import Image

ANCHO, ALTO = 2752, 1536
ORILLA = 36                                            # Píxeles de difuminado en el borde izquierdo y de arriba


def retocar(entrada, salida, x0, x1, y0=1380):
    im = Image.open(entrada).convert("RGB")
    if im.size != (ANCHO, ALTO):
        im = im.resize((ANCHO, ALTO), Image.NEAREST)
    a = np.array(im).astype(np.float32)
    ancho = x1 - x0
    parche = a[y0:ALTO, 0:ancho][:, ::-1]              # Rincón izquierdo, reflejado
    alfa = np.ones((ALTO - y0, ancho), np.float32)
    rampa = np.linspace(0.0, 1.0, ORILLA)
    alfa[:, :ORILLA] *= rampa[None, :]
    alfa[:ORILLA, :] *= rampa[:, None]
    zona = a[y0:ALTO, x0:x1]
    a[y0:ALTO, x0:x1] = zona * (1 - alfa[..., None]) + parche * alfa[..., None]
    Image.fromarray(a.clip(0, 255).astype(np.uint8)).save(salida, quality=95)
    print(f"{entrada} -> {salida} (x {x0}-{x1}, y {y0}-{ALTO})")


if __name__ == "__main__":
    args = sys.argv[1:]
    retocar(args[0], args[1], int(args[2]), int(args[3]), int(args[4]) if len(args) > 4 else 1380)
