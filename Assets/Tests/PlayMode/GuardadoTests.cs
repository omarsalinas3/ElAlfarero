using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// Pruebas de la issue #22: partida en JSON (nivel desbloqueado, récords, monedas, armas), guardado al ganar,
/// "¡NUEVO RECORD!" en la victoria y el botón de borrar partida del menú.
/// </summary>
public class GuardadoTests
{
    [SetUp]
    public void Preparar()
    {
        GestorGuardado.UsarRuta(GuardadoPruebas.Ruta);
        GestorGuardado.Borrar();
    }

    [TearDown]
    public void Limpiar()
    {
        Time.timeScale = 1f;
        GestorGuardado.Borrar();
    }

    [Test]
    public void Victoria_SeGuardaYSeVuelveACargar()
    {
        GestorGuardado.RegistrarVictoria(1, 1500, 95f, 40);
        GestorGuardado.DesbloquearArma("BarroVidriado");
        Assert.IsTrue(File.Exists(GuardadoPruebas.Ruta), "Ganar escribe el archivo de la partida");

        GestorGuardado.Cargar();                       // Como si se abriera el juego otra vez
        var d = GestorGuardado.Datos;
        Assert.AreEqual(2, d.nivelDesbloqueado, "Ganar el Nivel 1 desbloquea el 2");
        Assert.AreEqual(1500, d.Record(1).puntajeMaximo);
        Assert.AreEqual(95f, d.Record(1).mejorTiempo, 0.01f);
        Assert.AreEqual(40, d.monedas);
        CollectionAssert.AreEqual(new[] { "Talavera", "BarroVidriado" }, d.armasDesbloqueadas);
        Assert.IsTrue(GestorGuardado.HayPartida);
    }

    [Test]
    public void Record_SoloSeFestejaAlSuperarlo()
    {
        var primera = GestorGuardado.RegistrarVictoria(1, 1000, 120f, 10);
        Assert.IsFalse(primera.NuevoRecord, "La primera victoria fija la marca sin festejarla");

        var peor = GestorGuardado.RegistrarVictoria(1, 800, 150f, 10);
        Assert.IsFalse(peor.NuevoRecord);
        Assert.AreEqual(1000, peor.puntajeMaximo, "Una partida peor no baja el récord");
        Assert.AreEqual(120f, peor.mejorTiempo, 0.01f);

        var masPuntos = GestorGuardado.RegistrarVictoria(1, 1200, 130f, 10);
        Assert.IsTrue(masPuntos.nuevoRecordPuntaje);
        Assert.IsFalse(masPuntos.nuevoRecordTiempo);

        var masRapida = GestorGuardado.RegistrarVictoria(1, 900, 100f, 10);
        Assert.IsTrue(masRapida.nuevoRecordTiempo);
        Assert.AreEqual(1200, masRapida.puntajeMaximo);
        Assert.AreEqual(40, GestorGuardado.Datos.monedas, "Las monedas se acumulan entre victorias");
    }

    [Test]
    public void ArchivoDanado_EmpiezaPartidaNueva()
    {
        File.WriteAllText(GuardadoPruebas.Ruta, "{ esto no es una partida");
        LogAssert.ignoreFailingMessages = true;        // El aviso de partida dañada es esperado
        GestorGuardado.Cargar();
        LogAssert.ignoreFailingMessages = false;

        Assert.AreEqual(1, GestorGuardado.Datos.nivelDesbloqueado);
        Assert.AreEqual(0, GestorGuardado.Datos.records.Count);
        Assert.Contains("Talavera", GestorGuardado.Datos.armasDesbloqueadas);
    }

    [Test]
    public void BorrarPartida_QuitaElArchivoYEmpiezaDeCero()
    {
        GestorGuardado.RegistrarVictoria(1, 500, 60f, 5);
        GestorGuardado.Borrar();

        Assert.IsFalse(File.Exists(GuardadoPruebas.Ruta));
        Assert.IsFalse(GestorGuardado.HayPartida);
        Assert.AreEqual(1, GestorGuardado.Datos.nivelDesbloqueado);
        Assert.AreEqual(0, GestorGuardado.Datos.monedas);
    }

    [UnityTest]
    public IEnumerator Nivel1_LaVictoriaGuardaYMuestraElNuevoRecord()
    {
        GestorGuardado.RegistrarVictoria(1, 100, 999f, 0);       // Marca anterior fácil de superar

        yield return SceneManager.LoadSceneAsync("Nivel1_Dolores", LoadSceneMode.Single);
        yield return new WaitForSeconds(0.5f);
        var gestor = GestorNivel.Actual;
        Object.FindFirstObjectByType<PlayerController>().GetComponent<Health>().HacerInvulnerable(999f);

        gestor.SumarPuntos(5000);
        gestor.Completar();
        yield return new WaitForSeconds(0.6f);

        GestorGuardado.Cargar();
        Assert.AreEqual(gestor.Puntaje, GestorGuardado.Datos.Record(1).puntajeMaximo, "La victoria guarda el puntaje");
        Assert.AreEqual(2, GestorGuardado.Datos.nivelDesbloqueado);

        var resumen = Object.FindObjectsByType<Text>().First(t => t.name == "Resumen" && t.gameObject.activeInHierarchy);
        StringAssert.Contains("RECORD    " + gestor.Puntaje, resumen.text);
        StringAssert.Contains("MEJOR", resumen.text);
        Assert.IsTrue(Object.FindObjectsByType<Text>(FindObjectsInactive.Include).First(t => t.name == "NuevoRecord").gameObject.activeInHierarchy,
                      "Superar la marca muestra ¡NUEVO RECORD!");
    }

    [UnityTest]
    public IEnumerator Menu_BorrarPartidaPideConfirmacion()
    {
        GestorGuardado.RegistrarVictoria(1, 500, 60f, 5);

        yield return SceneManager.LoadSceneAsync("MenuPrincipal", LoadSceneMode.Single);
        yield return null;
        var boton = Object.FindObjectsByType<BotonBorrarPartida>(FindObjectsInactive.Include).Single();
        boton.GetComponentInParent<Panel>(true).Abrir();
        yield return null;

        boton.Presionar();
        Assert.IsTrue(GestorGuardado.HayPartida, "El primer clic solo pide confirmación");
        Assert.AreEqual("PRESIONA DE NUEVO", boton.GetComponentInChildren<Text>().text);

        boton.Presionar();
        Assert.IsFalse(GestorGuardado.HayPartida, "El segundo clic borra la partida");
        Assert.IsFalse(boton.GetComponent<Button>().interactable, "Sin partida el botón queda desactivado");
    }
}
