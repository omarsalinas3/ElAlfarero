using UnityEngine;

/// <summary>
/// Efecto de un solo uso (impacto, explosión, restos): reproduce una lista de sprites a cierto fps y se va.
/// Con "mantener último" se queda el último cuadro unos segundos y se desvanece (ej. tablas del huacal en el suelo).
/// Sale de Reciclaje: al terminar vuelve ahí y al reutilizarse empieza de nuevo (OnEnable).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteAnimadoTemporal : MonoBehaviour
{
    [SerializeField] Sprite[] cuadros;
    [SerializeField] float cuadrosPorSegundo = 24f;
    [SerializeField] bool mantenerUltimo = false;
    [SerializeField] float segundosUltimo = 3f;

    SpriteRenderer sr;
    float tiempo;

    void Awake() { sr = GetComponent<SpriteRenderer>(); }

    void OnEnable()
    {
        tiempo = 0f;
        sr.color = Color.white;
        if (cuadros != null && cuadros.Length > 0) sr.sprite = cuadros[0];
    }

    void Update()
    {
        tiempo += Time.deltaTime;
        int indice = (int)(tiempo * cuadrosPorSegundo);
        int total = cuadros == null ? 0 : cuadros.Length;

        if (indice < total)
        {
            sr.sprite = cuadros[indice];
            return;
        }
        if (!mantenerUltimo || total == 0)
        {
            Reciclaje.Guardar(gameObject);
            return;
        }

        float extra = tiempo - total / cuadrosPorSegundo;
        if (extra >= segundosUltimo) { Reciclaje.Guardar(gameObject); return; }
        float alfa = Mathf.Clamp01((segundosUltimo - extra) / 0.6f);   // Se desvanece el último medio segundo
        sr.color = new Color(1f, 1f, 1f, alfa);
    }
}
