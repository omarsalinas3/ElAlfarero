# El Alfarero de Dolores — Arranque del proyecto (Sprint 1)

Unity 6.6 (6000.6.0f1) · 2D · teclado + mouse · pixel art

Se hace en orden. Cada paso tiene su criterio de "listo" para no avanzar con cosas a medias.

## 1. Crear el proyecto
- Unity Hub > New project > **Universal 2D** > nombre `ElAlfarero`.
- Copia el contenido de este zip encima de la carpeta del proyecto (`Assets/`, `.gitignore`, `.gitattributes`).
- **Listo cuando:** Unity compila sin errores y aparece el menú **Alfarero** arriba.
  (Si marca error por `UnityEngine.InputSystem`, es normal: falta el paso 2.)

## 2. Paquetes
Window > Package Manager > botón **+** > *Install package by name*:

| Nombre del paquete | Para qué |
|---|---|
| `com.unity.inputsystem` | Teclado y mouse (lo usan los scripts) |
| `com.unity.cinemachine` | Cámara que sigue al jugador |
| `com.unity.feature.2d` | Tilemap, animación 2D, sprite shape, etc. |

TextMeshPro ya viene dentro de uGUI en Unity 6. El **Pixel Perfect Camera** viene con URP 2D (se agrega como componente a la cámara).

- Al instalar Input System, Unity pregunta si activa el backend nuevo: **Yes** (reinicia el editor).
- Si no pregunta: Project Settings > Player > Other Settings > **Active Input Handling = Input System Package (New)**.
- **Listo cuando:** no hay errores rojos en la consola.

## 3. Estructura de carpetas
- Menú **Alfarero > Crear estructura de carpetas**.

## 4. Capas y colisiones
Project Settings > Tags and Layers, crea: `Ground`, `Player`, `Enemy`, `Projectile`.
Project Settings > Physics 2D > Layer Collision Matrix: desactiva `Projectile` con `Projectile` y con `Player`.

## 5. Arte base recomendado (para que Noé y tú usen los mismos valores)
- **Pixels Per Unit: 32** en todos los sprites. Filter Mode: **Point (no filter)**. Compression: **None**.
- Personaje sugerido: ~32×48 px. Resolución de referencia del Pixel Perfect Camera: **480×270**.

## 6. Escena Sandbox (`Assets/Scenes/Sandbox.unity`)
1. **Suelo:** Sprite cuadrado, escala ancha, `BoxCollider2D`, layer `Ground`.
2. **Jugador:** GameObject `Alfarero` con `SpriteRenderer` (placeholder), `Rigidbody2D` (Gravity Scale 3), `BoxCollider2D`, `Health`, `PlayerController`, `PlayerShooter`, layer `Player`.
   - Hijo `PuntoSuelo` en los pies (Y negativa) → arrástralo a *Punto Suelo*; *Capa Suelo* = `Ground`.
   - Hijo `PuntoDisparo` a la altura de la mano → arrástralo a *Punto Disparo* en `PlayerShooter`.
   - Crea un **Physics Material 2D** con fricción 0 y asígnalo al collider (evita que se "pegue" a las paredes).
3. **Proyectil:** prefab `Proyectil_Talavera` con `SpriteRenderer`, `Rigidbody2D`, `CircleCollider2D` (Is Trigger ✔) y `Projectile`. Guárdalo en `Prefabs/Projectiles`.
4. **Cerámica:** clic derecho en `ScriptableObjects/Ceramicas` > Create > Alfarero > Ceramica → `Ceramica_Talavera`. Arrastra el prefab, y en *Capas Impacto* marca `Ground` y `Enemy`.
   Arrastra ese asset a *Ceramica Actual* del jugador.
5. **Dummy:** objeto con `BoxCollider2D`, `Health` (Destruir Al Morir ✔), layer `Enemy`.
6. **Cámara:** Cinemachine > 2D Camera, Follow = Alfarero. Añade `Pixel Perfect Camera` a la Main Camera.
- **Listo cuando:** corres, saltas, evades con Shift, apuntas con el mouse y destruyes el dummy mantenido clic izquierdo.

## 7. Git + GitHub
```powershell
git init
git lfs install
git add .
git commit -m "Sprint 1: base del proyecto"
# crea el repo vacío en GitHub y luego:
git branch -M main
git remote add origin https://github.com/TU_USUARIO/ElAlfarero.git
git push -u origin main
```
En Unity: Project Settings > Editor > **Asset Serialization = Force Text** y **Version Control Mode = Visible Meta Files** (suelen venir así por defecto en 2D; confirma).

Flujo sugerido entre ustedes: ramas por tarea (`feature/movimiento`, `art/sprites-alfarero`) y Pull Request a `main`. Eviten editar la misma escena a la vez.

## 8. GitHub Projects
- Repo > Projects > New project > plantilla **Board**. Columnas: `Backlog`, `Sprint actual`, `En progreso`, `En revisión`, `Hecho`.
- Corre `crear_issues_sprint1.ps1` para crear las issues del Sprint 1 y arrástralas al tablero.
- Bugs: issues con label `bug` (sustituye el Kanban de bugs del GDD).

## Controles actuales
| Acción | Tecla |
|---|---|
| Mover | A / D o flechas |
| Saltar | Espacio o W |
| Evadir | Shift izquierdo |
| Apuntar | Mouse |
| Disparar | Clic izquierdo (mantener) |
