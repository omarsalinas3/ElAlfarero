using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Apuntado con el mouse y disparo continuo (mantener clic izquierdo, estilo Metal Slug).
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
    float proximoDisparo;

    /// <summary>Dirección normalizada desde el punto de disparo hacia el mouse.</summary>
    public Vector2 DireccionApuntado { get; private set; } = Vector2.right;
    public CeramicaData CeramicaActual => ceramicaActual;

    void Awake()
    {
        camara = Camera.main;
        accionDisparar = new InputAction("Disparar", InputActionType.Button, "<Mouse>/leftButton");

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
        if (Mouse.current == null || camara == null) return;

        Vector2 pantalla = Mouse.current.position.ReadValue();
        Vector3 mundo = camara.ScreenToWorldPoint(new Vector3(pantalla.x, pantalla.y, -camara.transform.position.z));

        Vector2 direccion = (Vector2)mundo - (Vector2)puntoDisparo.position;

        // Si el mouse está casi encima del punto de disparo mantenemos la dirección anterior (evita temblores)
        if (direccion.sqrMagnitude > 0.0001f)
        {
            DireccionApuntado = direccion.normalized;
        }
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

        // TODO (Sprint 2/6): animación de disparo y SFX
    }

    /// <summary>Se usará en el Sprint 3 al recoger cerámicas / cambiar de arma.</summary>
    public void EquiparCeramica(CeramicaData nueva)
    {
        ceramicaActual = nueva;
    }
}
