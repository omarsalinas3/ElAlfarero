using UnityEngine;

/// <summary>
/// Efecto de un solo uso (impacto, explosión): reproduce una lista de sprites a cierto fps y se destruye.
/// Se usa para el estallido de la Talavera; sirve igual para Barro Negro / Vidriado más adelante.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteAnimadoTemporal : MonoBehaviour
{
    [SerializeField] Sprite[] cuadros;
    [SerializeField] float cuadrosPorSegundo = 24f;

    SpriteRenderer sr;
    float tiempo;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (cuadros != null && cuadros.Length > 0) sr.sprite = cuadros[0];
    }

    void Update()
    {
        tiempo += Time.deltaTime;
        int indice = (int)(tiempo * cuadrosPorSegundo);

        if (cuadros == null || indice >= cuadros.Length)
        {
            Destroy(gameObject);
            return;
        }
        sr.sprite = cuadros[indice];
    }
}
