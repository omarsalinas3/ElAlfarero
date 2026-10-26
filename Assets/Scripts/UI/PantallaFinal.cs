using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Pantalla de GAME OVER o de MISIÓN CUMPLIDA: título que cae, mensaje, resumen y dos botones.</summary>
public class PantallaFinal : MonoBehaviour
{
    [SerializeField] Panel panel;
    [SerializeField] Text titulo;
    [SerializeField] Text subtitulo;
    [SerializeField] Text resumen;
    [SerializeField] Button botonReintentar;
    [SerializeField] Button botonMenu;
    [SerializeField] GameObject avisoNuevoRecord;      // "¡NUEVO RECORD!" parpadeando (solo en la pantalla de victoria)

    void Awake()
    {
        botonReintentar.onClick.AddListener(() => GestorNivel.Actual?.Reintentar());
        botonMenu.onClick.AddListener(() => GestorNivel.Actual?.IrAlMenu());
        gameObject.SetActive(false);
    }

    public void Mostrar(string textoTitulo, string textoSubtitulo, string textoResumen, bool nuevoRecord = false)
    {
        gameObject.SetActive(true);
        titulo.text = textoTitulo;
        subtitulo.text = textoSubtitulo;
        resumen.text = textoResumen;
        if (avisoNuevoRecord != null) avisoNuevoRecord.SetActive(nuevoRecord);
        panel.Abrir();
        StartCoroutine(CaerTitulo());
    }

    IEnumerator CaerTitulo()
    {
        var rt = titulo.rectTransform;
        Vector2 destino = rt.anchoredPosition;
        for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
        {
            float k = t / 0.5f;
            float rebote = 1f - Mathf.Abs(Mathf.Cos(k * Mathf.PI * 1.5f)) * (1f - k);   // cae y rebota
            rt.anchoredPosition = destino + Vector2.up * (1f - rebote) * 160f;
            yield return null;
        }
        rt.anchoredPosition = destino;
    }
}
