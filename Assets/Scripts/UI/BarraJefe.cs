using UnityEngine;
using UnityEngine.UI;

/// <summary>Barra de vida del jefe abajo al centro, con su nombre. Aparece al empezar la pelea y se va al vencerlo.</summary>
public class BarraJefe : MonoBehaviour
{
    [SerializeField] Text nombre;
    [SerializeField] Image barra;
    [SerializeField] Image barraRetardo;

    Health salud;
    float mostrada = 1f, retardo = 1f;

    public void Mostrar(Health saludJefe, string nombreJefe)
    {
        salud = saludJefe;
        nombre.text = nombreJefe;
        mostrada = retardo = 1f;
        gameObject.SetActive(true);
    }

    void Update()
    {
        if (salud == null) { gameObject.SetActive(false); return; }

        float objetivo = (float)salud.VidaActual / salud.VidaMaxima;
        mostrada = Mathf.MoveTowards(mostrada, objetivo, Time.deltaTime * 1.5f);
        retardo = retardo < mostrada ? mostrada : Mathf.MoveTowards(retardo, mostrada, Time.deltaTime * 0.4f);
        barra.fillAmount = mostrada;
        if (barraRetardo != null) barraRetardo.fillAmount = retardo;

        if (salud.EstaMuerto && mostrada <= 0f) gameObject.SetActive(false);
    }
}
