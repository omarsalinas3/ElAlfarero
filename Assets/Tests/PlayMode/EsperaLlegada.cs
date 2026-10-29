using System.Collections;
using UnityEngine;

/// <summary>
/// El Nivel 2 empieza con Mateo llegando montado y tumbado por la Bruja (LlegadaMontado), que es una cinemática de unos
/// segundos. Las pruebas que juegan el Nivel 2 esperan a que termine antes de mover a Mateo.
/// </summary>
public static class EsperaLlegada
{
    public static IEnumerator Terminar()
    {
        var llegada = Object.FindFirstObjectByType<LlegadaMontado>();
        float limite = Time.realtimeSinceStartup + 10f;
        while (llegada != null && !llegada.Terminada && Time.realtimeSinceStartup < limite) yield return null;
        yield return null;
    }
}
