# Prompt para la hoja del Jinete sin Cabeza (#55)

En Google Flow (proyecto "oct 15 - 22:23"), botón **+** → elegir **"Bandit sprite sheet design"** como referencia de estilo, pegar el prompt y generar. Descargar en **1K (tamaño original)** y guardar como `Docs/Arte/Flow/jinete_flow.jpg`.

```
Pixel art sprite sheet in exactly the same art style, outline and palette as the reference image. Character: the Headless Horseman of Mexican legend: a headless charro rider in a black charro suit with silver buttons and a red sash, no head (flames rise from the neck), riding a black skeletal ghost horse with fiery orange mane, tail and hooves. He holds a curved saber in one hand and an old iron lantern with a flaming skull inside in the other. Same character identical in every frame, side view facing right, full body of horse and rider, feet aligned on the same baseline. Grid of 4 rows x 4 columns, evenly spaced, no overlapping: row 1 gallop cycle (4 frames); row 2 saber slash (4 frames: raise, swing, follow-through, recover); row 3 throwing the flaming skull from the lantern forward (4 frames); row 4: hurt recoil (1 frame) then death (3 frames: horse rears, rider and horse collapse, pile of bones with dying flames). Solid flat pure green #00FF00 background, no shadows, no text, no labels.
```

Filas → animaciones: 1 `jinete_galope_00..03`, 2 `jinete_sablazo_00..03`, 3 `jinete_lanzar_00..03`, 4 `jinete_golpe_00` + `jinete_muerte_00..02`.
Si Flow no respeta la cuadrícula, regenerar; el recorte automático espera 4 × 4.

Después, con Unity cerrado:

```
python -I Docs/Arte/recortar_jinete.py Docs/Arte/Flow/jinete_flow.jpg Assets/Art/Sprites/Bosses
```

y correr **Alfarero > Construir juego completo**: `ConstructorJinete` usa los cuadros `jinete_*` en lugar del bandido provisional. Revisar el tamaño (PPU `jinete_` = 60 en `ImportadorSprites`) y el collider con una captura.
