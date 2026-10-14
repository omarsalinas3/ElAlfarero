using UnityEngine;

/// <summary>
/// Proyectil de cerámica. Se mueve en línea recta y daña a lo primero que toque dentro de las capas de impacto.
/// Requisitos del prefab: Rigidbody2D, un Collider2D con "Is Trigger" activado y un SpriteRenderer.
/// TODO (Sprint 4/6): cambiar Instantiate/Destroy por object pooling para rendimiento.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    Rigidbody2D rb;
    CeramicaData datos;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;   // No lo afecta la gravedad ni empujones
        rb.gravityScale = 0f;
    }

    /// <summary>Lo llama quien dispara justo después de instanciarlo.</summary>
    public void Inicializar(CeramicaData ceramica, Vector2 direccion)
    {
        datos = ceramica;
        rb.linearVelocity = direccion.normalized * ceramica.velocidadProyectil;

        // El sprite mira a la derecha; al lanzar hacia la izquierda la rotación lo dejaría de cabeza
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.flipY = direccion.x < 0f;
        Destroy(gameObject, ceramica.tiempoVida);  // Alcance máximo
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (datos == null) return;

        // Filtro por capa: ignoramos todo lo que no esté en capasImpacto (ej. el propio jugador)
        if ((datos.capasImpacto.value & (1 << otro.gameObject.layer)) == 0) return;

        // El Health puede estar en el padre del collider, por eso GetComponentInParent
        Health salud = otro.GetComponentInParent<Health>();
        if (salud != null) salud.RecibirDano(datos.dano);

        if (datos.prefabImpacto != null) Instantiate(datos.prefabImpacto, transform.position, Quaternion.identity);
        // TODO (Sprint 6): SFX según el tipo de cerámica
        Destroy(gameObject);
    }
}
