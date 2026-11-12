using System.Collections;
using UnityEngine;

/// <summary>
/// Roca del derrumbe (#88). Sale de Reciclaje: Soltar() marca en el suelo dónde va a caer (una sombra que crece y
/// polvo que se desprende del techo), espera el aviso, cae con la misma Caida que las macetas y al chocar se hace
/// pedazos con su propio sonido. Lastima a Mateo si le cae encima (DanoPorContacto).
/// </summary>
[RequireComponent(typeof(DanoPorContacto))]
public class RocaQueCae : MonoBehaviour
{
    [SerializeField] Transform roca;                 // Lo que cae (sprite + collider)
    [SerializeField] SpriteRenderer sombra;          // Marca en el suelo: es el aviso de dónde va a caer
    [SerializeField] Sprite[] variantes;             // Una al azar por roca
    [SerializeField] float gravedad = 34f;
    [SerializeField] LayerMask capaJugador;

    readonly Caida caida = new Caida();

    public bool Cayendo { get; private set; }
    public bool Golpeo => caida.Golpeo;

    /// <summary>Cae sobre x desde altura yInicio hasta ySuelo, después de "aviso" segundos.</summary>
    public void Soltar(float x, float yInicio, float ySuelo, float aviso)
    {
        transform.position = new Vector3(x, ySuelo, 0f);
        roca.position = new Vector3(x, yInicio, 0f);
        roca.gameObject.SetActive(false);
        if (variantes != null && variantes.Length > 0)
            roca.GetComponent<SpriteRenderer>().sprite = variantes[Random.Range(0, variantes.Length)];
        StopAllCoroutines();
        StartCoroutine(Secuencia(yInicio, ySuelo, aviso));
    }

    IEnumerator Secuencia(float yInicio, float ySuelo, float aviso)
    {
        Cayendo = true;
        // Aviso: la sombra crece en el suelo y se desprende polvo del techo
        sombra.gameObject.SetActive(true);
        float x = transform.position.x;
        for (float t = 0f; t < aviso; t += Time.deltaTime)
        {
            float k = t / aviso;
            sombra.transform.localScale = new Vector3(Mathf.Lerp(0.3f, 1f, k), Mathf.Lerp(0.3f, 1f, k), 1f);
            sombra.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.15f, 0.55f, k));
            if (Random.value < Time.deltaTime * 6f) Efectos.Particulas(TipoParticula.Polvo, new Vector2(x + Random.Range(-0.4f, 0.4f), yInicio - 0.5f));
            yield return null;
        }

        roca.gameObject.SetActive(true);
        yield return caida.Caer(roca, roca.GetComponent<Collider2D>(), GetComponent<DanoPorContacto>(), capaJugador, gravedad, ySuelo);

        GestorAudio.Reproducir(Sfx.RocaImpacto, 0.1f);
        Efectos.Particulas(TipoParticula.Astillas, roca.position);
        Efectos.Particulas(TipoParticula.Aterrizaje, new Vector2(x, ySuelo));
        var cam = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (cam != null) cam.Sacudir(0.15f, 0.12f);
        sombra.gameObject.SetActive(false);
        roca.gameObject.SetActive(false);
        Cayendo = false;
        Reciclaje.Guardar(gameObject);
    }
}
