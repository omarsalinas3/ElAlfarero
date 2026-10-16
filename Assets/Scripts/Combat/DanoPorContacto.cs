using UnityEngine;

/// <summary>
/// Daña a lo que lo toque dentro de capasObjetivo (normalmente el jugador): brasas, macetas que caen, esqueletos.
/// Funciona con colliders sólidos o triggers. Si el objetivo tiene ReaccionDanoJugador, también lo empuja.
/// </summary>
public class DanoPorContacto : MonoBehaviour
{
    [SerializeField] int dano = 10;
    [SerializeField] float empuje = 7f;
    [SerializeField] LayerMask capasObjetivo;

    public int Dano => dano;

    void OnCollisionEnter2D(Collision2D c) => Aplicar(c.collider);
    void OnCollisionStay2D(Collision2D c) => Aplicar(c.collider);
    void OnTriggerEnter2D(Collider2D c) => Aplicar(c);
    void OnTriggerStay2D(Collider2D c) => Aplicar(c);

    /// <summary>Devuelve true si llegó a dañar.</summary>
    public bool Aplicar(Collider2D otro)
    {
        if (!enabled || (capasObjetivo.value & (1 << otro.gameObject.layer)) == 0) return false;

        Health salud = otro.GetComponentInParent<Health>();
        if (salud == null || salud.EsInvulnerable || salud.EstaMuerto) return false;

        salud.RecibirDano(dano);
        var reaccion = salud.GetComponent<ReaccionDanoJugador>();
        if (reaccion != null) reaccion.Empujar(transform.position, empuje);
        return true;
    }
}
