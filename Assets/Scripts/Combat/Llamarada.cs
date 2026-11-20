using System.Collections;
using UnityEngine;

/// <summary>
/// Llamarada del Charro Negro (#90): primero se enciende una grieta en el suelo (el aviso para quitarse) y luego brota
/// una columna de fuego infernal más alta que el salto de Mateo. Lastima una sola vez a quien la toque mientras arde;
/// se esquiva saliéndose de la grieta o evadiendo a través de ella. Como el chorro de La Llorona, pero de fuego.
/// Sale de Reciclaje y vuelve ahí sola.
/// </summary>
public class Llamarada : MonoBehaviour
{
    [SerializeField] SpriteRenderer sprite;
    [SerializeField] Sprite cuadroAviso;
    [SerializeField] Sprite[] cuadrosFuego;
    [SerializeField] float aviso = 0.65f;
    [SerializeField] float duracion = 0.8f;
    [SerializeField] int dano = 20;
    [SerializeField] Vector2 caja = new Vector2(1.6f, 4.2f);
    [SerializeField] LayerMask capaJugador;

    public bool Ardiendo { get; private set; }

    void OnEnable()
    {
        Ardiendo = false;
        StartCoroutine(Secuencia());
    }

    IEnumerator Secuencia()
    {
        // La grieta parpadea para que se note
        sprite.sprite = cuadroAviso;
        for (float t = 0f; t < aviso; t += Time.deltaTime)
        {
            sprite.color = Color.Lerp(Color.white, new Color(1f, 0.6f, 0.4f), Mathf.PingPong(t * 8f, 1f));
            yield return null;
        }
        sprite.color = Color.white;

        Ardiendo = true;
        GestorAudio.Reproducir(Sfx.Explosion, 0.1f);
        bool pego = false;
        float porCuadro = duracion / cuadrosFuego.Length;
        for (float t = 0f; t < duracion; t += Time.deltaTime)
        {
            sprite.sprite = cuadrosFuego[Mathf.Min((int)(t / porCuadro), cuadrosFuego.Length - 1)];
            if (!pego) pego = Golpear();
            yield return null;
        }
        Ardiendo = false;
        Reciclaje.Guardar(gameObject);
    }

    bool Golpear()
    {
        Vector2 centro = (Vector2)transform.position + Vector2.up * caja.y / 2f;
        var golpe = Physics2D.OverlapBox(centro, caja, 0f, capaJugador);
        var salud = golpe != null ? golpe.GetComponentInParent<Health>() : null;
        if (salud == null || salud.EsInvulnerable) return false;
        salud.RecibirDano(dano);
        golpe.GetComponentInParent<ReaccionDanoJugador>()?.Empujar(transform.position, 9f);
        return true;
    }
}
