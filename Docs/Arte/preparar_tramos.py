"""Prepara los tramos de fondo de un nivel a partir de imágenes de Flow (#58).

Cada tramo queda de 2752 x 1536 (el tamaño de los fondos del juego: 21.5 x 12 unidades a 128 px/u). Las imágenes de
1K (1376 x 768) se escalan al doble con vecino más cercano para no suavizar el pixel art. Todos los tramos salvo el
primero llevan en su borde izquierdo un degradado de transparencia de <solape> píxeles: en el juego se superponen esa
cantidad sobre el anterior (ConfigurarSprint1.CrearFondos) y la unión no se nota.

Uso (desde la raíz del repo, con Unity cerrado):
    python -I Docs/Arte/preparar_tramos.py <carpeta destino> <prefijo> <solape px> <imagen 1> <imagen 2> ...

Ejemplo (Nivel 3, panteón junto al río):
    python -I Docs/Arte/preparar_tramos.py Assets/Art/Sprites/Environment lvl3 320 \\
        Docs/Arte/Flow/lvl3_seg01_entrada_flow.jpg Docs/Arte/Flow/lvl3_seg02_nichos_flow.jpg ...
"""
import os
import sys
import numpy as np
from PIL import Image

ANCHO, ALTO = 2752, 1536


def preparar(origen, solape):
    im = Image.open(origen).convert("RGB")
    if im.size != (ANCHO, ALTO):
        im = im.resize((ANCHO, ALTO), Image.NEAREST)
    a = np.array(im.convert("RGBA"))
    if solape > 0:
        rampa = np.linspace(0.0, 1.0, solape) ** 1.5            # Más suave al principio: no se ve el corte
        a[:, :solape, 3] = (rampa[None, :] * 255).astype(np.uint8)
    return Image.fromarray(a)


def main(args):
    destino, prefijo, solape = args[0], args[1], int(args[2])
    for i, origen in enumerate(args[3:]):
        salida = os.path.join(destino, f"{prefijo}_seg{i + 1:02d}.png")
        preparar(origen, 0 if i == 0 else solape).save(salida)
        print(f"{origen} -> {salida}")


if __name__ == "__main__":
    main(sys.argv[1:])
