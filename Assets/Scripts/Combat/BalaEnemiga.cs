using UnityEngine;

/// <summary>
/// Bala de los bandidos: vuela en línea recta, lastima a Mateo y se rompe contra paredes, huacales y hornos.
/// Va en la capa EnemyProjectile, que solo choca con Player y Ground (no con otros enemigos ni con la cerámica).
/// La Maceta Escudo de Mateo la detiene si le llega de frente.
/// Sale de Reciclaje y vuelve ahí al chocar o al terminar su vida.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BalaEnemiga : MonoBehaviour
{
    [SerializeField] float tiempoVida = 2f;

    int dano;
    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
    }

    public void Disparar(Vector2 direccion, float velocidad, int danoBala)
    {
        dano = danoBala;
        rb.linearVelocity = direccion.normalized * velocidad;
        transform.right = direccion;
        Reciclaje.Guardar(gameObject, tiempoVida);
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (!gameObject.activeSelf) return;                 // Ya chocó en este cuadro con otra cosa
        var salud = otro.GetComponentInParent<Health>();
        var jugador = otro.GetComponentInParent<PlayerController>();
        if (salud != null && jugador != null)
        {
            if (salud.Invulnerable) return;   // Evadiendo: la bala le pasa de largo
            // Con la Maceta Escudo al frente la bala se estrella en ella (#33)
            var escudo = jugador.GetComponentInChildren<EscudoMaceta>();
            if (escudo == null || !escudo.Bloquea(rb.linearVelocity)) salud.RecibirDano(dano);
        }
        Reciclaje.Guardar(gameObject);
    }
}
