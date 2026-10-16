using UnityEngine;

/// <summary>
/// Cómo reacciona Mateo a un golpe: sonido, un segundo de invulnerabilidad con parpadeo y un empujón
/// hacia atrás. (El destello rojo lo pone ParpadeoDano.)
/// </summary>
[RequireComponent(typeof(Health))]
public class ReaccionDanoJugador : MonoBehaviour
{
    [SerializeField] float invulnerabilidadTrasGolpe = 1f;
    [SerializeField] SpriteRenderer spriteJugador;

    Health salud;
    PlayerController controlador;
    InventarioJugador inventario;
    float parpadeoHasta;

    void Awake()
    {
        salud = GetComponent<Health>();
        controlador = GetComponent<PlayerController>();
        inventario = GetComponent<InventarioJugador>();
        if (spriteJugador == null) spriteJugador = GetComponentInChildren<SpriteRenderer>();
    }

    void OnEnable() { salud.OnDano += AlRecibirDano; }
    void OnDisable() { salud.OnDano -= AlRecibirDano; }

    void AlRecibirDano(int cantidad)
    {
        GestorAudio.Reproducir(Sfx.DanoJugador);
        if (salud.EstaMuerto) return;
        salud.HacerInvulnerable(invulnerabilidadTrasGolpe);
        parpadeoHasta = Time.time + invulnerabilidadTrasGolpe;
    }

    /// <summary>Lo llama DanoPorContacto: empuja en dirección contraria a la fuente del daño.</summary>
    public void Empujar(Vector3 desde, float fuerza)
    {
        if (controlador == null || fuerza <= 0f) return;
        float lado = Mathf.Sign(transform.position.x - desde.x);
        if (lado == 0f) lado = -controlador.DireccionMirada;
        controlador.AplicarEmpuje(new Vector2(lado * fuerza, fuerza * 0.8f));
    }

    void Update()
    {
        if (spriteJugador == null || (inventario != null && inventario.CaguamaActiva)) return;
        bool parpadeando = Time.time < parpadeoHasta && !salud.EstaMuerto;
        var c = spriteJugador.color;
        c.a = parpadeando && Mathf.Repeat(Time.time * 12f, 1f) < 0.5f ? 0.25f : 1f;
        spriteJugador.color = c;
    }
}
