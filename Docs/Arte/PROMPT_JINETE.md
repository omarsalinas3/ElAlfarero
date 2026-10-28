# Hoja del Jinete sin Cabeza (#55)

- **Referencia:** `Docs/Arte/Flow/jinete_referencia_flow.jpg` (el personaje que eligió el equipo, subido a Flow como `headless_horseman`).
- **Hoja generada:** `Docs/Arte/Flow/jinete_flow.jpg` (Google Flow, Nano Banana 2.1, con la referencia de arriba).

Prompt usado:

```
Pixel art sprite sheet of EXACTLY this same character from the reference image: same headless rider in the navy blue coat with gold epaulettes, blood-stained saber, black horse with red eye, ghostly cyan smoke and the glowing skull lantern hanging from the saddle. Same pixel art style, outline, palette and pixel size as the reference. Same character identical in every frame, side view facing RIGHT, full body of horse and rider, hooves aligned on the same ground baseline. Grid of 4 rows x 4 columns, evenly spaced, no overlapping: row 1 gallop cycle (4 frames); row 2 saber slash (4 frames: raise, swing, follow-through, recover); row 3 throwing the flaming skull from the lantern forward (4 frames, the skull flies out with cyan flames); row 4: hurt recoil with the horse rearing up (1 frame) then death (3 frames: rider and horse collapse, falling apart into smoke, pile of bones with dying cyan flames). Solid flat pure green #00FF00 background, no shadows, no text, no labels.
```

Recorte (con Unity cerrado; `--minimo=0.15` deja fuera las calaveras que vuelan sueltas en la fila de lanzar):

```
python -I Docs/Arte/recortar_hoja.py Docs/Arte/Flow/jinete_flow.jpg Assets/Art/Sprites/Bosses jinete 4 4 galope_00 galope_01 galope_02 galope_03 sablazo_00 sablazo_01 sablazo_02 sablazo_03 lanzar_00 lanzar_01 lanzar_02 lanzar_03 golpe_00 muerte_00 muerte_01 muerte_02 --minimo=0.15
```

La calavera del proyectil (`Assets/Art/Sprites/Projectiles/jinete_calavera_00.png`) se recortó aparte de la que vuela en el último cuadro de la fila de lanzar. Luego, **Alfarero > Construir juego completo**.
