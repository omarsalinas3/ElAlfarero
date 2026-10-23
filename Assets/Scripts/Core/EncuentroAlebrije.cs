using System.Collections;
using UnityEngine;

/// <summary>
/// Final del Nivel 1: al llegar a la plaza de la parroquia se dispara la cinemática del Alebrije Gigante.
/// La cámara se fija en la plaza, retumba, el alebrije aparece entre las torres de la iglesia (pequeño y oscuro,
/// como si estuviera lejos), crece, se lanza volando, aterriza frente a la escalinata, ruge y empieza la pelea.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class EncuentroAlebrije : MonoBehaviour
{
    [SerializeField] JefeAlebrije jefe;
    [SerializeField] BarraJefe barraJefe;
    [SerializeField] Vector2 puntoTorres = new Vector2(70f, 3f);     // Entre las dos torres del fondo
    [SerializeField] float xAterrizaje = 72f;
    [SerializeField] float ySuelo = -4.5f;
    [SerializeField] string nombreJefe = "ALEBRIJE GIGANTE";

    public bool Iniciado { get; private set; }
    public bool PeleaEmpezada { get; private set; }

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        if (barraJefe != null) barraJefe.gameObject.SetActive(false);
        if (jefe != null) jefe.gameObject.SetActive(false);   // Nadie lo ve hasta la cinemática
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (Iniciado || jefe == null || otro.GetComponentInParent<PlayerController>() == null) return;
        StartCoroutine(Cinematica());
    }

    IEnumerator Cinematica()
    {
        Iniciado = true;
        var gestor = GestorNivel.Actual;
        var camara = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;

        // La plaza queda cerrada: cámara fija al final del nivel y Mateo no puede regresar
        float mitad = camara != null ? camara.MitadAncho : 10f;
        float centro = camara != null ? camara.Bloquear(camara.LimiteDerecho) : transform.position.x;
        float min = centro - mitad + 0.6f, max = centro + mitad - 0.6f;
        gestor?.LimitarJugador(min, max);
        gestor?.IniciarCinematica();
        gestor?.FijarObjetivo("DERROTA AL " + nombreJefe);
        GestorAudio.DetenerMusica(1f);

        yield return new WaitForSeconds(0.8f);
        camara?.Sacudir(1.2f, 0.12f);                                 // Algo retumba en la iglesia
        GestorAudio.Reproducir(Sfx.RugidoAlebrije, 0f);
        yield return new WaitForSeconds(1f);

        // 1) Asoma entre las torres: chiquito y oscuro, como si estuviera lejos, y crece
        var t = jefe.transform;
        float escala = Mathf.Abs(t.localScale.x);
        var sprites = jefe.GetComponentsInChildren<SpriteRenderer>();
        jefe.gameObject.SetActive(true);
        jefe.Dormir();
        jefe.Mirar(-1);
        jefe.Pose("Rugido");
        t.position = puntoTorres;
        Color lejos = new Color(0.25f, 0.2f, 0.35f, 0f);
        for (float k = 0f; k < 1f; k += Time.deltaTime / 1.3f)
        {
            Colorear(sprites, Color.Lerp(lejos, new Color(0.55f, 0.45f, 0.65f, 1f), k));
            Escalar(t, escala * Mathf.Lerp(0.12f, 0.3f, k));
            yield return null;
        }

        // 2) Se lanza volando hacia la plaza: baja en arco, crece y recupera su color
        jefe.Pose("Salto");
        GestorAudio.Reproducir(Sfx.RugidoAlebrije, 0f);
        Vector2 desde = puntoTorres, hasta = new Vector2(xAterrizaje, ySuelo);
        for (float k = 0f; k < 1f; k += Time.deltaTime / 1.1f)
        {
            float x = Mathf.Lerp(desde.x, hasta.x, k);
            float y = Mathf.Lerp(desde.y, hasta.y, k * k) + Mathf.Sin(k * Mathf.PI) * 2f;
            t.position = new Vector3(x, y, 0f);
            Escalar(t, escala * Mathf.Lerp(0.3f, 1f, k));
            Colorear(sprites, Color.Lerp(new Color(0.55f, 0.45f, 0.65f, 1f), Color.white, k));
            yield return null;
        }
        t.position = hasta;
        Escalar(t, escala);
        Colorear(sprites, Color.white);

        // 3) Aterriza con temblor y ruge
        jefe.Pose("Aterrizaje");
        GestorAudio.Reproducir(Sfx.RomperHorno, 0f);
        camara?.Sacudir(0.5f, 0.45f);
        yield return new WaitForSeconds(0.6f);
        jefe.Pose("Rugido");
        GestorAudio.Reproducir(Sfx.RugidoAlebrije, 0f);
        camara?.Sacudir(1f, 0.2f);
        gestor?.MostrarLetrero("JEFE", nombreJefe, "¡ACABALO!");
        yield return new WaitForSeconds(1.6f);

        // 4) ¡A pelear!
        if (barraJefe != null) barraJefe.Mostrar(jefe.Salud, nombreJefe);
        GestorAudio.CambiarMusica(Musica.Nivel1, 0.5f);
        gestor?.TerminarCinematica();
        jefe.Activar(min, max);
        PeleaEmpezada = true;
    }

    static void Colorear(SpriteRenderer[] sprites, Color c)
    {
        foreach (var s in sprites) s.color = c;
    }

    static void Escalar(Transform t, float escala)
    {
        float signo = Mathf.Sign(t.localScale.x);
        t.localScale = new Vector3(escala * signo, escala, 1f);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(puntoTorres, 0.5f);
        Gizmos.DrawLine(puntoTorres, new Vector3(xAterrizaje, ySuelo, 0f));
    }
}
