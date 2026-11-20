"""Compone viñetas de cinemática (1600 x 900) con los fondos y sprites del juego (#35, #92).

Cada viñeta = un fondo de Environment (u otra imagen) oscurecido o teñido + sprites pegados por los pies con
vecino más cercano (no se suaviza el pixel art) + viñeteado en las orillas, como las de la intro.

Uso (desde la raíz del repo):  python -I Docs/Arte/componer_vinetas.py <grupo>
Grupos: panteon (Pozos → panteón) · tuneles (panteón → túneles; la 2 es la ilustración de Flow
Docs/Arte/Flow/vineta_rio_baja_flow.jpg: el río del panteón seco y la boca de los túneles)
"""
import os
import sys
import numpy as np
from PIL import Image, ImageDraw, ImageEnhance

R = os.path.normpath(os.path.join(os.path.dirname(__file__), "..", ".."))
S = os.path.join(R, "Assets", "Art", "Sprites")
SALIDA = os.path.join(S, "Intro")
W, H = 1600, 900


def fondo(ruta, brillo=0.8, tinte=None, fuerza=0.25):
    im = Image.open(ruta if os.path.isabs(ruta) else os.path.join(S, ruta)).convert("RGB").resize((W, H), Image.NEAREST)
    im = ImageEnhance.Brightness(im).enhance(brillo)
    if tinte:
        im = Image.blend(im, Image.new("RGB", (W, H), tinte), fuerza)
    return im.convert("RGBA")


def pegar(base, ruta, alto, x, pie_y, espejo=False, alfa=1.0, sombra=True):
    sp = Image.open(os.path.join(S, ruta)).convert("RGBA")
    sp = sp.crop(sp.getbbox())
    sp = sp.resize((max(1, int(sp.width * alto / sp.height)), int(alto)), Image.NEAREST)
    if espejo:
        sp = sp.transpose(Image.FLIP_LEFT_RIGHT)
    if alfa < 1:
        a = np.array(sp)
        a[..., 3] = (a[..., 3] * alfa).astype("uint8")
        sp = Image.fromarray(a)
    if sombra:
        s = Image.new("RGBA", (int(sp.width * 0.8), 24), (0, 0, 0, 0))
        ImageDraw.Draw(s).ellipse((0, 0, s.width - 1, 23), fill=(0, 0, 0, 110))
        base.alpha_composite(s, (int(x - s.width / 2), int(pie_y - 12)))
    base.alpha_composite(sp, (int(x - sp.width / 2), int(pie_y - sp.height)))


def guardar(nombre, im):
    a = np.array(im).astype(float)
    yy, xx = np.mgrid[0:H, 0:W]
    d = np.sqrt(((xx - W / 2) / (W / 2)) ** 2 + ((yy - H / 2) / (H / 2)) ** 2)
    a[..., :3] *= np.clip(1.15 - 0.45 * d ** 2, 0.35, 1)[..., None]
    Image.fromarray(a.clip(0, 255).astype("uint8")).convert("RGB").save(os.path.join(SALIDA, nombre + ".png"))
    print(nombre)


def panteon():
    # 1. El Jinete se deshace en humo en el patio de la capilla
    im = fondo("Environment/lvl2_seg05.png", 0.75)
    pegar(im, "Bosses/jinete_muerte_02.png", 300, 1030, 840, alfa=0.55)
    pegar(im, "Player/mateo_idle_00.png", 250, 560, 845)
    guardar("panteon_01", im)

    # 2. Las huellas de fuego bajan hacia el río; Mateo las sigue montado
    im = fondo("Environment/lvl3_seg04.png", 0.8)
    for i, x in enumerate(range(900, 1600, 110)):
        pegar(im, "Effects/huella_fuego_00.png", 48, x, 858, sombra=False)
    pegar(im, "Allies/montado_galope_01.png", 320, 560, 862)
    guardar("panteon_02", im)

    # 3. Un llanto entre la niebla del panteón
    im = fondo("Environment/lvl3_seg02.png", 0.7, (20, 40, 70))
    pegar(im, "Bosses/llorona_flotar_00.png", 360, 1150, 700, alfa=0.45, sombra=False)
    pegar(im, "Player/mateo_idle_00.png", 250, 420, 860)
    guardar("panteon_03", im)


def tuneles(fondo_tunel="Environment/lvl4_seg01.png"):
    # 1. La Llorona, liberada, se desvanece sobre el río en calma
    im = fondo("Environment/lvl3_seg05.png", 0.85)
    pegar(im, "Bosses/llorona_desvanecer_00.png", 380, 1060, 720, alfa=0.8, sombra=False)
    pegar(im, "Player/mateo_idle_00.png", 250, 470, 860)
    guardar("tuneles_01", im)

    # 2. Al liberar a La Llorona el río baja y aparece la entrada a los túneles (ilustración de Flow)
    guardar("tuneles_02", fondo(os.path.join(R, "Docs", "Arte", "Flow", "vineta_rio_baja_flow.jpg"), 1.0))

    # 3. Mateo baja a los túneles
    im = fondo(fondo_tunel, 0.8, (60, 0, 10), 0.2)
    pegar(im, "Player/mateo_idle_00.png", 250, 700, 830)
    guardar("tuneles_03", im)


if __name__ == "__main__":
    grupo = sys.argv[1]
    if grupo == "panteon":
        panteon()
    else:
        tuneles()
