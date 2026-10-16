using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Lo que Mateo lleva encima: monedas y caguamas. Con E (o Q) se bebe una caguama: invencibilidad total
/// durante unos segundos con parpadeo dorado. La Guacamaya se come al recogerla (cura al momento).
/// </summary>
[RequireComponent(typeof(Health))]
public class InventarioJugador : MonoBehaviour
{
    [Header("Caguama")]
    [SerializeField] int maximoCaguamas = 3;
    [SerializeField] int caguamasIniciales = 0;
    [SerializeField] float duracionCaguama = 5f;
    [SerializeField] SpriteRenderer spriteJugador;
    [SerializeField] Color colorDorado = new Color(1f, 0.85f, 0.25f);

    Health salud;
    PlayerController controlador;
    InputAction accionBeber;
    float caguamaHasta;

    public int Monedas { get; private set; }
    public int Caguamas { get; private set; }
    public int MaximoCaguamas => maximoCaguamas;
    public float DuracionCaguama => duracionCaguama;
    public bool CaguamaActiva => Time.time < caguamaHasta;
    public float CaguamaRestante => Mathf.Max(0f, caguamaHasta - Time.time);

    public event Action OnCambio;

    void Awake()
    {
        salud = GetComponent<Health>();
        controlador = GetComponent<PlayerController>();
        if (spriteJugador == null) spriteJugador = GetComponentInChildren<SpriteRenderer>();
        Caguamas = Mathf.Clamp(caguamasIniciales, 0, maximoCaguamas);

        accionBeber = new InputAction("BeberCaguama", InputActionType.Button, "<Keyboard>/e");
        accionBeber.AddBinding("<Keyboard>/q");
    }

    void OnEnable() { accionBeber.Enable(); }
    void OnDisable() { accionBeber.Disable(); }

    void Update()
    {
        bool puedeActuar = Time.timeScale > 0f && (controlador == null || controlador.ControlActivo);
        if (puedeActuar && accionBeber.WasPressedThisFrame()) BeberCaguama();

        // Parpadeo dorado mientras dura la caguama
        if (CaguamaActiva)
        {
            float k = Mathf.PingPong(Time.time * 8f, 1f);
            spriteJugador.color = Color.Lerp(Color.white, colorDorado, k);
        }
        else if (spriteJugador.color != Color.white && caguamaHasta > 0f)
        {
            spriteJugador.color = Color.white;
            caguamaHasta = 0f;
        }
    }

    public bool BeberCaguama()
    {
        if (Caguamas <= 0 || CaguamaActiva || salud.EstaMuerto) return false;
        Caguamas--;
        caguamaHasta = Time.time + duracionCaguama;
        salud.HacerInvulnerable(duracionCaguama);
        GestorAudio.Reproducir(Sfx.BeberCaguama, 0f);
        OnCambio?.Invoke();
        return true;
    }

    public bool AgregarCaguamas(int n)
    {
        if (Caguamas >= maximoCaguamas) return false;
        Caguamas = Mathf.Min(maximoCaguamas, Caguamas + n);
        OnCambio?.Invoke();
        return true;
    }

    public bool AgregarMonedas(int n)
    {
        Monedas += n;
        OnCambio?.Invoke();
        return true;
    }

    /// <summary>La Guacamaya: cura. Si la vida ya está llena no se recoge.</summary>
    public bool Comer(int vida)
    {
        if (salud.EstaMuerto || salud.VidaActual >= salud.VidaMaxima) return false;
        salud.Curar(vida);
        return true;
    }
}
