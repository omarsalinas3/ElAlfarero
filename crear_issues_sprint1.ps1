# Crea las issues del Sprint 1 en el repo actual con GitHub CLI (gh).
# Requisitos: instalar gh (https://cli.github.com), correr `gh auth login` y estar dentro de la carpeta del repo.
# Después, en GitHub Projects, agrega las issues al tablero (o usa el workflow "Auto-add to project").

gh label create "sprint-1" --color "0E8A16" --description "Semanas 1 y 2" --force

$issues = @(
    @{ t = "Crear proyecto Unity 6.6 (Universal 2D) y estructura de carpetas"; b = "Correr el menú Alfarero > Crear estructura de carpetas." },
    @{ t = "Configurar Git, .gitignore, LFS y repo en GitHub"; b = "Primer commit + invitar a Noé como colaborador." },
    @{ t = "Instalar paquetes (Input System, Cinemachine, 2D feature set) y activar Input System"; b = "Project Settings > Player > Active Input Handling = Input System Package (New)." },
    @{ t = "Movimiento horizontal con aceleración"; b = "PlayerController: velocidadMaxima, aceleracion, desaceleracion." },
    @{ t = "Salto: coyote time, buffer, altura variable y caída pesada"; b = "Ajustar los valores en el Inspector hasta que se sienta preciso." },
    @{ t = "Evasión con invulnerabilidad"; b = "Shift izquierdo; usa Health.Invulnerable." },
    @{ t = "Apuntado con mouse y volteo del personaje"; b = "PlayerShooter.DireccionApuntado + PlayerController.ActualizarOrientacion." },
    @{ t = "Sistema básico de disparo (CeramicaData + Projectile)"; b = "Crear el asset Ceramica_Talavera y el prefab del proyectil." },
    @{ t = "Componente Health y daño básico"; b = "Dummy de pruebas que reciba daño y se destruya." },
    @{ t = "Escena Sandbox de pruebas"; b = "Suelo, plataformas, dummies, cámara con Cinemachine y Pixel Perfect." },
    @{ t = "Documento de arquitectura"; b = "Estructura de scripts, convenciones de nombres, flujo de datos con ScriptableObjects." }
)

foreach ($i in $issues) {
    gh issue create --title $i.t --body $i.b --label "sprint-1"
}
