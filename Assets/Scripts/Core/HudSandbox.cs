using UnityEngine;

/// <summary>
/// HUD temporal de depuración para el Sandbox (el HUD de barro real es del Sprint 6).
/// Muestra controles, vida y estado del jugador.
/// </summary>
public class HudSandbox : MonoBehaviour
{
    [Tooltip("Apagado por defecto: el texto de depuración tapaba la pantalla.")]
    [SerializeField] bool mostrar = false;
    [SerializeField] Health salud;
    [SerializeField] PlayerController jugador;
    [SerializeField] PlayerShooter tirador;

    void OnGUI()
    {
        if (!mostrar) return;

        GUILayout.BeginArea(new Rect(12, 10, 640, 140));
        GUILayout.Label("EL ALFARERO DE DOLORES - Sandbox Sprint 1");
        GUILayout.Label("A/D o flechas: mover   Espacio/W: saltar   Shift: evadir   Mouse: apuntar   Clic izq.: lanzar");
        if (salud != null) GUILayout.Label($"Vida: {salud.VidaActual}/{salud.VidaMaxima}");
        if (jugador != null) GUILayout.Label($"Suelo: {jugador.EstaEnSuelo}   Evadiendo: {jugador.EstaEvadiendo}");
        if (tirador != null && tirador.CeramicaActual != null) GUILayout.Label($"Ceramica: {tirador.CeramicaActual.nombre}");
        GUILayout.EndArea();
    }
}
