using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// Pruebas de la issue #35: cuadro de diálogo con retrato y letra por letra (Don Toribio al inicio del Nivel 1),
/// que se adelanta y se salta, y la cinemática de viñetas entre el Nivel 1 y el viaje a Pozos. Con la #92, las viñetas
/// de Pozos al panteón y del panteón a los túneles y el diálogo al empezar el Nivel 4.
/// </summary>
public class NarrativaTests
{
    Keyboard teclado;

    [UnitySetUp]
    public IEnumerator Preparar()
    {
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        teclado = InputSystem.AddDevice<Keyboard>();
        GestorGuardado.UsarRuta(GuardadoPruebas.Ruta);
        GestorGuardado.Borrar();
        yield return null;
    }

    [TearDown]
    public void Limpiar()
    {
        InputSystem.RemoveDevice(teclado);
        GestorGuardado.Borrar();
    }

    IEnumerator Presionar(Key k)
    {
        InputSystem.QueueStateEvent(teclado, new KeyboardState(k));
        yield return null;
        InputSystem.QueueStateEvent(teclado, new KeyboardState());
        yield return null;
    }

    [UnityTest]
    public IEnumerator Nivel1_DonToribioPresentaElObjetivoSinDetenerElJuego()
    {
        yield return SceneManager.LoadSceneAsync("Nivel1_Dolores", LoadSceneMode.Single);
        var dialogo = CuadroDialogo.Actual;
        Assert.IsNotNull(dialogo, "El nivel tiene cuadro de diálogo");

        float limite = Time.time + 6f;
        while (!dialogo.Activo && Time.time < limite) yield return null;
        Assert.IsTrue(dialogo.Activo, "Al empezar el nivel habla Don Toribio");
        yield return new WaitForSeconds(0.5f);

        var nombre = Object.FindObjectsByType<Text>(FindObjectsSortMode.None).First(t => t.name == "Nombre");
        StringAssert.Contains("TORIBIO", nombre.text);
        var retrato = Object.FindObjectsByType<Image>(FindObjectsSortMode.None).First(i => i.name == "Retrato");
        Assert.IsNotNull(retrato.sprite, "Con su retrato");
        Assert.AreEqual(GestorNivel.Estado.Jugando, GestorNivel.Actual.EstadoActual, "El diálogo no detiene la acción");

        // Letra por letra: Enter completa la línea y otro Enter pasa a la siguiente
        yield return Presionar(Key.Enter);
        yield return null;
        int linea = dialogo.LineaActual;
        yield return Presionar(Key.Enter);
        yield return new WaitForSeconds(0.1f);
        Assert.AreEqual(linea + 1, dialogo.LineaActual, "Enter pasa a la siguiente línea");

        // Retroceso salta todo
        yield return Presionar(Key.Backspace);
        yield return new WaitForSeconds(0.4f);
        Assert.IsFalse(dialogo.Activo, "Se puede saltar todo el diálogo");
    }

    [UnityTest]
    public IEnumerator Dialogo_EscribeLetraPorLetraYAvanzaSolo()
    {
        yield return SceneManager.LoadSceneAsync("Nivel1_Dolores", LoadSceneMode.Single);
        Object.Destroy(Object.FindAnyObjectByType<DialogoInicio>().gameObject);   // Que Don Toribio no lo interrumpa
        var dialogo = CuadroDialogo.Actual;
        var lineas = new[]
        {
            new LineaDialogo { personaje = "CARMEN", texto = "¡MATEO! ¡AQUI ESTOY!" },
            new LineaDialogo { personaje = "EL CHARRO NEGRO", texto = "NUNCA LA ALCANZARAS." },
        };
        bool termino = false;
        dialogo.Mostrar(lineas, () => termino = true);
        yield return new WaitForSeconds(0.3f);
        Assert.Less(dialogo.TextoVisible.Length, lineas[0].texto.Length, "El texto aparece letra por letra");

        float limite = Time.time + 15f;
        while (!termino && Time.time < limite) yield return null;
        Assert.IsTrue(termino, "Sin tocar nada, las líneas avanzan solas y avisa al terminar");
    }

    [UnityTest]
    public IEnumerator Transicion_VinetasEntreNivel1YViajeSePuedenSaltar()
    {
        yield return SceneManager.LoadSceneAsync("Transicion_Pozos", LoadSceneMode.Single);
        yield return new WaitForSeconds(0.5f);
        var cinematica = Object.FindAnyObjectByType<SecuenciaIntro>();
        Assert.IsNotNull(cinematica, "La cinemática reutiliza SecuenciaIntro");

        yield return Presionar(Key.Escape);
        float limite = Time.realtimeSinceStartup + 5f;
        while (SceneManager.GetActiveScene().name == "Transicion_Pozos" && Time.realtimeSinceStartup < limite) yield return null;
        Assert.AreEqual("Viaje_Pozos", SceneManager.GetActiveScene().name, "Al saltarla sigue el viaje montado a Pozos");
    }

    static IEnumerator CargarEscena(string escena)
    {
        yield return SceneManager.LoadSceneAsync(escena, LoadSceneMode.Single);
        yield return new WaitForSeconds(0.4f);
    }

    // ------------------------------------------------------------------ #92 Narrativa entre niveles
    IEnumerator SaltarVinetasHasta(string escena)
    {
        yield return new WaitForSeconds(0.6f);
        Assert.IsNotNull(Object.FindAnyObjectByType<SecuenciaIntro>(), "Son viñetas con SecuenciaIntro");
        InputSystem.QueueStateEvent(teclado, new KeyboardState(Key.Escape));
        yield return null;
        InputSystem.QueueStateEvent(teclado, new KeyboardState());
        float limite = Time.realtimeSinceStartup + 8f;
        while (SceneManager.GetActiveScene().name != escena && Time.realtimeSinceStartup < limite) yield return null;
        Assert.AreEqual(escena, SceneManager.GetActiveScene().name);
    }

    [UnityTest]
    public IEnumerator Narrativa_VinetasDePozosAlPanteonYDelPanteonALosTuneles()
    {
        yield return CargarEscena(Escenas.TransicionPanteon);
        Assert.AreEqual(3, Object.FindAnyObjectByType<SecuenciaIntro>().Vinetas);
        yield return SaltarVinetasHasta(Escenas.Nivel3);

        yield return CargarEscena(Escenas.TransicionTuneles);
        Assert.AreEqual(3, Object.FindAnyObjectByType<SecuenciaIntro>().Vinetas);
        yield return SaltarVinetasHasta(Escenas.Nivel4);
    }

    [UnityTest]
    public IEnumerator Narrativa_LiberarALaLloronaLlevaALasVinetasDeLosTuneles()
    {
        yield return CargarEscena(Escenas.Nivel3);
        Object.FindAnyObjectByType<PlayerController>().GetComponent<Health>().HacerInvulnerable(999f);
        GestorNivel.Actual.Completar();
        yield return new WaitForSeconds(0.6f);
        var siguiente = Object.FindObjectsByType<Button>().FirstOrDefault(b => b.name == "Siguiente");
        Assert.IsNotNull(siguiente, "La victoria del panteón ofrece seguir a los túneles");
        siguiente.onClick.Invoke();
        float limite = Time.realtimeSinceStartup + 8f;
        while (SceneManager.GetActiveScene().name != Escenas.TransicionTuneles && Time.realtimeSinceStartup < limite) yield return null;
        Assert.AreEqual(Escenas.TransicionTuneles, SceneManager.GetActiveScene().name, "El río baja y aparecen los túneles");
    }

    [UnityTest]
    public IEnumerator Narrativa_DialogoAlEmpezarLosTuneles()
    {
        yield return CargarEscena(Escenas.Nivel4);
        var dialogo = CuadroDialogo.Actual;
        float limite = Time.time + 6f;
        while (!dialogo.Activo && Time.time < limite) yield return null;
        Assert.IsTrue(dialogo.Activo, "Al entrar a los túneles hablan Carmen y el Charro");
        var nombres = Object.FindAnyObjectByType<DialogoInicio>().Lineas.Select(l => l.personaje).ToArray();
        CollectionAssert.Contains(nombres, "CARMEN");
        CollectionAssert.Contains(nombres, "EL CHARRO NEGRO");
    }
}
