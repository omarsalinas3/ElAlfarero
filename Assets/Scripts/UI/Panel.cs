using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Panel de interfaz que aparece con un pequeño "pop" (tiempo real, funciona en pausa) y deja seleccionado
/// su primer botón para poder usar el teclado.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class Panel : MonoBehaviour
{
    [SerializeField] GameObject primerSeleccionado;
    [SerializeField] RectTransform contenido;

    CanvasGroup grupo;

    void Awake() { grupo = GetComponent<CanvasGroup>(); }

    public void Abrir()
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(Aparecer());
        if (primerSeleccionado != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(primerSeleccionado);
    }

    public void Cerrar() { gameObject.SetActive(false); }

    IEnumerator Aparecer()
    {
        var rt = contenido != null ? contenido : (RectTransform)transform;
        for (float t = 0f; t < 0.18f; t += Time.unscaledDeltaTime)
        {
            float k = t / 0.18f;
            grupo.alpha = k;
            rt.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, 1f - (1f - k) * (1f - k));
            yield return null;
        }
        grupo.alpha = 1f;
        rt.localScale = Vector3.one;
    }
}
