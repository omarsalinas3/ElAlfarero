using System.Collections;
using UnityEngine;

/// <summary>
/// Obstáculo: una maceta cae desde lo alto cuando el jugador pasa por debajo.
/// Primero tiembla (aviso), luego cae; al tocar el suelo o al jugador se hace añicos y daña con DanoPorContacto.
/// Va en un objeto con un trigger ancho (zona de activación); la maceta es un hijo.
/// </summary>
public class MacetaQueCae : MonoBehaviour
{
    [SerializeField] Transform maceta;
    [SerializeField] float tiempoAviso = 0.45f;
    [SerializeField] float gravedad = 30f;
    [SerializeField] float ySuelo = -4.5f;
    [SerializeField] SpriteAnimadoTemporal prefabRota;
    [SerializeField] LayerMask capaJugador;

    bool activada;

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (activada || (capaJugador.value & (1 << otro.gameObject.layer)) == 0) return;
        activada = true;
        StartCoroutine(Caer());
    }

    IEnumerator Caer()
    {
        Vector3 inicio = maceta.position;
        for (float t = 0f; t < tiempoAviso; t += Time.deltaTime)
        {
            maceta.position = inicio + new Vector3(Mathf.Sin(t * 70f) * 0.06f, 0f, 0f);
            yield return null;
        }
        maceta.position = inicio;

        var dano = maceta.GetComponent<DanoPorContacto>();
        var col = maceta.GetComponent<Collider2D>();
        float velocidad = 0f;
        float mitadAlto = col != null ? col.bounds.extents.y : 0.3f;

        while (maceta.position.y - mitadAlto > ySuelo)
        {
            velocidad += gravedad * Time.deltaTime;
            maceta.position += Vector3.down * velocidad * Time.deltaTime;

            // Golpe directo al jugador
            if (col != null && dano != null)
            {
                var golpeado = Physics2D.OverlapBox(col.bounds.center, col.bounds.size, 0f, capaJugador);
                if (golpeado != null && dano.Aplicar(golpeado)) break;
            }
            yield return null;
        }

        GestorAudio.Reproducir(Sfx.ImpactoCeramica);
        if (prefabRota != null) Instantiate(prefabRota, maceta.position, Quaternion.identity);
        Destroy(gameObject);
    }
}
