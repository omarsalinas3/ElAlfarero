using System.Collections;
using UnityEngine;

/// <summary>
/// Final del Nivel 2: al entrar al patio de la capilla se dispara la cinemática del Jinete sin Cabeza.
/// La cámara se fija en el patio, retumban cascos, el jinete sale galopando de entre las ruinas (empieza oscuro y
/// transparente, como un fantasma), se planta frente a Mateo, se encabrita y empieza la pelea.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class EncuentroJinete : MonoBehaviour
{
    [SerializeField] JefeJinete jefe;
    [SerializeField] BarraJefe barraJefe;
    [SerializeField] float xAparicion = 90f;                          // Entre las ruinas, a la derecha del patio
    [SerializeField] float xLlegada = 84f;
    [SerializeField] float ySuelo = -4.5f;
    [SerializeField] string nombreJefe = "JINETE SIN CABEZA";
    [SerializeField] Musica musicaPelea = Musica.Nivel2;

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

        // El patio queda cerrado: cámara fija al final del nivel y Mateo no puede regresar
        float mitad = camara != null ? camara.MitadAncho : 10f;
        float centro = camara != null ? camara.Bloquear(camara.LimiteDerecho) : transform.position.x;
        float min = centro - mitad + 0.6f, max = centro + mitad - 0.6f;
        gestor?.LimitarJugador(min, max);
        gestor?.IniciarCinematica();
        gestor?.FijarObjetivo("DERROTA AL " + nombreJefe);
        GestorAudio.DetenerMusica(1f);

        // 1) Retumban cascos en la oscuridad
        yield return new WaitForSeconds(0.6f);
        GestorAudio.Reproducir(Sfx.Estampida, 0f);
        camara?.Sacudir(1.4f, 0.1f);
        yield return new WaitForSeconds(0.9f);

        // 2) Sale galopando de entre las ruinas, de fantasma a sólido
        var t = jefe.transform;
        var sprites = jefe.GetComponentsInChildren<SpriteRenderer>(true);
        var colores = System.Array.ConvertAll(sprites, s => s.color);   // Su color propio (el arte provisional va teñido)
        jefe.gameObject.SetActive(true);
        jefe.Dormir();
        jefe.Mirar(-1);
        jefe.Pose("Galope");
        Vector3 desde = new Vector3(xAparicion, ySuelo, 0f), hasta = new Vector3(xLlegada, ySuelo, 0f);
        for (float k = 0f; k < 1f; k += Time.deltaTime / 1.4f)
        {
            t.position = Vector3.Lerp(desde, hasta, 1f - (1f - k) * (1f - k));   // Frena al llegar
            for (int i = 0; i < sprites.Length; i++) sprites[i].color = Color.Lerp(new Color(0.3f, 0.3f, 0.5f, 0f), colores[i], k);
            yield return null;
        }
        t.position = hasta;
        for (int i = 0; i < sprites.Length; i++) sprites[i].color = colores[i];

        // 3) Se encabrita frente a Mateo
        jefe.Pose("Golpe");
        GestorAudio.Reproducir(Sfx.RugidoAlebrije, 0f);
        camara?.Sacudir(0.6f, 0.25f);
        gestor?.MostrarLetrero("JEFE", nombreJefe, "¡ACABALO!");
        yield return new WaitForSeconds(1.6f);

        // 4) ¡A pelear!
        if (barraJefe != null) barraJefe.Mostrar(jefe.Salud, nombreJefe);
        GestorAudio.CambiarMusica(musicaPelea, 0.5f);
        gestor?.TerminarCinematica();
        jefe.Activar(min + 1f, max - 1f);
        PeleaEmpezada = true;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(new Vector3(xAparicion, ySuelo, 0f), new Vector3(xLlegada, ySuelo, 0f));
    }
}
