using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Apuntado y disparo continuo (mantener clic izquierdo o Espacio, estilo Metal Slug).
/// Gana el último control que se usó: si se mueve el mouse se apunta hacia él; si se camina con las flechas
/// (PlayerController llama a UsarTeclado) se apunta al frente, hacia donde mira Mateo.
/// Es el único script que lee la posición del mouse; PlayerController usa DireccionApuntado para voltear al personaje.
/// El arma actual es un CeramicaData: el cambio de cerámica del Sprint 3 solo llama a EquiparCeramica().
/// </summary>
public class PlayerShooter : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] CeramicaData ceramicaActual;
    [SerializeField] Transform puntoDisparo;      // Hijo del jugador, colocado en la mano / boca del arma

    InputAction accionDisparar;
    Camera camara;
    PlayerController controlador;
    float proximoDisparo;
    Vector2 ultimaPosMouse;
    bool posMouseLeida;

    /// <summary>Dirección normalizada del disparo: hacia el mouse, o al frente si se está usando el teclado.</summary>
    public Vector2 DireccionApuntado { get; private set; } = Vector2.right;

    /// <summary>True si el último control usado fue el mouse (moverlo o dar clic).</summary>
    public bool UsandoMouse { get; private set; }

    /// <summary>Lo llama PlayerController al caminar con las flechas o A/D: desde ahí se apunta al frente.</summary>
    public void UsarTeclado() => UsandoMouse = false;
    public CeramicaData CeramicaActual => ceramicaActual;

    void Awake()
    {
        camara = Camera.main;
        controlador = GetComponent<PlayerController>();
        accionDisparar = new InputAction("Disparar", InputActionType.Button, "<Mouse>/leftButton");
        accionDisparar.AddBinding("<Keyboard>/space");

        if (puntoDisparo == null)
        {
            Debug.LogWarning("PlayerShooter: no hay puntoDisparo asignado, se usa el transform del jugador.", this);
            puntoDisparo = transform;
        }
    }

    void OnEnable()  { accionDisparar.Enable(); }
    void OnDisable() { accionDisparar.Disable(); }

    void Update()
    {
        // En pausa, muerto o en una cinemática no se apunta ni se dispara (el clic es para los menús)
        if (Time.timeScale == 0f || (controlador != null && !controlador.ControlActivo)) return;

        ActualizarApuntado();

        if (ceramicaActual == null || ceramicaActual.prefabProyectil == null) return;

        // IsPressed = mientras se mantenga el botón (disparo automático según la cadencia)
        if (accionDisparar.IsPressed() && Time.time >= proximoDisparo)
        {
            Disparar();
        }
    }

    void ActualizarApuntado()
    {
        DetectarMouse();
        if (!UsandoMouse || camara == null)
        {
            // Con teclado se lanza al frente, hacia donde mira Mateo
            DireccionApuntado = new Vector2(controlador != null ? controlador.DireccionMirada : 1, 0f);
            return;
        }

        Vector2 pantalla = Mouse.current.position.ReadValue();
        Vector3 mundo = camara.ScreenToWorldPoint(new Vector3(pantalla.x, pantalla.y, -camara.transform.position.z));

        Vector2 direccion = (Vector2)mundo - (Vector2)puntoDisparo.position;

        // Si el mouse está casi encima del punto de disparo mantenemos la dirección anterior (evita temblores)
        if (direccion.sqrMagnitude > 0.0001f)
        {
            DireccionApuntado = direccion.normalized;
        }
    }

    void DetectarMouse()
    {
        if (Mouse.current == null) { UsandoMouse = false; return; }

        Vector2 pos = Mouse.current.position.ReadValue();
        if (posMouseLeida && (pos - ultimaPosMouse).sqrMagnitude > 9f) UsandoMouse = true;   // Se movió más de 3 px
        if (Mouse.current.leftButton.isPressed) UsandoMouse = true;
        ultimaPosMouse = pos;
        posMouseLeida = true;
    }

    void Disparar()
    {
        proximoDisparo = Time.time + 1f / ceramicaActual.disparosPorSegundo;

        for (int i = 0; i < ceramicaActual.proyectilesPorDisparo; i++)
        {
            // Desvío aleatorio dentro de ± dispersionGrados
            float desvio = Random.Range(-ceramicaActual.dispersionGrados, ceramicaActual.dispersionGrados);
            Vector2 direccion = (Vector2)(Quaternion.Euler(0f, 0f, desvio) * (Vector3)DireccionApuntado);

            float angulo = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;

            Projectile proyectil = Instantiate(
                ceramicaActual.prefabProyectil,
                puntoDisparo.position,
                Quaternion.Euler(0f, 0f, angulo));

            proyectil.Inicializar(ceramicaActual, direccion);
        }

        GestorAudio.Reproducir(Sfx.Lanzar, 0.1f);
        // TODO (Sprint 2/6): animación de disparo
    }

    /// <summary>Se usará en el Sprint 3 al recoger cerámicas / cambiar de arma.</summary>
    public void EquiparCeramica(CeramicaData nueva)
    {
        ceramicaActual = nueva;
    }
}
