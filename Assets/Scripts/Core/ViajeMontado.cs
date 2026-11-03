using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Escena corta entre el Nivel 1 y el 2 (#57): Mateo sale de Dolores montado en el alebrije liberado, siguiendo las
/// huellas de fuego que dejó el caballo del Charro Negro, rumbo a Mineral de Pozos. Al salir de cuadro (o al saltarla
/// con Enter / Espacio / Esc / clic) carga el Nivel 2, que empieza con la llegada montado (LlegadaMontado).
/// Ya suena la música de Pozos, así sigue sin corte en el nivel.
/// </summary>
public class ViajeMontado : MonoBehaviour
{
    static readonly int AnimVelocidad = Animator.StringToHash("Velocidad");

    [SerializeField] Transform montado;                  // Mateo sobre el alebrije (Animator "Montado")
    [SerializeField] Animator animator;
    [SerializeField] Transform camara;
    [SerializeField] float velocidad = 6.5f;
    [SerializeField] float xFin = 45f;                   // Aquí ya salió de cuadro por la derecha
    [SerializeField] float adelantoCamara = 3f;
    [SerializeField] float camaraMin, camaraMax;         // Para no ver las orillas del fondo

    [Header("Huellas de fuego")]
    [SerializeField] SpriteRenderer[] huellas;
    [SerializeField] Sprite[] cuadrosFuego;
    [SerializeField] float cuadrosPorSegundo = 10f;

    [Header("Letrero")]
    [SerializeField] CanvasGroup letrero;
    [SerializeField] float esperaSaltar = 0.4f;          // La tecla con que se eligió "RUMBO A POZOS" no salta el viaje
    [SerializeField] string escenaSiguiente = Escenas.Nivel2;

    public bool Terminado { get; private set; }
    public Transform Montado => montado;

    InputAction accionSaltar;
    float inicio;

    void Awake()
    {
        accionSaltar = new InputAction("SaltarViaje", InputActionType.Button, "<Keyboard>/enter");
        accionSaltar.AddBinding("<Keyboard>/space");
        accionSaltar.AddBinding("<Keyboard>/escape");
        accionSaltar.AddBinding("<Mouse>/leftButton");
        accionSaltar.AddBinding("<Gamepad>/buttonSouth");
        accionSaltar.AddBinding("<Gamepad>/start");
    }

    void OnEnable() => accionSaltar.Enable();
    void OnDisable() => accionSaltar.Disable();

    void Start()
    {
        inicio = Time.time;
        GestorAudio.CambiarMusica(Musica.Nivel2, 1f);
        GestorAudio.Reproducir(Sfx.Estampida, 0f);           // Galope
        if (animator != null) animator.SetFloat(AnimVelocidad, velocidad);
        if (letrero != null) { letrero.alpha = 0f; StartCoroutine(MostrarLetrero()); }
    }

    void Update()
    {
        if (Terminado) return;
        if (Time.time - inicio >= esperaSaltar && accionSaltar.WasPressedThisFrame()) { Terminar(); return; }

        montado.position += Vector3.right * velocidad * Time.deltaTime;
        if (montado.position.x >= xFin) Terminar();

        // Cada huella titila con su propio desfase para que el fuego no se vea sincronizado
        if (cuadrosFuego == null || cuadrosFuego.Length == 0) return;
        for (int i = 0; i < huellas.Length; i++)
            huellas[i].sprite = cuadrosFuego[(int)(Time.time * cuadrosPorSegundo + i * 7) % cuadrosFuego.Length];
    }

    void LateUpdate()
    {
        if (camara == null) return;
        float x = Mathf.Clamp(montado.position.x + adelantoCamara, camaraMin, camaraMax);
        camara.position = new Vector3(x, camara.position.y, camara.position.z);
    }

    IEnumerator MostrarLetrero()
    {
        yield return new WaitForSeconds(0.6f);
        for (float t = 0f; t < 0.5f; t += Time.deltaTime) { letrero.alpha = t / 0.5f; yield return null; }
        letrero.alpha = 1f;
        yield return new WaitForSeconds(3f);
        for (float t = 0f; t < 0.6f; t += Time.deltaTime) { letrero.alpha = 1f - t / 0.6f; yield return null; }
        letrero.alpha = 0f;
    }

    void Terminar()
    {
        Terminado = true;
        CambioEscena.Cargar(escenaSiguiente);
    }
}
