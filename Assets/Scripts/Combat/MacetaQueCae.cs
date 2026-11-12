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

        // La caída la comparte con las rocas del derrumbe (Caida)
        yield return new Caida().Caer(maceta, maceta.GetComponent<Collider2D>(), maceta.GetComponent<DanoPorContacto>(),
                                      capaJugador, gravedad, ySuelo);

        GestorAudio.Reproducir(Sfx.ImpactoCeramica);
        if (prefabRota != null) Reciclaje.Sacar(prefabRota, maceta.position, Quaternion.identity);
        Destroy(gameObject);
    }
}
