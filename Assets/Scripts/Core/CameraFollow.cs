using UnityEngine;

/// <summary>
/// Cámara lateral estilo Metal Slug: sigue al jugador en X (con un poco de adelanto hacia donde mira),
/// mantiene Y fija y nunca se sale de los límites del nivel.
/// Se puede fijar en un punto (emboscadas, jefe) y sacudir (impactos, rugidos).
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform objetivo;
    [SerializeField] float suavizado = 0.15f;       // Tiempo de SmoothDamp (menor = más pegada)
    [SerializeField] float adelanto = 2f;           // Unidades por delante del jugador
    [SerializeField] float yFija = 0f;

    [Header("Límites del nivel (borde izquierdo y derecho del mundo)")]
    [SerializeField] float limiteIzquierdo = -10.75f;
    [SerializeField] float limiteDerecho = 75.25f;

    Camera cam;
    PlayerController jugador;
    float velocidadX;
    float? xFija;
    float sacudidaHasta, fuerzaSacudida;
    Vector2 desfase;

    /// <summary>Mitad del ancho visible en unidades del mundo.</summary>
    public float MitadAncho => cam.orthographicSize * cam.aspect;
    public float LimiteDerecho => limiteDerecho;
    public bool Bloqueada => xFija.HasValue;

    /// <summary>Deja la cámara quieta centrada en x (dentro de los límites del nivel). Devuelve el centro real.</summary>
    public float Bloquear(float x)
    {
        xFija = x;
        return Limitar(x);
    }

    public void Desbloquear() => xFija = null;

    public void Sacudir(float segundos, float fuerza)
    {
        sacudidaHasta = Mathf.Max(sacudidaHasta, Time.time + segundos);
        fuerzaSacudida = Mathf.Max(Time.time < sacudidaHasta ? fuerzaSacudida : 0f, fuerza);
    }

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (objetivo != null) jugador = objetivo.GetComponent<PlayerController>();
    }

    void Start()
    {
        // Arranca ya centrada en el jugador para no "volar" al inicio de la escena
        if (objetivo != null) transform.position = new Vector3(ObjetivoX(), yFija, transform.position.z);
    }

    void LateUpdate()
    {
        if (objetivo == null) return;

        float x = Mathf.SmoothDamp(transform.position.x - desfase.x, ObjetivoX(), ref velocidadX, xFija.HasValue ? 0.5f : suavizado);
        desfase = Time.time < sacudidaHasta
            ? Random.insideUnitCircle * fuerzaSacudida * Mathf.Clamp01((sacudidaHasta - Time.time) * 4f)
            : Vector2.zero;
        transform.position = new Vector3(x + desfase.x, yFija + desfase.y, transform.position.z);
    }

    float ObjetivoX()
    {
        float deseado = xFija ?? objetivo.position.x + (jugador != null ? jugador.DireccionMirada * adelanto : 0f);
        return Limitar(deseado);
    }

    float Limitar(float deseado)
    {
        float mitadAncho = cam.orthographicSize * cam.aspect;
        float min = limiteIzquierdo + mitadAncho;
        float max = limiteDerecho - mitadAncho;
        if (min > max) return (limiteIzquierdo + limiteDerecho) * 0.5f; // Nivel más angosto que la pantalla
        return Mathf.Clamp(deseado, min, max);
    }
}
