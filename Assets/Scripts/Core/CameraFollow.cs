using UnityEngine;

/// <summary>
/// Cámara lateral estilo Metal Slug: sigue al jugador en X (con un poco de adelanto hacia donde mira),
/// mantiene Y fija y nunca se sale de los límites del nivel.
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

        float x = Mathf.SmoothDamp(transform.position.x, ObjetivoX(), ref velocidadX, suavizado);
        transform.position = new Vector3(x, yFija, transform.position.z);
    }

    float ObjetivoX()
    {
        float deseado = objetivo.position.x + (jugador != null ? jugador.DireccionMirada * adelanto : 0f);

        float mitadAncho = cam.orthographicSize * cam.aspect;
        float min = limiteIzquierdo + mitadAncho;
        float max = limiteDerecho - mitadAncho;
        if (min > max) return (limiteIzquierdo + limiteDerecho) * 0.5f; // Nivel más angosto que la pantalla
        return Mathf.Clamp(deseado, min, max);
    }
}
