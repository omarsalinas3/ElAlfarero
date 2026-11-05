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
/// Pruebas de la issue #26: opciones con secciones Audio / Gráficos / Controles, calidad Baja/Media/Alta con
/// QualitySettings, pantalla completa o ventana, resolución, que todo se guarde entre sesiones y que también se abran
/// desde el menú de pausa.
/// </summary>
public class OpcionesTests
{
    Keyboard teclado;
    float musicaAntes, efectosAntes;

    [UnitySetUp]
    public IEnumerator Preparar()
    {
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        teclado = InputSystem.AddDevice<Keyboard>();
        GestorGuardado.UsarRuta(GuardadoPruebas.Ruta);
        GestorGuardado.Borrar();
        musicaAntes = GestorAudio.VolumenMusica;
        efectosAntes = GestorAudio.VolumenEfectos;
        ConfiguracionGrafica.Restablecer();
        yield return null;
    }

    [TearDown]
    public void Limpiar()
    {
        InputSystem.RemoveDevice(teclado);
        GestorAudio.VolumenMusica = musicaAntes;
        GestorAudio.VolumenEfectos = efectosAntes;
        ConfiguracionGrafica.Restablecer();
        GestorGuardado.Borrar();
    }

    static Button Boton(string panel, string nombre) =>
        Object.FindObjectsByType<Button>(FindObjectsInactive.Include).First(b => b.name == nombre && b.transform.parent.name == panel);

    static string TextoDe(Button b) => b.GetComponentInChildren<Text>(true).text;

    IEnumerator AbrirMenu()
    {
        yield return SceneManager.LoadSceneAsync(Escenas.Menu, LoadSceneMode.Single);
        yield return new WaitForSeconds(0.3f);
        InputSystem.QueueStateEvent(teclado, new KeyboardState(Key.Enter));
        yield return null;
        InputSystem.QueueStateEvent(teclado, new KeyboardState());
        yield return new WaitForSeconds(0.3f);
        Boton("PanelMenu", "Opciones").onClick.Invoke();
        yield return new WaitForSeconds(0.3f);
    }

    [UnityTest]
    public IEnumerator Menu_OpcionesTieneAudioGraficosYControles()
    {
        yield return AbrirMenu();
        var panel = Object.FindObjectsByType<Panel>().First(p => p.name == "PanelOpciones");
        var textos = panel.GetComponentsInChildren<Text>().Select(t => t.text).ToArray();
        foreach (var seccion in new[] { "AUDIO", "GRAFICOS", "CONTROLES" })
            Assert.IsTrue(textos.Any(t => t.Contains(seccion)), $"Falta la sección {seccion}");
        Assert.AreEqual(2, panel.GetComponentsInChildren<Slider>().Length, "Volumen de música y efectos");
        StringAssert.StartsWith("CALIDAD", TextoDe(Boton("PanelOpciones", "Calidad")));
        StringAssert.StartsWith("PANTALLA", TextoDe(Boton("PanelOpciones", "Pantalla")));
        StringAssert.StartsWith("RESOLUCION", TextoDe(Boton("PanelOpciones", "Resolucion")));

        Boton("PanelOpciones", "Controles").onClick.Invoke();
        yield return new WaitForSeconds(0.3f);
        var controles = Object.FindObjectsByType<Panel>().First(p => p.name == "PanelControles");
        var lineas = controles.GetComponentsInChildren<Text>().Select(t => t.text).ToArray();
        Assert.IsTrue(lineas.Contains("TECLADO") && lineas.Contains("GAMEPAD"), "Esquema de teclado y de gamepad");
    }

    [UnityTest]
    public IEnumerator Calidad_BajaMediaAltaConQualitySettingsYSeGuarda()
    {
        yield return AbrirMenu();
        var boton = Boton("PanelOpciones", "Calidad");
        Assert.AreEqual("CALIDAD  ALTA", TextoDe(boton));

        boton.onClick.Invoke();                                      // Alta → Baja
        Assert.AreEqual("CALIDAD  BAJA", TextoDe(boton));
        Assert.AreEqual("Low", QualitySettings.names[QualitySettings.GetQualityLevel()]);
        Assert.AreEqual(1, QualitySettings.vSyncCount, "Sigue en 60 FPS con VSync");
        boton.onClick.Invoke();                                      // Baja → Media
        Assert.AreEqual("Medium", QualitySettings.names[QualitySettings.GetQualityLevel()]);
        Assert.AreEqual(1, PlayerPrefs.GetInt("graficos_calidad"), "Se guarda entre sesiones");

        // Al volver a abrir el juego (otra escena) se respeta lo guardado
        yield return SceneManager.LoadSceneAsync(Escenas.Menu, LoadSceneMode.Single);
        Assert.AreEqual(1, ConfiguracionGrafica.Calidad);
    }

    [UnityTest]
    public IEnumerator Pantalla_CompletaOVentanaSeGuarda()
    {
        yield return AbrirMenu();
        var boton = Boton("PanelOpciones", "Pantalla");
        bool antes = ConfiguracionGrafica.PantallaCompleta;
        boton.onClick.Invoke();
        Assert.AreEqual(antes ? 0 : 1, PlayerPrefs.GetInt("graficos_completa"), "Guarda el modo elegido");
        boton.onClick.Invoke();
        Assert.AreEqual(antes ? 1 : 0, PlayerPrefs.GetInt("graficos_completa"));
        StringAssert.StartsWith("PANTALLA", TextoDe(boton));
    }

    [UnityTest]
    public IEnumerator Volumen_SeGuardaEntreSesiones()
    {
        yield return AbrirMenu();
        var slider = Object.FindObjectsByType<Slider>().First(s => s.name == "SliderMusica");
        slider.value = 0.3f;
        Assert.AreEqual(0.3f, GestorAudio.VolumenMusica, 0.01f);
        Assert.AreEqual(0.3f, PlayerPrefs.GetFloat("vol_musica"), 0.01f);
    }

    [UnityTest]
    public IEnumerator Pausa_AbreLasMismasOpcionesYLosControles()
    {
        yield return SceneManager.LoadSceneAsync(Escenas.Nivel1, LoadSceneMode.Single);
        yield return new WaitForSeconds(0.5f);
        GestorNivel.Actual.Pausar();
        yield return null;
        Boton("PanelPausa", "Opciones").onClick.Invoke();
        yield return null;
        Assert.IsTrue(Boton("PanelOpciones", "Calidad").gameObject.activeInHierarchy, "Gráficos también desde la pausa");
        Assert.IsTrue(Object.FindObjectsByType<Slider>().Any(s => s.name == "SliderEfectos"), "y el audio");

        Boton("PanelOpciones", "Controles").onClick.Invoke();
        yield return null;
        Assert.IsTrue(Object.FindObjectsByType<Panel>().Any(p => p.name == "PanelControles"), "Los controles se ven en la pausa");
        Boton("PanelControles", "Volver").onClick.Invoke();
        yield return null;
        Assert.IsTrue(Object.FindObjectsByType<Panel>().Any(p => p.name == "PanelOpciones"), "Volver regresa a Opciones");
        GestorNivel.Actual.Reanudar();
    }
}
