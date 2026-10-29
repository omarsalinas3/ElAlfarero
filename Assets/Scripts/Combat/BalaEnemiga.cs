using UnityEngine;

/// <summary>
/// Bala de los bandidos: vuela en línea recta, lastima a Mateo y se rompe contra paredes, huacales y hornos.
/// Va en la capa EnemyProjectile, que solo choca con Player y Ground (no con otros enemigos ni con la cerámica).
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
        if (salud != null && otro.GetComponentInParent<PlayerController>() != null)
        {
            if (salud.Invulnerable) return;   // Evadiendo: la bala le pasa de largo
            salud.RecibirDano(dano);
        }
        Reciclaje.Guardar(gameObject);
    }
}
