using System.Collections;
using UnityEngine;

/// <summary>
/// Pelea final (#90): al entrar a la arena se dispara la cinemática del Charro Negro. Retumban cascos, el Charro sale
/// galopando de la oscuridad en su caballo de fuego, se encabrita frente a Mateo y empieza la fase 1. Cuando el caballo
/// cae, el Charro le ofrece a Mateo el pacto (una bolsa de oro a cambio de renunciar a Carmen, fiel a la leyenda);
/// Mateo se niega y sigue la fase 2, a pie. Cada fase muestra su propia barra de vida.
/// Si Mateo cayó en la fase 2, al reintentar la escena empieza directo ahí: Mateo aparece en la arena y el Charro, a pie.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class EncuentroCharro : MonoBehaviour
{
    const string Nombre = "EL CHARRO NEGRO";

    [SerializeField] JefeCharro jefe;
    [SerializeField] BarraJefe barraJefe;
    [SerializeField] float xAparicion = 90f;                          // Sale de la oscuridad, a la derecha de la arena
    [SerializeField] float xLlegada = 84f;
    [SerializeField] float ySuelo = -4.5f;
    [SerializeField] Musica musicaPelea = Musica.Llorona;             // Provisional: la pista propia llega con la #93
    [SerializeField] LineaDialogo[] pacto;

    public bool Iniciado { get; private set; }
    public bool PeleaEmpezada { get; private set; }
    public bool PactoTerminado { get; private set; }
    /// <summary>La escena empezó desde el punto de control de la fase 2.</summary>
    public bool Retomado { get; private set; }
    public LineaDialogo[] LineasPacto => pacto;

    float min, max;

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        if (barraJefe != null) barraJefe.gameObject.SetActive(false);
        if (jefe != null) jefe.gameObject.SetActive(false);   // Nadie lo ve hasta la cinemática
    }

    void Start()
    {
        if (jefe != null && JefeCharro.TomarPuntoDeControl()) StartCoroutine(Retomar());
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (Iniciado || jefe == null || otro.GetComponentInParent<PlayerController>() == null) return;
        StartCoroutine(Cinematica());
    }

    /// <summary>Cámara fija en la arena y Mateo sin poder salirse.</summary>
    void CerrarArena(GestorNivel gestor, CameraFollow camara)
    {
        float mitad = camara != null ? camara.MitadAncho : 10f;
        float centro = camara != null ? camara.Bloquear(camara.LimiteDerecho) : transform.position.x;
        min = centro - mitad + 0.6f;
        max = centro + mitad - 0.6f;
        gestor?.LimitarJugador(min, max);
        gestor?.IniciarCinematica();
        gestor?.FijarObjetivo("DERROTA AL " + Nombre);
    }

    IEnumerator Cinematica()
    {
        Iniciado = true;
        var gestor = GestorNivel.Actual;
        var camara = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        CerrarArena(gestor, camara);
        GestorAudio.DetenerMusica(1f);

        // 1) Retumban cascos en la oscuridad
        yield return new WaitForSeconds(0.6f);
        GestorAudio.Reproducir(Sfx.Estampida, 0f);
        camara?.Sacudir(1.6f, 0.12f);
        yield return new WaitForSeconds(1f);

        // 2) Sale galopando de la oscuridad, de sombra a sólido
        var t = jefe.transform;
        var sprites = jefe.GetComponentsInChildren<SpriteRenderer>();
        jefe.gameObject.SetActive(true);
        jefe.Dormir();
        jefe.Mirar(-1);
        jefe.Pose("Galope");
        Vector3 desde = new Vector3(xAparicion, ySuelo, 0f), hasta = new Vector3(xLlegada, ySuelo, 0f);
        for (float k = 0f; k < 1f; k += Time.deltaTime / 1.4f)
        {
            t.position = Vector3.Lerp(desde, hasta, 1f - (1f - k) * (1f - k));   // Frena al llegar
            foreach (var s in sprites) s.color = Color.Lerp(new Color(0.4f, 0f, 0f, 0f), Color.white, k);
            yield return null;
        }
        t.position = hasta;
        foreach (var s in sprites) s.color = Color.white;

        // 3) Se encabrita frente a Mateo
        jefe.Pose("Encabritar");
        GestorAudio.Reproducir(Sfx.RugidoAlebrije, 0f);
        camara?.Sacudir(0.7f, 0.3f);
        gestor?.MostrarLetrero("JEFE FINAL", Nombre, "¡RESCATA A CARMEN!");
        yield return new WaitForSeconds(1.8f);

        // 4) ¡A pelear!
        jefe.OnCaballoVencido += AlCaerCaballo;
        if (barraJefe != null) barraJefe.Mostrar(jefe.SaludCaballo, Nombre);
        GestorAudio.CambiarMusica(musicaPelea, 0.5f);
        gestor?.TerminarCinematica();
        jefe.Activar(min + 1f, max - 1f);
        PeleaEmpezada = true;
    }

    void AlCaerCaballo()
    {
        jefe.OnCaballoVencido -= AlCaerCaballo;
        StartCoroutine(Pacto());
    }

    IEnumerator Pacto()
    {
        var gestor = GestorNivel.Actual;
        gestor?.IniciarCinematica();
        yield return new WaitForSeconds(0.5f);

        // El Charro ofrece el oro; Mateo se niega. Si no hay cuadro de diálogo (o se salta), sigue igual
        var cuadro = CuadroDialogo.Actual;
        if (cuadro != null && pacto != null && pacto.Length > 0)
        {
            bool termino = false;
            cuadro.Mostrar(pacto, () => termino = true);
            float limite = Time.time + 30f;
            while (!termino && Time.time < limite) yield return null;
        }
        PactoTerminado = true;

        jefe.Pose("Furia");
        GestorAudio.Reproducir(Sfx.RugidoAlebrije, 0f);
        Camera.main?.GetComponent<CameraFollow>()?.Sacudir(0.8f, 0.25f);
        gestor?.MostrarLetrero("", "¡NO HAY TRATO!", "¡LIBERA A CARMEN!");
        yield return new WaitForSeconds(1.2f);
        EmpezarFase2();
    }

    /// <summary>Reintento tras caer en la fase 2: Mateo aparece en la arena y el Charro lo espera a pie.</summary>
    IEnumerator Retomar()
    {
        Iniciado = true;
        Retomado = true;
        yield return null;                                // Que el nivel termine de arrancar

        var jugador = FindFirstObjectByType<PlayerController>();
        if (jugador != null)
        {
            var rbJugador = jugador.GetComponent<Rigidbody2D>();
            Vector2 p = new Vector2(transform.position.x + 1f, ySuelo + 0.1f);
            rbJugador.position = p;
            jugador.transform.position = p;
        }
        yield return null;                                // La cámara llega con Mateo antes de fijarse

        var gestor = GestorNivel.Actual;
        CerrarArena(gestor, Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null);
        jefe.gameObject.SetActive(true);
        jefe.Dormir();
        jefe.PrepararFase2(min + 1f, max - 1f);
        jefe.transform.position = new Vector3(xLlegada, ySuelo, 0f);
        jefe.Mirar(-1);
        gestor?.MostrarLetrero("", "¡OTRA VEZ, ALFARERO!", Nombre + " TE ESPERA");
        yield return new WaitForSeconds(1.5f);
        PactoTerminado = true;
        EmpezarFase2();
    }

    void EmpezarFase2()
    {
        if (barraJefe != null) barraJefe.Mostrar(jefe.SaludPie, Nombre);
        GestorAudio.CambiarMusica(musicaPelea, 0.5f);
        GestorNivel.Actual?.TerminarCinematica();
        jefe.EmpezarFase2();
        PeleaEmpezada = true;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(xAparicion, ySuelo, 0f), new Vector3(xLlegada, ySuelo, 0f));
    }
}
