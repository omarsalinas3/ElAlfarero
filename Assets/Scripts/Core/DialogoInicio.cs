using System.Collections;
using UnityEngine;

/// <summary>
/// Lanza un diálogo al empezar el nivel (#35), cuando ya pasó el letrero de misión: Don Toribio presenta el
/// objetivo en Dolores y el Charro Negro se burla en Pozos. No detiene el juego (ver CuadroDialogo).
/// </summary>
public class DialogoInicio : MonoBehaviour
{
    [SerializeField] LineaDialogo[] lineas;
    [SerializeField] float espera = 3f;                    // Deja ver el letrero "MISION n"

    public LineaDialogo[] Lineas => lineas;

    IEnumerator Start()
    {
        yield return new WaitForSeconds(espera);
        if (CuadroDialogo.Actual != null) CuadroDialogo.Actual.Mostrar(lineas);
    }
}
