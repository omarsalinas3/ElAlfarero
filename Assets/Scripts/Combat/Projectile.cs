using UnityEngine;

/// <summary>
/// Proyectil de cerámica. Se mueve en línea recta (o en arco si su cerámica tiene gravedad) y daña a lo primero que
/// toque dentro de las capas de impacto. El Barro Negro además explota: al chocar o al acabarse la mecha daña todo lo
/// que esté en su radio (enemigos, huacales y hornos).
/// Requisitos del prefab: Rigidbody2D, un Collider2D con "Is Trigger" activado y un SpriteRenderer.
/// Sale de Reciclaje y vuelve ahí al chocar o al terminar su alcance (no se destruye).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    Rigidbody2D rb;
    CeramicaData datos;
    float mecha;                                     // Momento en que explota solo (granadas)
    static readonly Collider2D[] alcanzados = new Collider2D[24];

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;   // No lo afectan empujones; la gravedad del arco se aplica aquí
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

        if (Explota) mecha = Time.time + ceramica.tiempoVida;          // Explota él mismo, no solo desaparece
        else Reciclaje.Guardar(gameObject, ceramica.tiempoVida);        // Alcance máximo
    }

    bool Explota => datos != null && datos.radioExplosion > 0f;

    void Update()
    {
        if (Explota && Time.time >= mecha) Explotar();
    }

    void FixedUpdate()
    {
        if (datos == null || datos.gravedad <= 0f) return;
        rb.linearVelocity += Vector2.down * datos.gravedad * Time.fixedDeltaTime;
        Vector2 v = rb.linearVelocity;
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg);   // Apunta hacia donde cae
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (datos == null || !gameObject.activeSelf) return;   // Ya chocó en este cuadro con otra cosa

        // Filtro por capa: ignoramos todo lo que no esté en capasImpacto (ej. el propio jugador)
        if ((datos.capasImpacto.value & (1 << otro.gameObject.layer)) == 0) return;

        if (Explota) { Explotar(); return; }

        // El Health puede estar en el padre del collider, por eso GetComponentInParent
        Health salud = otro.GetComponentInParent<Health>();
        if (salud != null) salud.RecibirDano(datos.dano);

        if (datos.prefabImpacto != null) Reciclaje.Sacar(datos.prefabImpacto, transform.position, Quaternion.identity);
        Efectos.Particulas(TipoParticula.Fragmentos, transform.position);
        GestorAudio.Reproducir(Sfx.ImpactoCeramica, 0.12f);
        Reciclaje.Guardar(gameObject);
    }

    void Explotar()
    {
        Vector2 centro = transform.position;
        var filtro = new ContactFilter2D { useLayerMask = true, layerMask = datos.capasImpacto, useTriggers = true };
        int n = Physics2D.OverlapCircle(centro, datos.radioExplosion, filtro, alcanzados);
        for (int i = 0; i < n; i++)
        {
            Health salud = alcanzados[i].GetComponentInParent<Health>();
            if (salud == null || salud.GetComponent<PlayerController>() != null) continue;
            bool repetido = false;                         // Un enemigo con varios colliders recibe un solo golpe
            for (int j = 0; j < i && !repetido; j++) repetido = alcanzados[j].GetComponentInParent<Health>() == salud;
            if (!repetido) salud.RecibirDano(datos.danoExplosion);
        }

        if (datos.prefabExplosion != null) Reciclaje.Sacar(datos.prefabExplosion, centro, Quaternion.identity);
        GestorAudio.Reproducir(Sfx.Explosion, 0.08f);
        Efectos.Particulas(TipoParticula.Chispas, centro);
        Efectos.GolpeFuerte(0.05f, 0.18f, 0.25f);
        datos = null;                                      // No vuelve a explotar mientras regresa a la reserva
        Reciclaje.Guardar(gameObject);
    }
}
