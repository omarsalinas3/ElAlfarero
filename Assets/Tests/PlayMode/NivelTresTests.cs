using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Pruebas de las issues #58 (base del Nivel 3: fondos en tramos, suelo, muros, Mateo y cámara) y #59 (la Llorona
/// con sus animaciones en el Animator).
/// </summary>
public class NivelTresTests
{
    [UnitySetUp]
    public IEnumerator Preparar()
    {
        GestorGuardado.UsarRuta(GuardadoPruebas.Ruta);
        GestorGuardado.Borrar();
        yield return SceneManager.LoadSceneAsync(Escenas.Nivel3, LoadSceneMode.Single);
        yield return new WaitForSeconds(0.5f);
    }

    [TearDown]
    public void Limpiar() { GestorGuardado.Borrar(); }

    [Test]
    public void Nivel3_CincoTramosUnidosSinHuecos()
    {
        var fondos = GameObject.Find("Fondo").GetComponentsInChildren<SpriteRenderer>().OrderBy(s => s.transform.position.x).ToArray();
        Assert.AreEqual(5, fondos.Length, "El panteón se arma con 5 tramos");
        foreach (var f in fondos) StringAssert.StartsWith("lvl3_", f.sprite.name);
        for (int i = 1; i < fondos.Length; i++)
            Assert.Less(fondos[i].bounds.min.x, fondos[i - 1].bounds.max.x, $"El tramo {i + 1} se traslapa con el anterior (degradado)");
        float largo = fondos.Last().bounds.max.x - fondos.First().bounds.min.x;
        Assert.Greater(largo, 80f, "Mide al menos lo que Dolores");
    }

    [UnityTest]
    public IEnumerator Nivel3_MateoEnElSueloYCamaraDentroDelNivel()
    {
        var mateo = Object.FindAnyObjectByType<PlayerController>();
        float limite = Time.time + 2f;
        while (!mateo.EstaEnSuelo && Time.time < limite) yield return null;
        Assert.IsTrue(mateo.EstaEnSuelo, "Hay suelo bajo Mateo");
        Assert.IsNotNull(Camera.main.GetComponent<CameraFollow>(), "La cámara lo sigue");
        Assert.IsNotNull(GameObject.Find("Muro_Izquierdo"));
        Assert.IsNotNull(GameObject.Find("Muro_Derecho"));
        Assert.AreEqual(3, GestorNivel.Actual.NumeroNivel);
    }

    [UnityTest]
    public IEnumerator Llorona_FlotaConSusCincoAnimaciones()
    {
        var llorona = GameObject.Find("Llorona");
        Assert.IsNotNull(llorona, "La Llorona espera en la orilla del río");
        var animator = llorona.GetComponentInChildren<Animator>();
        Assert.IsNotNull(animator.runtimeAnimatorController);
        var clips = animator.runtimeAnimatorController.animationClips.Select(c => c.name).ToArray();
        foreach (var c in new[] { "Llorona_Flotar", "Llorona_Grito", "Llorona_Agua", "Llorona_Golpe", "Llorona_Desvanecer" })
            CollectionAssert.Contains(clips, c);

        var sr = llorona.GetComponentInChildren<SpriteRenderer>();
        yield return new WaitForSeconds(0.2f);
        StringAssert.StartsWith("llorona_flotar", sr.sprite.name, "Empieza flotando");
        Assert.Greater(sr.bounds.size.y, 3f, "Es más alta que Mateo");

        animator.Play("Agua");
        yield return new WaitForSeconds(0.3f);
        StringAssert.StartsWith("llorona_agua", sr.sprite.name, "Ataque de agua");
    }

    [UnityTest]
    public IEnumerator Nivel3_LlegarALaOrillaCompletaElNivelProvisional()
    {
        var mateo = Object.FindAnyObjectByType<PlayerController>();
        var meta = GameObject.Find("Meta_Provisional");
        var rb = mateo.GetComponent<Rigidbody2D>();
        rb.position = new Vector2(meta.transform.position.x - 1f, rb.position.y);
        mateo.transform.position = rb.position;
        float limite = Time.time + 3f;
        while (GestorNivel.Actual.EstadoActual != GestorNivel.Estado.Victoria && Time.time < limite)
        {
            rb.linearVelocity = new Vector2(6f, rb.linearVelocity.y);
            yield return new WaitForFixedUpdate();
        }
        Assert.AreEqual(GestorNivel.Estado.Victoria, GestorNivel.Actual.EstadoActual);
    }
}
