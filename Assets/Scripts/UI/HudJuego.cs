using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD rústico: jarrito de barro + barra de vida (arriba izquierda), caguamas y su temporizador dorado,
/// munición de Talavera en marco de azulejo y monedas (arriba derecha).
/// </summary>
public class HudJuego : MonoBehaviour
{
    [Header("Jugador")]
    [SerializeField] Health salud;
    [SerializeField] InventarioJugador inventario;

    [Header("Vida")]
    [SerializeField] Image jarrito;
    [SerializeField] Sprite jarritoLleno, jarritoMedio, jarritoRoto;
    [SerializeField] Image barraVida;
    [SerializeField] Image barraVidaRetardo;         // Franja clara que alcanza a la barra después de un golpe

    [Header("Caguamas")]
    [SerializeField] Image[] iconosCaguama;
    [SerializeField] GameObject grupoTemporizador;
    [SerializeField] Image barraCaguama;
    [SerializeField] Text textoCaguama;

    [Header("Otros")]
    [SerializeField] Text textoMonedas;
    [SerializeField] Text textoMunicion;

    float vidaMostrada = 1f, retardoMostrado = 1f;

    // Últimos valores mostrados: los textos solo se reescriben cuando cambian (evita basura de memoria cada frame)
    int monedasMostradas = -1, segundosMostrados = -1, caguamasMostradas = -1;
    bool latiendo;

    void Start()
    {
        if (textoMunicion != null) textoMunicion.text = "∞";
    }

    void Update()
    {
        if (salud == null) return;

        // Vida: la barra baja rápido y la franja de retardo la sigue despacio
        float objetivo = (float)salud.VidaActual / salud.VidaMaxima;
        vidaMostrada = Mathf.MoveTowards(vidaMostrada, objetivo, Time.unscaledDeltaTime * 2.5f);
        retardoMostrado = retardoMostrado < vidaMostrada ? vidaMostrada : Mathf.MoveTowards(retardoMostrado, vidaMostrada, Time.unscaledDeltaTime * 0.6f);
        barraVida.fillAmount = vidaMostrada;
        if (barraVidaRetardo != null) barraVidaRetardo.fillAmount = retardoMostrado;

        jarrito.sprite = objetivo > 0.6f ? jarritoLleno : objetivo > 0.25f ? jarritoMedio : jarritoRoto;

        // Con poca vida el jarrito late (solo se toca la escala mientras late, para no reconstruir el Canvas en vano)
        bool debeLatir = objetivo <= 0.25f && objetivo > 0f;
        if (debeLatir)
            jarrito.rectTransform.localScale = Vector3.one * (1f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f)) * 0.12f);
        else if (latiendo)
            jarrito.rectTransform.localScale = Vector3.one;
        latiendo = debeLatir;

        if (inventario == null) return;

        if (inventario.Caguamas != caguamasMostradas)
        {
            caguamasMostradas = inventario.Caguamas;
            for (int i = 0; i < iconosCaguama.Length; i++)
            {
                iconosCaguama[i].gameObject.SetActive(i < inventario.MaximoCaguamas);
                iconosCaguama[i].color = i < caguamasMostradas ? Color.white : new Color(0.25f, 0.2f, 0.2f, 0.55f);
            }
        }

        bool activa = inventario.CaguamaActiva;
        if (grupoTemporizador.activeSelf != activa) grupoTemporizador.SetActive(activa);
        if (activa)
        {
            barraCaguama.fillAmount = inventario.CaguamaRestante / inventario.DuracionCaguama;
            int segundos = Mathf.CeilToInt(inventario.CaguamaRestante);
            if (segundos != segundosMostrados)
            {
                segundosMostrados = segundos;
                textoCaguama.text = segundos + "s";
            }
        }

        if (inventario.Monedas != monedasMostradas)
        {
            monedasMostradas = inventario.Monedas;
            textoMonedas.text = "x" + monedasMostradas.ToString("00");
        }
    }
}
