using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Panel de interfaz que aparece con un pequeño "pop" (tiempo real, funciona en pausa) y deja seleccionado
/// su primer botón para poder usar el teclado. Sus botones entran escalonados desde la izquierda (#30).
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class Panel : MonoBehaviour
{
    const float Duracion = 0.18f;
    const float RetrasoPorBoton = 0.04f;
    const float Desplazamiento = 60f;

    [SerializeField] GameObject primerSeleccionado;
    [SerializeField] RectTransform contenido;

    CanvasGroup grupo;
    RectTransform[] botones;
    Vector2[] posiciones;                          // Las de diseño: el escalonado siempre parte de aquí

    void Awake()
    {
        grupo = GetComponent<CanvasGroup>();
        var selec = GetComponentsInChildren<Selectable>(true);
        botones = new RectTransform[selec.Length];
        posiciones = new Vector2[selec.Length];
        for (int i = 0; i < selec.Length; i++)
        {
            botones[i] = (RectTransform)selec[i].transform;
            posiciones[i] = botones[i].anchoredPosition;
        }
    }

    public void Abrir()
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(Aparecer());
        if (primerSeleccionado != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(primerSeleccionado);
    }

    public void Cerrar()
    {
        StopAllCoroutines();
        ColocarBotones(1f);
        gameObject.SetActive(false);
    }

    IEnumerator Aparecer()
    {
        var rt = contenido != null ? contenido : (RectTransform)transform;
        float total = Duracion + RetrasoPorBoton * botones.Length;
        for (float t = 0f; t < total; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.Clamp01(t / Duracion);
            grupo.alpha = k;
            rt.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, Suave(k));
            ColocarBotones(t);
            yield return null;
        }
        grupo.alpha = 1f;
        rt.localScale = Vector3.one;
        ColocarBotones(float.MaxValue);
    }

    // Cada botón llega a su lugar un poco después del anterior
    void ColocarBotones(float t)
    {
        if (botones == null) return;
        for (int i = 0; i < botones.Length; i++)
        {
            if (botones[i] == null) continue;
            float k = Suave(Mathf.Clamp01((t - i * RetrasoPorBoton) / Duracion));
            botones[i].anchoredPosition = posiciones[i] + new Vector2((1f - k) * -Desplazamiento, 0f);
        }
    }

    static float Suave(float k) => 1f - (1f - k) * (1f - k);
}
