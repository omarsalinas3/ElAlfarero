using UnityEngine;

public enum TipoRecogible { Guacamaya, Caguama, Moneda }

/// <summary>
/// Objeto que el jugador recoge al tocarlo: La Guacamaya (cura), La Caguama (se guarda para beberla) y monedas.
/// Flota arriba y abajo y puede animarse con una lista de cuadros.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Recogible : MonoBehaviour
{
    [SerializeField] TipoRecogible tipo = TipoRecogible.Moneda;
    [Tooltip("Guacamaya: vida que cura. Moneda/Caguama: cuántas da.")]
    [SerializeField] int cantidad = 1;

    [Header("Visual")]
    [SerializeField] SpriteRenderer sprite;
    [SerializeField] Sprite[] cuadros;
    [SerializeField] float cuadrosPorSegundo = 10f;
    [SerializeField] float amplitudFlote = 0.12f;
    [SerializeField] SpriteAnimadoTemporal efectoRecoger;

    public TipoRecogible Tipo => tipo;

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
        if (!recogido) return;   // Ej. ya trae el máximo de caguamas: se queda en el suelo

        GestorAudio.Reproducir(tipo switch
        {
            TipoRecogible.Guacamaya => Sfx.RecogerGuacamaya,
            TipoRecogible.Caguama => Sfx.RecogerCaguama,
            _ => Sfx.Moneda,
        });
        if (efectoRecoger != null) Instantiate(efectoRecoger, sprite.transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}
