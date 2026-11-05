using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// Pruebas de la issue #37: zona de juego 16:9 en 16:9, 16:10, 4:3, 21:9 y celular con muesca, y que todos los
/// canvas usen el escalado "Expand" con su contenido dentro del área segura.
/// </summary>
public class ResolucionesTests
{
    static void AssertRect(Rect esperado, Rect real, string caso)
    {
        Assert.AreEqual(esperado.x, real.x, 0.5f, caso + " x");
        Assert.AreEqual(esperado.y, real.y, 0.5f, caso + " y");
        Assert.AreEqual(esperado.width, real.width, 0.5f, caso + " ancho");
        Assert.AreEqual(esperado.height, real.height, 0.5f, caso + " alto");
    }

    [Test]
    public void AreaJuego_EsSiempre16a9YCentrada()
    {
        AssertRect(new Rect(0, 0, 1920, 1080), Pantalla.AreaJuego(1920, 1080, new Rect(0, 0, 1920, 1080)), "16:9");
        AssertRect(new Rect(0, 60, 1920, 1080), Pantalla.AreaJuego(1920, 1200, new Rect(0, 0, 1920, 1200)), "16:10: barras arriba y abajo");
        AssertRect(new Rect(0, 135, 1440, 810), Pantalla.AreaJuego(1440, 1080, new Rect(0, 0, 1440, 1080)), "4:3");
        AssertRect(new Rect(320, 0, 1920, 1080), Pantalla.AreaJuego(2560, 1080, new Rect(0, 0, 2560, 1080)), "21:9: barras a los lados");

        // Celular acostado 19.5:9 con muesca de 100 px a la izquierda: la zona no la toca
        Rect area = Pantalla.AreaJuego(2340, 1080, new Rect(100, 0, 2240, 1080));
        Assert.GreaterOrEqual(area.xMin, 100f, "Respeta la muesca");
        Assert.AreEqual(16f / 9f, area.width / area.height, 0.01f);
        Assert.AreEqual(1080f, area.height, 0.5f, "Usa todo el alto");
    }

    [Test]
    public void AreaJuego_SinAreaSeguraUsaLaPantalla()
    {
        AssertRect(new Rect(0, 0, 1920, 1080), Pantalla.AreaJuego(1920, 1080, Rect.zero), "Área segura vacía");
    }

    [UnityTest]
    public IEnumerator Escenas_CanvasExpandYCamaraEnLaZonaDeJuego([Values("MenuPrincipal", "Intro", "Nivel1_Dolores", "Nivel2_Pozos")] string escena)
    {
        yield return SceneManager.LoadSceneAsync(escena, LoadSceneMode.Single);
        yield return null;
        yield return null;

        var escaladores = Object.FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include)
                                .Where(e => e.GetComponent<Canvas>().renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
        Assert.IsNotEmpty(escaladores);
        foreach (var e in escaladores)
        {
            Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, e.uiScaleMode, e.name);
            Assert.AreEqual(CanvasScaler.ScreenMatchMode.Expand, e.screenMatchMode, e.name);
            var area = e.GetComponent<AreaSegura>();
            Assert.IsNotNull(area, e.name + " acomoda su contenido en el área segura");
            Assert.AreEqual(AjustePantalla.Viewport, area.Area, e.name);
        }

        Camera cam = Camera.main;
        Assert.AreEqual(AjustePantalla.Viewport, cam.rect, "La cámara dibuja solo en la zona de juego");
        Assert.AreEqual(16f / 9f, (float)cam.pixelWidth / cam.pixelHeight, 0.02f, "La cámara ve en 16:9");
    }
}
