using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Letrero de inicio estilo arcade: "MISIÓN 1 / DOLORES HIDALGO / ¡A LA CARGA!" entra, se queda y se va.</summary>
public class BannerMision : MonoBehaviour
{
    [SerializeField] Text linea1;
    [SerializeField] Text linea2;
    [SerializeField] Text linea3;
    [SerializeField] float duracion = 2.6f;

    public void Mostrar(string a, string b, string c)
    {
        gameObject.SetActive(true);
        linea1.text = a; linea2.text = b; linea3.text = c;
        StopAllCoroutines();
        StartCoroutine(Animar());
    }

    IEnumerator Animar()
    {
        var lineas = new[] { linea1, linea2, linea3 };
        var destinos = new Vector2[3];
        for (int i = 0; i < 3; i++) destinos[i] = lineas[i].rectTransform.anchoredPosition;

        for (float t = 0f; t < duracion; t += Time.deltaTime)
        {
            for (int i = 0; i < 3; i++)
            {
                // Cada línea entra desde un lado con un pequeño retraso, y al final todas se desvanecen
                float k = Mathf.Clamp01((t - i * 0.18f) / 0.35f);
                float suave = 1f - (1f - k) * (1f - k) * (1f - k);
                float lado = i % 2 == 0 ? -1f : 1f;
                lineas[i].rectTransform.anchoredPosition = destinos[i] + Vector2.right * lado * (1f - suave) * 1400f;
                var col = lineas[i].color;
                col.a = Mathf.Clamp01((duracion - t) / 0.4f);
                lineas[i].color = col;
            }
            if (t > duracion * 0.55f && t - Time.deltaTime <= duracion * 0.55f) linea3.transform.localScale = Vector3.one * 1.25f;
            linea3.transform.localScale = Vector3.Lerp(linea3.transform.localScale, Vector3.one, Time.deltaTime * 8f);
            yield return null;
        }
        for (int i = 0; i < 3; i++) lineas[i].rectTransform.anchoredPosition = destinos[i];
        gameObject.SetActive(false);
    }
}
