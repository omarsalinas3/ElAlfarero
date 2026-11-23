"""Compone viñetas de cinemática (1600 x 900) con los fondos y sprites del juego (#35, #92).

Cada viñeta = un fondo de Environment (u otra imagen) oscurecido o teñido + sprites pegados por los pies con
vecino más cercano (no se suaviza el pixel art) + viñeteado en las orillas, como las de la intro.

Uso (desde la raíz del repo):  python -I Docs/Arte/componer_vinetas.py <grupo>
Grupos: panteon (Pozos → panteón) · tuneles (panteón → túneles; la 2 es la ilustración de Flow
Docs/Arte/Flow/vineta_rio_baja_flow.jpg: el río del panteón seco y la boca de los túneles) · final (el rescate de
Carmen al amanecer, #91)
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


def sin_magenta(sp):
    """Quita el fondo magenta de los modelos de la carpeta Modelos (p. ej. Allies/carmen.png) y las motas sueltas."""
    from scipy import ndimage
    a = np.array(sp)
    r, g, b = [a[..., i].astype(int) for i in range(3)]
    a[(r > 170) & (b > 170) & (g < 120), 3] = 0
    etiquetas, n = ndimage.label(a[..., 3] > 0)
    if n > 1:
        tam = ndimage.sum(np.ones_like(etiquetas), etiquetas, range(1, n + 1))
        a[etiquetas != int(np.argmax(tam)) + 1, 3] = 0       # Solo la figura más grande
    return Image.fromarray(a)


def pegar(base, ruta, alto, x, pie_y, espejo=False, alfa=1.0, sombra=True, magenta=False):
    sp = Image.open(os.path.join(S, ruta)).convert("RGBA")
    if magenta:
        sp = sin_magenta(sp)
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


def final():
    """El final del juego (#91): el rescate de Carmen al amanecer."""
    # 1. El Charro Negro cae de rodillas en su guarida; la luna roja se apaga
    im = fondo("Environment/lvl4_seg05.png", 0.7, (70, 0, 10), 0.25)
    pegar(im, "Bosses/charro_pie_muerte_00.png", 300, 1080, 850)
    pegar(im, "Player/mateo_idle_00.png", 250, 480, 860)
    guardar("final_01", im)

    # 2. El Alebrije, libre y leal, llega rugiendo a la guarida
    im = fondo("Environment/lvl4_seg05.png", 0.8, (40, 10, 60), 0.2)
    pegar(im, "Player/mateo_idle_00.png", 250, 420, 860)
    pegar(im, "Bosses/alebrije_rugido_00.png", 420, 1100, 865)
    guardar("final_02", im)

    # 3. Carmen lanza la última Talavera consagrada y el Charro se deshace en humo
    im = fondo("Environment/lvl4_seg05.png", 0.85, (120, 60, 0), 0.15)
    pegar(im, "Allies/carmen.png", 270, 420, 865, magenta=True)
    pegar(im, "Projectiles/talavera_shot_02.png", 70, 800, 560, sombra=False)
    pegar(im, "Bosses/charro_pie_muerte_01.png", 230, 1150, 850, alfa=0.6)
    guardar("final_03", im)

    # 4. Amanece sobre Dolores: Mateo y Carmen vuelven a casa
    im = fondo("Environment/lvl1_seg02.png", 1.3, (255, 175, 90), 0.2)
    pegar(im, "Player/mateo_idle_00.png", 250, 700, 860)
    pegar(im, "Allies/carmen.png", 255, 900, 862, espejo=True, magenta=True)
    guardar("final_04", im)


if __name__ == "__main__":
    grupo = sys.argv[1]
    if grupo == "panteon":
        panteon()
    elif grupo == "final":
        final()
    else:
        tuneles()
