using System.Collections;
using UnityEngine;

/// <summary>
/// Inicio del Nivel 2 (#50): Mateo llega a Mineral de Pozos montado en el alebrije liberado, galopando desde la orilla.
/// Al detenerse, una bola de fuego de la Bruja cae del cielo, lo tumba del alebrije (que se va volando) y Mateo sigue a
/// pie. Mientras dura es una cinemática: Mateo no se controla y los enemigos esperan.
/// </summary>
public class LlegadaMontado : MonoBehaviour
{
    [SerializeField] MonturaAlebrije montura;
    [SerializeField] BalaEnemiga prefabBola;
    [SerializeField] float xInicio = -9.5f;
    [SerializeField] float xLlegada = -6f;
    [SerializeField] float velocidadGalope = 6f;
    [SerializeField] Vector2 origenBola = new Vector2(7f, 8f);       // Desde dónde cae, respecto a Mateo
    [SerializeField] float velocidadBola = 14f;

    public bool Terminada { get; private set; }

    void Start()
    {
        if (montura == null) { Terminada = true; return; }
        StartCoroutine(Secuencia());
    }

    IEnumerator Secuencia()
    {
        yield return null;                                         // Que todos terminen su Start (GestorNivel, montura)
        var gestor = GestorNivel.Actual;
        var jugador = FindFirstObjectByType<PlayerController>();
        if (jugador == null) { Terminada = true; yield break; }
        var rb = jugador.GetComponent<Rigidbody2D>();
        gestor?.IniciarCinematica();

        // 1) Entra galopando desde la orilla del pueblo
        rb.position = new Vector2(xInicio, rb.position.y);
        jugador.transform.position = rb.position;
        montura.Liberar(jugador.transform.position, 1);
        montura.Montar();
        while (rb.position.x < xLlegada)
        {
            jugador.AplicarEmpuje(new Vector2(velocidadGalope, rb.linearVelocity.y), 0.1f);   // Sin frenar mientras galopa
            yield return new WaitForFixedUpdate();
        }
        jugador.AplicarEmpuje(new Vector2(0f, rb.linearVelocity.y), 0.05f);
        yield return new WaitForSeconds(0.5f);

        // 2) La Bruja de Fuego los recibe con una bola de fuego desde el cielo (no lastima: solo los tumba)
        Vector2 objetivo = rb.position + Vector2.up * 1.5f;
        Vector2 desde = objetivo + origenBola;
        if (prefabBola != null) Reciclaje.Sacar(prefabBola, desde, Quaternion.identity).Disparar(objetivo - desde, velocidadBola, 0);
        yield return new WaitForSeconds(Vector2.Distance(desde, objetivo) / velocidadBola);

        Camera.main?.GetComponent<CameraFollow>()?.Sacudir(0.5f, 0.3f);
        GestorAudio.Reproducir(Sfx.RomperHorno, 0f);
        montura.Derribar("¡LA BRUJA DE FUEGO!", "TE TUMBO DEL ALEBRIJE");
        yield return new WaitForSeconds(1.2f);

        gestor?.TerminarCinematica();
        Terminada = true;
    }
}
