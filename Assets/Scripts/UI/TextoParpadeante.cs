using UnityEngine;
using UnityEngine.UI;

/// <summary>Hace parpadear un texto (ej. "PRESIONA CUALQUIER TECLA"). Usa tiempo real, funciona en pausa.</summary>
[RequireComponent(typeof(Graphic))]
public class TextoParpadeante : MonoBehaviour
{
    [SerializeField] float periodo = 1.1f;
    [SerializeField, Range(0f, 1f)] float alfaMinimo = 0.1f;

    Graphic grafico;

    void Awake() { grafico = GetComponent<Graphic>(); }

    void Update()
    {
        var c = grafico.color;
        c.a = Mathf.Lerp(alfaMinimo, 1f, (Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / periodo) + 1f) * 0.5f);
        grafico.color = c;
    }
}
