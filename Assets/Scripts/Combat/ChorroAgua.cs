using System.Collections;
using UnityEngine;

/// <summary>
/// Chorro de agua de La Llorona: primero burbujea en el suelo (el aviso para quitarse) y luego brota hacia arriba.
/// Lastima una sola vez a quien lo toque mientras brota; evadir pasa de largo y la Maceta Escudo no ayuda, porque sale
/// de abajo. Sale de Reciclaje y vuelve ahí solo.
/// </summary>
public class ChorroAgua : MonoBehaviour
{
    [SerializeField] SpriteRenderer sprite;
    [SerializeField] Sprite[] cuadrosAviso;
    [SerializeField] Sprite[] cuadrosChorro;
    [SerializeField] float aviso = 0.8f;
    [SerializeField] float duracion = 0.7f;
    [SerializeField] float cuadrosPorSegundo = 12f;
    [SerializeField] int dano = 18;
    [SerializeField] Vector2 caja = new Vector2(1.2f, 3.6f);
    [SerializeField] LayerMask capaJugador;

    public bool Brotando { get; private set; }

    void OnEnable()
    {
        Brotando = false;
        StartCoroutine(Secuencia());
    }

    IEnumerator Secuencia()
    {
        for (float t = 0f; t < aviso; t += Time.deltaTime)
        {
            sprite.sprite = cuadrosAviso[(int)(t * cuadrosPorSegundo) % cuadrosAviso.Length];
            yield return null;
        }

        Brotando = true;
        GestorAudio.Reproducir(Sfx.ChorroAgua, 0.1f);
        bool pego = false;
        for (float t = 0f; t < duracion; t += Time.deltaTime)
        {
            sprite.sprite = cuadrosChorro[(int)(t * cuadrosPorSegundo) % cuadrosChorro.Length];
            if (!pego) pego = Golpear();
            yield return null;
        }
        Brotando = false;
        Reciclaje.Guardar(gameObject);
    }

    bool Golpear()
    {
        Vector2 centro = (Vector2)transform.position + Vector2.up * caja.y / 2f;
        var golpe = Physics2D.OverlapBox(centro, caja, 0f, capaJugador);
        var salud = golpe != null ? golpe.GetComponentInParent<Health>() : null;
        if (salud == null || salud.EsInvulnerable) return false;
        salud.RecibirDano(dano);
        golpe.GetComponentInParent<ReaccionDanoJugador>()?.Empujar(transform.position, 8f);
        return true;
    }
}
