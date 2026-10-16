using System;
using UnityEngine;

/// <summary>
/// Vida genérica: sirve para el Alfarero, enemigos, huacales, hornos, jefes, etc.
/// No sabe nada de quién lo golpea; solo expone eventos para que otros (HUD, animator, IA) reaccionen.
/// </summary>
public class Health : MonoBehaviour
{
    [SerializeField] int vidaMaxima = 100;
    [SerializeField] bool destruirAlMorir = false;   // Enemigos y objetos: true. Jugador: false (lo maneja GestorNivel)

    float invulnerableHasta;

    public int VidaActual { get; private set; }
    public int VidaMaxima => vidaMaxima;
    public bool EstaMuerto => VidaActual <= 0;

    /// <summary>Invulnerabilidad manual mientras esté en true (ej. durante la evasión del jugador).</summary>
    public bool Invulnerable { get; set; }

    /// <summary>True si ahora mismo ignora el daño, ya sea por Invulnerable o por un tiempo (golpe recibido, caguama).</summary>
    public bool EsInvulnerable => Invulnerable || Time.time < invulnerableHasta;
    public float InvulnerableRestante => Mathf.Max(0f, invulnerableHasta - Time.time);

    public event Action<int, int> OnVidaCambiada;    // (vidaActual, vidaMaxima)
    public event Action<int> OnDano;                 // cantidad recibida
    public event Action OnMuerte;

    void Awake()
    {
        VidaActual = vidaMaxima;
    }

    public void RecibirDano(int cantidad)
    {
        if (EstaMuerto || EsInvulnerable || cantidad <= 0) return;

        VidaActual = Mathf.Max(0, VidaActual - cantidad);
        OnDano?.Invoke(cantidad);
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

    /// <summary>Ignora el daño durante unos segundos (no acorta una invulnerabilidad más larga ya activa).</summary>
    public void HacerInvulnerable(float segundos)
    {
        invulnerableHasta = Mathf.Max(invulnerableHasta, Time.time + segundos);
    }
}
