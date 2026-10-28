"""Recorta la hoja del Jinete sin Cabeza de Flow (4 x 4, fondo verde #00FF00) en los cuadros del juego.

Uso (desde la raíz del repo, con Unity cerrado):
    python -I Docs/Arte/recortar_jinete.py Docs/Arte/Flow/jinete_flow.jpg Assets/Art/Sprites/Bosses

Filas: 1 galope (4), 2 sablazo (4), 3 lanzar calavera (4), 4 golpe (1) + muerte (3).
Quita el verde con despill de orillas, descarta motas sueltas y alinea todos los cuadros por los pies en un lienzo común.
Luego correr "Alfarero > Construir juego completo": el constructor usa estos cuadros en lugar del provisional.
"""
import sys
import numpy as np
from PIL import Image
from scipy import ndimage

FILAS = [
    ["galope_00", "galope_01", "galope_02", "galope_03"],
    ["sablazo_00", "sablazo_01", "sablazo_02", "sablazo_03"],
    ["lanzar_00", "lanzar_01", "lanzar_02", "lanzar_03"],
    ["golpe_00", "muerte_00", "muerte_01", "muerte_02"],
]


def sin_verde(rgb):
    r, g, b = [rgb[..., i].astype(np.float32) for i in range(3)]
    verdor = g - np.maximum(r, b)                       # Cuánto más verde que lo demás
    alfa = np.clip(1.0 - (verdor - 40.0) / 80.0, 0.0, 1.0)
    g2 = np.minimum(g, np.maximum(r, b))                # Despill: la orilla no queda verdosa
    salida = np.dstack([r, g2, b, alfa * 255.0]).astype(np.uint8)
    salida[alfa < 0.15] = 0
    return salida


def main(origen, destino):
    a = sin_verde(np.array(Image.open(origen).convert("RGB")))
    alto, ancho = a.shape[:2]
    cuadros = {}
    for f, nombres in enumerate(FILAS):
        for c, nombre in enumerate(nombres):
            celda = a[f * alto // 4:(f + 1) * alto // 4, c * ancho // 4:(c + 1) * ancho // 4].copy()
            mascara = celda[..., 3] > 30
            etiquetas, n = ndimage.label(mascara)
            if n == 0:
                print("Celda vacía:", nombre)
                continue
            tam = ndimage.sum(mascara, etiquetas, range(1, n + 1))
            sirve = np.isin(etiquetas, [i + 1 for i, t in enumerate(tam) if t >= 0.03 * tam.max()])
            celda[~sirve] = 0
            ys, xs = np.where(sirve)
            cuadros[nombre] = celda[ys.min():ys.max() + 1, xs.min():xs.max() + 1]

    w = max(c.shape[1] for c in cuadros.values())
    h = max(c.shape[0] for c in cuadros.values())
    for nombre, c in cuadros.items():
        lienzo = np.zeros((h, w, 4), np.uint8)
        ch, cw = c.shape[:2]
        x0 = (w - cw) // 2
        lienzo[h - ch:, x0:x0 + cw] = c                  # Alineado por los pies
        Image.fromarray(lienzo).save(f"{destino}/jinete_{nombre}.png")
    print(f"{len(cuadros)} cuadros de {w}x{h} en {destino}")


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
