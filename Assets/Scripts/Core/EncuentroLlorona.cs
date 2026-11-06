using System.Collections;
using UnityEngine;

/// <summary>
/// Final del Nivel 3: al llegar a la orilla del río se dispara la cinemática de La Llorona. La cámara se fija, se calla
/// la música, se oye su lamento y ella aparece flotando sobre el agua, de transparente a sólida. Grita, sale su barra
/// de vida, entra la música de la pelea y empieza el combate.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class EncuentroLlorona : MonoBehaviour
{
    [SerializeField] JefeLlorona jefa;
    [SerializeField] BarraJefe barraJefe;
    [SerializeField] float xAparicion = 85f;
    [SerializeField] float xLlegada = 81f;
    [SerializeField] string nombreJefa = "LA LLORONA";
    [SerializeField] Musica musicaPelea = Musica.Llorona;

    public bool Iniciado { get; private set; }
    public bool PeleaEmpezada { get; private set; }

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        if (barraJefe != null) barraJefe.gameObject.SetActive(false);
        if (jefa != null) jefa.gameObject.SetActive(false);   // Nadie la ve hasta la cinemática
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (Iniciado || jefa == null || otro.GetComponentInParent<PlayerController>() == null) return;
        StartCoroutine(Cinematica());
    }

    IEnumerator Cinematica()
    {
        Iniciado = true;
        var gestor = GestorNivel.Actual;
        var camara = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;

        // La orilla queda cerrada: cámara fija al final del nivel y Mateo no puede regresar
        float mitad = camara != null ? camara.MitadAncho : 10f;
        float centro = camara != null ? camara.Bloquear(camara.LimiteDerecho) : transform.position.x;
        float min = centro - mitad + 0.6f, max = centro + mitad - 0.6f;
        gestor?.LimitarJugador(min, max);
        gestor?.IniciarCinematica();
        gestor?.FijarObjetivo("LIBERA A " + nombreJefa);
        GestorAudio.DetenerMusica(1f);

        // 1) Silencio y un lamento que viene del río
        yield return new WaitForSeconds(0.8f);
        GestorAudio.Reproducir(Sfx.GritoLlorona, 0f);
        camara?.Sacudir(1f, 0.08f);
        yield return new WaitForSeconds(1f);

        // 2) Aparece flotando sobre el agua, de fantasma a sólida
        var t = jefa.transform;
        float y = t.position.y;
        jefa.gameObject.SetActive(true);
        jefa.Dormir();
        jefa.Mirar(-1);
        jefa.Pose("Flotar");
        Vector3 desde = new Vector3(xAparicion, y, 0f), hasta = new Vector3(xLlegada, y, 0f);
        for (float k = 0f; k < 1f; k += Time.deltaTime / 1.6f)
        {
            t.position = Vector3.Lerp(desde, hasta, 1f - (1f - k) * (1f - k));   // Frena al llegar
            jefa.Alfa(k);
            yield return null;
        }
        t.position = hasta;
        jefa.Alfa(1f);

        // 3) Grita frente a Mateo
        jefa.Pose("Grito");
        GestorAudio.Reproducir(Sfx.GritoLlorona, 0f);
        camara?.Sacudir(0.6f, 0.25f);
        gestor?.MostrarLetrero("JEFA", nombreJefa, "¡ROMPE SU HECHIZO!");
        yield return new WaitForSeconds(1.6f);

        // 4) ¡A pelear!
        if (barraJefe != null) barraJefe.Mostrar(jefa.Salud, nombreJefa);
        GestorAudio.CambiarMusica(musicaPelea, 0.5f);
        gestor?.TerminarCinematica();
        jefa.Activar(min + 1f, max - 1f);
        PeleaEmpezada = true;
    }
}
