# Hojas del Charro Negro (#89)

- **Referencia:** `Personajes/Pixel_art_boss_standing_idle…` de la carpeta Modelos (en Flow aparece al buscar "boss"): el Charro de pie, con espada y fuego.
- **Hojas generadas** (Google Flow, Nano Banana 2.1, proyecto "oct 08 - 17:18"):
  - `Docs/Arte/Flow/charro_caballo_flow.jpg`: fase 1, a caballo, 4 x 4.
  - `Docs/Arte/Flow/charro_pie_flow.jpg`: fase 2, a pie, 4 x 5.
  - `Docs/Arte/Flow/charro_fuego_flow.jpg`: fuego infernal; esta referencia fue la hoja a pie, para que el fuego tenga la misma paleta.

## Prompts

A caballo:
```
Create an image: pixel art sprite sheet of EXACTLY this same character from the reference image (the evil charro in the black embroidered charro suit with silver studs, wide black sombrero with silver trim, red bow tie, glowing red eyes, cursed sword with dark red flames), now RIDING a huge black demonic horse with a mane and tail of red-orange hellfire, glowing red eyes, burning hooves and a silver-studded black saddle. Same pixel art style, outline, palette and pixel size as the reference. Same horse and rider identical in every frame, side view facing RIGHT, full body of horse and rider, hooves aligned on the same ground baseline. Grid of 4 rows x 4 columns, evenly spaced, no overlapping: row 1 gallop cycle (4 frames); row 2: idle with the horse pawing the ground (2 frames), hurt recoil (1 frame), horse rearing up on its hind legs about to charge (1 frame); row 3: the rider throws a blast of hellfire forward from his open hand (4 frames: wind up, release, fireball leaves the hand, recover); row 4: the horse is defeated (4 frames: horse stumbles, horse collapses on its side, horse dissolves into black smoke and embers, the charro lands standing on his feet next to the smoke holding his sword). Solid flat pure green #00FF00 background, no shadows, no text, no labels.
```

A pie:
```
Create an image: pixel art sprite sheet of EXACTLY this same character from the reference image, ON FOOT (no horse): the evil charro in the black embroidered charro suit with silver studs, wide black sombrero with silver trim, red bow tie, glowing red eyes, cursed sword with dark red flames, dark hellfire smoke at his boots. Same pixel art style, outline, palette and pixel size as the reference. Same character identical in every frame, side view facing RIGHT, full body, feet aligned on the same ground baseline. Grid of 4 rows x 5 columns, evenly spaced, no overlapping: row 1: idle standing pose holding the sword (1 frame) then walk cycle (4 frames); row 2: sword slash (4 frames: raise the sword, swing down, follow-through with a red fire trail, recover) then hurt recoil (1 frame); row 3: he throws a blast of hellfire forward from his open hand (4 frames: wind up with fire in the hand, release, fireball leaves the hand, recover) then a furious roar with arms spread and red flames bursting around his body (1 frame); row 4: he plunges the sword into the ground with both hands (3 frames: raise the sword overhead, stab into the ground, flames burst up from the ground around him) then death (2 frames: falls to his knees, collapses burning into black smoke and embers). Solid flat pure green #00FF00 background, no shadows, no text, no labels.
```

Fuego (se pidieron 2 x 4 y salió en 3 x 5; se usan la fila de arriba y la de abajo):
```
Create an image: pixel art sprite sheet of hellfire visual effects ONLY (no characters), using exactly the same pixel art style, outline, palette and pixel size as the red and orange hellfire in the reference image. Grid of 2 rows x 4 columns, evenly spaced, each effect centered in its cell, no overlapping: row 1: a hellfire fireball projectile flying to the RIGHT with a fiery trail behind it (4 animation frames, same size, flickering flames, dark red core, orange and yellow fire, small black smoke); row 2: a pillar of hellfire erupting from the ground (4 frames: small embers and a crack glowing on the ground as a warning, flames starting to rise, tall full column of red-orange fire, fire fading into smoke), the base of every frame aligned on the same ground baseline. Solid flat pure green #00FF00 background, no shadows, no ground, no text, no labels.
```

## Recorte (con Unity cerrado)

```
python -I Docs/Arte/recortar_hoja.py Docs/Arte/Flow/charro_caballo_flow.jpg Assets/Art/Sprites/Bosses charro_caballo 4 4 galope_00 galope_01 galope_02 galope_03 reposo_00 reposo_01 golpe_00 encabritar_00 lanzar_00 lanzar_01 lanzar_02 lanzar_03 caida_00 caida_01 caida_02 caida_03 --minimo=0.15
python -I Docs/Arte/recortar_hoja.py Docs/Arte/Flow/charro_pie_flow.jpg Assets/Art/Sprites/Bosses charro_pie 4 5 reposo_00 caminar_00 caminar_01 caminar_02 caminar_03 espadazo_00 espadazo_01 espadazo_02 espadazo_03 golpe_00 lanzar_00 lanzar_01 lanzar_02 lanzar_03 furia_00 clavar_00 clavar_01 clavar_02 muerte_00 muerte_01 --minimo=0.15
```

Ajustes a mano después del recorte:
- **`charro_caballo_caida_03`:** se borró. En ese cuadro el Charro ya está de pie, pero a la escala de la hoja montada (más grande); en el juego entra en su lugar la hoja a pie.
- **`charro_pie_lanzar_02`:** se le quitó la bola de fuego suelta delante de la mano, para que no se vea doble con el proyectil.
- **Fuego:** la hoja se cortó en dos franjas antes de recortar, porque la columna alta de fuego invade la fila de en medio:
  - Bolas: `0–30 %` del alto, 1 x 5, prefijo `Projectiles/charro_fuego`.
  - Llamaradas: `50.5–100 %` del alto, 1 x 5, prefijo `Projectiles/charro_llamarada`.
  - Las bolas se alinearon por la cabeza (orilla derecha) para que no salten entre cuadros.

Escalas en `ImportadorSprites`:

| Prefijo | Pixeles por unidad | Tamaño en el juego |
|---|---|---|
| `charro_caballo_` | 36 | ~5.1 u de alto |
| `charro_pie_` | 45 | ~3.8 u de alto |
| `charro_fuego` | 95 | ~2 u de largo |
| `charro_llamarada` | 80 | ~4.5 u de alto, con pivote en la base |

Luego, **Alfarero > Construir juego completo** (`Editor/ConstructorCharro.cs`).
