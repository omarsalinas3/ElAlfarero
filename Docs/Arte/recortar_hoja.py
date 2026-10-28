"""Recorta una hoja de sprites de Flow (fondo verde #00FF00, cuadrícula pareja) en los cuadros del juego.

Uso (desde la raíz del repo, con Unity cerrado):
    python -I Docs/Arte/recortar_hoja.py <hoja> <carpeta destino> <prefijo> <filas> <columnas> <nombre por celda...> [--voltear]

Los nombres van fila por fila, de izquierda a derecha; "-" salta la celda. Con --voltear se espejan los cuadros
(p. ej. si Flow los dibujó mirando al lado contrario del que espera el código). Con --minimo=0.15 se descartan las piezas
sueltas más chicas que esa fracción de la figura principal de la celda (por omisión 0.03: solo motas).
Quita el verde con despill de orillas y toma las figuras completas aunque se salgan un poco de su celda: cada pieza
cuenta para la celda donde cae su centro. Luego alinea todos los cuadros por los pies en un lienzo común.

Ejemplo (Mateo montado en el alebrije, hoja de 3 x 4):
    python -I Docs/Arte/recortar_hoja.py Docs/Arte/Flow/montado_flow.jpg Assets/Art/Sprites/Allies montado 3 4 \
        galope_00 galope_01 galope_02 galope_03 reposo_00 - salto_00 golpe_00 zarpazo_00 zarpazo_01 zarpazo_02 zarpazo_03 --voltear
"""
import sys
import numpy as np
from PIL import Image
from scipy import ndimage


def sin_verde(rgb):
    r, g, b = [rgb[..., i].astype(np.float32) for i in range(3)]
    verdor = g - np.maximum(r, b)                       # Cuánto más verde que lo demás
    alfa = np.clip(1.0 - (verdor - 40.0) / 80.0, 0.0, 1.0)
    g2 = np.minimum(g, np.maximum(r, b))                # Despill: la orilla no queda verdosa
    salida = np.dstack([r, g2, b, alfa * 255.0]).astype(np.uint8)
    salida[alfa < 0.15] = 0
    return salida


def main(args):
    voltear = "--voltear" in args
    minimo = next((float(a.split("=")[1]) for a in args if a.startswith("--minimo=")), 0.03)
    args = [a for a in args if not a.startswith("--")]
    origen, destino, prefijo, filas, columnas = args[0], args[1], args[2], int(args[3]), int(args[4])
    nombres = args[5:]
    if len(nombres) != filas * columnas:
        sys.exit(f"Se esperaban {filas * columnas} nombres y llegaron {len(nombres)}")

    a = sin_verde(np.array(Image.open(origen).convert("RGB")))
    alto, ancho = a.shape[:2]
    alto_celda, ancho_celda = alto / filas, ancho / columnas
    # Piezas de toda la hoja: cada una cuenta para la celda donde cae su centro, aunque se salga un poco (cola, sable).
    # Se corta en las líneas de la cuadrícula solo donde dos cuadros vecinos se tocan, para que no se unan en una pieza.
    mascara = a[..., 3] > 30
    cortada = mascara.copy()
    for f in range(1, filas):
        cortada[int(f * alto_celda), :] = False
    for c in range(1, columnas):
        cortada[:, int(c * ancho_celda)] = False
    etiquetas, n = ndimage.label(cortada)
    tam = ndimage.sum(mascara, etiquetas, range(1, n + 1))
    centros = ndimage.center_of_mass(mascara, etiquetas, range(1, n + 1))
    celda_de = [min(int(cy / alto_celda), filas - 1) * columnas + min(int(cx / ancho_celda), columnas - 1) for cy, cx in centros]

    cuadros = {}
    for i, nombre in enumerate(nombres):
        if nombre == "-":
            continue
        piezas = [k for k in range(n) if celda_de[k] == i]
        if not piezas:
            print("Celda vacía:", nombre)
            continue
        mayor = max(tam[k] for k in piezas)
        sirve = np.isin(etiquetas, [k + 1 for k in piezas if tam[k] >= minimo * mayor])
        ys, xs = np.where(sirve)
        recorte = a[ys.min():ys.max() + 1, xs.min():xs.max() + 1].copy()
        recorte[~sirve[ys.min():ys.max() + 1, xs.min():xs.max() + 1]] = 0
        cuadros[nombre] = recorte[:, ::-1] if voltear else recorte

    w = max(c.shape[1] for c in cuadros.values())
    h = max(c.shape[0] for c in cuadros.values())
    for nombre, c in cuadros.items():
        lienzo = np.zeros((h, w, 4), np.uint8)
        ch, cw = c.shape[:2]
        x0 = (w - cw) // 2
        lienzo[h - ch:, x0:x0 + cw] = c                  # Alineado por los pies
        Image.fromarray(lienzo).save(f"{destino}/{prefijo}_{nombre}.png")
    print(f"{len(cuadros)} cuadros de {w}x{h} en {destino}")


if __name__ == "__main__":
    main(sys.argv[1:])
