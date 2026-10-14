using System;
using UnityEngine;

/// <summary>
/// Vida genérica: sirve para el Alfarero, bandidos, calaveras, jefes, etc.
/// No sabe nada de quién lo golpea; solo expone eventos para que otros (HUD, animator, IA) reaccionen.
/// </summary>
public class Health : MonoBehaviour
{
    [SerializeField] int vidaMaxima = 100;
    [SerializeField] bool destruirAlMorir = false;   // Enemigos: true. Jugador: false (lo maneja un GameManager después)

    public int VidaActual { get; private set; }
    public int VidaMaxima => vidaMaxima;
    public bool EstaMuerto => VidaActual <= 0;

    /// <summary>Si es true ignora el daño (ej. durante la evasión del jugador).</summary>
    public bool Invulnerable { get; set; }

    public event Action<int, int> OnVidaCambiada;    // (vidaActual, vidaMaxima)
    public event Action OnMuerte;

    void Awake()
    {
        VidaActual = vidaMaxima;
    }

    public void RecibirDano(int cantidad)
    {
        if (EstaMuerto || Invulnerable || cantidad <= 0) return;

        VidaActual = Mathf.Max(0, VidaActual - cantidad);
        OnVidaCambiada?.Invoke(VidaActual, vidaMaxima);

        if (EstaMuerto)
        {
            OnMuerte?.Invoke();
            if (destruirAlMorir) Destroy(gameObject);
        }
    }

    public void Curar(int cantidad)
    {
        if (EstaMuerto || cantidad <= 0) return;

        VidaActual = Mathf.Min(vidaMaxima, VidaActual + cantidad);
        OnVidaCambiada?.Invoke(VidaActual, vidaMaxima);
    }
}
