using UnityEngine;
using UnityEngine.UI;

/// <summary>Dos sliders de volumen (música y efectos) que se guardan entre partidas.</summary>
public class OpcionesAudio : MonoBehaviour
{
    [SerializeField] Slider sliderMusica;
    [SerializeField] Slider sliderEfectos;
    [SerializeField] Text textoMusica;
    [SerializeField] Text textoEfectos;

    float siguientePrueba;

    void OnEnable()
    {
        sliderMusica.SetValueWithoutNotify(GestorAudio.VolumenMusica);
        sliderEfectos.SetValueWithoutNotify(GestorAudio.VolumenEfectos);
        Refrescar();
    }

    void Awake()
    {
        sliderMusica.onValueChanged.AddListener(v => { GestorAudio.VolumenMusica = v; Refrescar(); });
        sliderEfectos.onValueChanged.AddListener(v =>
        {
            GestorAudio.VolumenEfectos = v;
            Refrescar();
            if (Time.unscaledTime > siguientePrueba)       // Sonido de muestra para oír el volumen
            {
                GestorAudio.Reproducir(Sfx.Moneda, 0f);
                siguientePrueba = Time.unscaledTime + 0.25f;
            }
        });
    }

    void Refrescar()
    {
        if (textoMusica != null) textoMusica.text = $"MUSICA   {Mathf.RoundToInt(sliderMusica.value * 100),3}%";
        if (textoEfectos != null) textoEfectos.text = $"EFECTOS  {Mathf.RoundToInt(sliderEfectos.value * 100),3}%";
    }
}
