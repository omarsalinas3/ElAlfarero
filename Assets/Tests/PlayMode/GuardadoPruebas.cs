using System.IO;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Antes de cualquier prueba de este ensamblado: el guardado se manda a un archivo de pruebas,
/// así las victorias que provocan las pruebas no tocan la partida real del jugador.
/// </summary>
[SetUpFixture]
public class GuardadoPruebas
{
    public static string Ruta => Path.Combine(Application.temporaryCachePath, "partida_pruebas.json");

    [OneTimeSetUp]
    public void Antes()
    {
        GestorGuardado.UsarRuta(Ruta);
        GestorGuardado.Borrar();
    }

    [OneTimeTearDown]
    public void Despues() { GestorGuardado.Borrar(); }
}
