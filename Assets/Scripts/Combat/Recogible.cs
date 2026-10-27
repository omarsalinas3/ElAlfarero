using UnityEngine;

public enum TipoRecogible { Guacamaya, Caguama, Moneda }

/// <summary>
/// Objeto que el jugador recoge al tocarlo: La Guacamaya (cura), La Caguama (se guarda para beberla) y monedas.
/// Si Mateo ya trae la vida llena o el máximo de caguamas, la guacamaya o la caguama se cambian por monedas.
/// Flota arriba y abajo y puede animarse con una lista de cuadros.
/// El botín de huacales y hornos sale de Reciclaje y vuelve ahí al recogerse; los puestos en la escena se destruyen.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Recogible : MonoBehaviour
{
    [SerializeField] TipoRecogible tipo = TipoRecogible.Moneda;
    [Tooltip("Guacamaya: vida que cura. Moneda/Caguama: cuántas da.")]
    [SerializeField] int cantidad = 1;
    [Tooltip("Monedas que da si no se puede usar (vida llena / caguamas al máximo). 0 = se queda en el suelo.")]
    [SerializeField] int monedasSiLleno = -1;   // -1: valor por defecto según el tipo

    [Header("Visual")]
    [SerializeField] SpriteRenderer sprite;
    [SerializeField] Sprite[] cuadros;
    [SerializeField] float cuadrosPorSegundo = 10f;
    [SerializeField] float amplitudFlote = 0.12f;
    [SerializeField] SpriteAnimadoTemporal efectoRecoger;

    public TipoRecogible Tipo => tipo;
    public int MonedasSiLleno => monedasSiLleno >= 0 ? monedasSiLleno : tipo switch
    {
        TipoRecogible.Guacamaya => 5,
        TipoRecogible.Caguama => 10,
        _ => 0,
    };

    Vector3 origen;
    float fase;

    void Awake()
    {
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
        GetComponent<Collider2D>().isTrigger = true;
        fase = Random.value * 10f;
    }

    void Start() { origen = sprite.transform.localPosition; }

    void Update()
    {
        float t = Time.time + fase;
        sprite.transform.localPosition = origen + Vector3.up * Mathf.Sin(t * 3f) * amplitudFlote;
        if (cuadros != null && cuadros.Length > 1)
            sprite.sprite = cuadros[(int)(t * cuadrosPorSegundo) % cuadros.Length];
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        var inventario = otro.GetComponentInParent<InventarioJugador>();
        if (inventario == null) return;

        bool recogido = tipo switch
        {
            TipoRecogible.Guacamaya => inventario.Comer(cantidad),
            TipoRecogible.Caguama => inventario.AgregarCaguamas(cantidad),
            _ => inventario.AgregarMonedas(cantidad),
        };
        bool cambiadoPorMonedas = false;
        if (!recogido)
        {
            // Vida llena o caguamas al máximo: en lugar de dejarla tirada, se cambia por monedas
            if (MonedasSiLleno <= 0) return;
            inventario.AgregarMonedas(MonedasSiLleno);
            cambiadoPorMonedas = true;
        }

        GestorAudio.Reproducir(cambiadoPorMonedas ? Sfx.Moneda : tipo switch
        {
            TipoRecogible.Guacamaya => Sfx.RecogerGuacamaya,
            TipoRecogible.Caguama => Sfx.RecogerCaguama,
            _ => Sfx.Moneda,
        });
        if (efectoRecoger != null) Reciclaje.Sacar(efectoRecoger, sprite.transform.position, Quaternion.identity);
        Reciclaje.Guardar(gameObject);
    }
}
