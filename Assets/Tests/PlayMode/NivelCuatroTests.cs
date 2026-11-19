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
/// Pruebas del Nivel 4, Túneles de Guanajuato: base (#86), recorrido con enemigos y exvotos (#87), derrumbe de mina
/// (#88).
/// </summary>
public class NivelCuatroTests
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
        Time.timeScale = 1f;
        GestorGuardado.Borrar();
    }

    static IEnumerator Cargar(string escena)
    {
        yield return SceneManager.LoadSceneAsync(escena, LoadSceneMode.Single);
        yield return new WaitForSeconds(0.4f);
    }

    static int EnemigosDelNivel()
    {
        int sueltos = Object.FindObjectsByType<IABandido>().Length
                    + Object.FindObjectsByType<IAEsqueleto>().Length
                    + Object.FindObjectsByType<IABruja>().Length;
        var emboscada = Object.FindAnyObjectByType<Emboscada>();
        return sueltos + (emboscada != null ? emboscada.TotalBandidos : 0);
    }

    // ------------------------------------------------------------------ #86 Base del nivel
    [UnityTest]
    public IEnumerator Nivel4_CincoTramosSueloMurosYCamara()
    {
        yield return Cargar(Escenas.Nivel4);
        var fondos = GameObject.Find("Fondo").GetComponentsInChildren<SpriteRenderer>().OrderBy(s => s.transform.position.x).ToArray();
        Assert.AreEqual(5, fondos.Length, "Los túneles se arman con 5 tramos");
        foreach (var f in fondos) StringAssert.StartsWith("lvl4_", f.sprite.name);
        for (int i = 1; i < fondos.Length; i++)
            Assert.Less(fondos[i].bounds.min.x, fondos[i - 1].bounds.max.x, $"El tramo {i + 1} se traslapa con el anterior (degradado)");

        var mateo = Object.FindAnyObjectByType<PlayerController>();
        float limite = Time.time + 2f;
        while (!mateo.EstaEnSuelo && Time.time < limite) yield return null;
        Assert.IsTrue(mateo.EstaEnSuelo, "Hay suelo bajo Mateo");
        Assert.IsNotNull(GameObject.Find("Muro_Izquierdo"));
        Assert.IsNotNull(GameObject.Find("Muro_Derecho"));
        Assert.IsNotNull(Camera.main.GetComponent<CameraFollow>());
        Assert.IsNotNull(Object.FindAnyObjectByType<HudJuego>(), "Con HUD");
        Assert.AreEqual(4, GestorNivel.Actual.NumeroNivel);
    }

    [Test]
    public void Nivel4_EntraALaListaDeNivelesDeContinuar()
    {
        Assert.AreEqual(Escenas.Nivel4, Escenas.Nivel(4));
        Assert.AreEqual(Escenas.Nivel4, Escenas.Nivel(9), "Más allá del último, el último");
    }

    [UnityTest]
    public IEnumerator Nivel4_ArenaDelCharroLibreYPlana()
    {
        yield return Cargar(Escenas.Nivel4);
        var arena = GameObject.Find("Arena_Charro");
        Assert.IsNotNull(arena, "Queda marcada la arena del jefe final");
        float inicio = arena.transform.position.x - 10.5f;
        var estorbos = Object.FindObjectsByType<Destructible>().Select(d => d.transform.position.x)
                             .Concat(Object.FindObjectsByType<IABandido>().Select(b => b.transform.position.x))
                             .Concat(Object.FindObjectsByType<IAEsqueleto>().Select(e => e.transform.position.x));
        Assert.IsFalse(estorbos.Any(x => x > inicio), "Sin huacales ni enemigos en los últimos ~21 u: el Charro galopa de lado a lado");
    }

    // ------------------------------------------------------------------ #87 Recorrido
    [UnityTest]
    public IEnumerator Nivel4_MasDificilQueElPanteonYTresExvotos()
    {
        yield return Cargar(Escenas.Nivel3);
        int enPanteon = EnemigosDelNivel();
        int oleadasPanteon = Object.FindAnyObjectByType<Emboscada>().Oleadas;
        int curacionPanteon = Object.FindObjectsByType<Recogible>().Count(r => r.Tipo == TipoRecogible.Guacamaya);

        yield return Cargar(Escenas.Nivel4);
        Assert.Greater(EnemigosDelNivel(), enPanteon, "Más enemigos que en el panteón");
        Assert.Greater(Object.FindAnyObjectByType<Emboscada>().Oleadas, oleadasPanteon, "Una oleada más en la emboscada");
        Assert.LessOrEqual(Object.FindObjectsByType<Recogible>().Count(r => r.Tipo == TipoRecogible.Guacamaya), curacionPanteon,
                           "Curación escasa");
        Assert.AreEqual(3, Object.FindObjectsByType<Exvoto>().Length, "3 exvotos escondidos");
        Assert.AreEqual(3, GestorNivel.Actual.ExvotosEnNivel);
        Assert.IsNotNull(Object.FindAnyObjectByType<Derrumbe>(), "Con su derrumbe de mina");
    }

    [UnityTest]
    [Timeout(300000)]
    public IEnumerator Nivel4_SePuedeRecorrerDeInicioAFin()
    {
        yield return Cargar(Escenas.Nivel4);
        var gestor = GestorNivel.Actual;
        var jugador = Object.FindAnyObjectByType<PlayerController>();
        var salud = jugador.GetComponent<Health>();
        var rb = jugador.GetComponent<Rigidbody2D>();
        var emboscada = Object.FindAnyObjectByType<Emboscada>();
        var derrumbe = Object.FindAnyObjectByType<Derrumbe>();
        Time.timeScale = 2f;

        float limite = Time.realtimeSinceStartup + 220f, siguienteRevision = 0f, xRevision = rb.position.x;
        InputSystem.QueueStateEvent(teclado, new KeyboardState(Key.RightArrow, Key.Space));
        while (gestor.EstadoActual != GestorNivel.Estado.Victoria && Time.realtimeSinceStartup < limite)
        {
            salud.HacerInvulnerable(5f);
            Assert.AreNotEqual(GestorNivel.Estado.GameOver, gestor.EstadoActual);

            // Vence lo que tenga cerca (enemigos y huacales que estorban)
            foreach (var h in Object.FindObjectsByType<Health>())
            {
                if (h == salud || h.EstaMuerto) continue;
                bool estorba = h.GetComponent<Destructible>() != null && h.transform.position.x > rb.position.x && h.transform.position.x - rb.position.x < 3f;
                bool enemigo = h.gameObject.layer == LayerMask.NameToLayer("Enemy") && Mathf.Abs(h.transform.position.x - rb.position.x) < 12f;
                if (estorba || enemigo) h.RecibirDano(100000);
            }

            // Si algo lo detiene se adelanta un poco (salvo cuando un evento lo tiene encerrado a propósito)
            if (Time.time >= siguienteRevision)
            {
                bool encerrado = (derrumbe != null && derrumbe.Activo) || (emboscada != null && emboscada.Activa);
                if (!encerrado && gestor.EstadoActual == GestorNivel.Estado.Jugando && rb.position.x - xRevision < 0.5f)
                {
                    Vector2 adelante = new Vector2(rb.position.x + 2f, rb.position.y + 1.5f);
                    rb.position = adelante;
                    jugador.transform.position = adelante;
                }
                xRevision = rb.position.x;
                siguienteRevision = Time.time + 1.5f;
            }
            yield return new WaitForSeconds(0.2f);
        }
        InputSystem.QueueStateEvent(teclado, new KeyboardState());

        Assert.IsTrue(derrumbe.Terminado, "Pasó por el derrumbe");
        Assert.IsTrue(emboscada.Terminada, "Pasó por la emboscada");
        Assert.AreEqual(GestorNivel.Estado.Victoria, gestor.EstadoActual, "Llega a la guarida del Charro");
        Assert.AreEqual(5, GestorGuardado.Datos.nivelDesbloqueado, "y queda guardado");
    }

    // ------------------------------------------------------------------ #88 Derrumbe
    IEnumerator EmpezarDerrumbe(Derrumbe d)
    {
        var mateo = Object.FindAnyObjectByType<PlayerController>();
        var rb = mateo.GetComponent<Rigidbody2D>();
        var pos = new Vector2(d.transform.position.x + 0.5f, rb.position.y + 0.1f);
        rb.position = pos;
        mateo.transform.position = pos;
        float limite = Time.time + 3f;
        while (!d.Activo && Time.time < limite) yield return null;
    }

    [UnityTest]
    public IEnumerator Derrumbe_AvisaConTemblorYFijaLaCamara()
    {
        yield return Cargar(Escenas.Nivel4);
        var d = Object.FindAnyObjectByType<Derrumbe>();
        var camara = Camera.main.GetComponent<CameraFollow>();
        Object.FindAnyObjectByType<PlayerController>().GetComponent<Health>().HacerInvulnerable(999f);
        yield return EmpezarDerrumbe(d);
        Assert.IsTrue(d.Activo, "Al entrar a la galería empieza el derrumbe");
        Assert.IsTrue(camara.Bloqueada, "La cámara se queda fija");

        float y0 = Camera.main.transform.position.y;
        bool tembla = false;
        for (float t = 0f; t < 1f && !tembla; t += Time.deltaTime)
        {
            tembla = Mathf.Abs(Camera.main.transform.position.y - y0) > 0.01f;
            yield return null;
        }
        Assert.IsTrue(tembla, "La cámara tiembla antes de que caiga nada");
        Assert.AreEqual(0, d.RocasSoltadas, "Primero el aviso, después las rocas");
    }

    [UnityTest]
    public IEnumerator Derrumbe_LasRocasMarcanSuSombraCaenYLastiman()
    {
        yield return Cargar(Escenas.Nivel4);
        var d = Object.FindAnyObjectByType<Derrumbe>();
        var salud = Object.FindAnyObjectByType<PlayerController>().GetComponent<Health>();
        yield return EmpezarDerrumbe(d);
        Time.timeScale = 2f;

        // Mateo se queda quieto: alguna roca "sobre Mateo" le tiene que caer encima
        int vidaInicial = salud.VidaActual;
        bool vioSombra = false;
        float limite = Time.realtimeSinceStartup + 40f;
        while (!d.Terminado && Time.realtimeSinceStartup < limite)
        {
            foreach (var r in Object.FindObjectsByType<RocaQueCae>())
                vioSombra |= r.Cayendo && r.transform.Find("Sombra").gameObject.activeSelf;
            if (salud.VidaActual < vidaInicial) break;
            yield return null;
        }
        Assert.IsTrue(vioSombra, "Cada roca marca en el suelo dónde va a caer");
        Assert.Less(salud.VidaActual, vidaInicial, "Una roca lastima a Mateo si se queda quieto");
    }

    [UnityTest]
    public IEnumerator Derrumbe_TerminaYLiberaElCamino()
    {
        yield return Cargar(Escenas.Nivel4);
        var d = Object.FindAnyObjectByType<Derrumbe>();
        Object.FindAnyObjectByType<PlayerController>().GetComponent<Health>().HacerInvulnerable(999f);
        yield return EmpezarDerrumbe(d);
        Time.timeScale = 3f;
        float limite = Time.realtimeSinceStartup + 40f;
        while (!d.Terminado && Time.realtimeSinceStartup < limite) yield return null;
        Time.timeScale = 1f;
        Assert.IsTrue(d.Terminado);
        Assert.AreEqual(d.TotalRocas, d.RocasSoltadas, "Cayeron todas las tandas");
        Assert.IsFalse(Camera.main.GetComponent<CameraFollow>().Bloqueada, "La cámara vuelve a seguir a Mateo");
        Assert.AreEqual(GestorNivel.Estado.Jugando, GestorNivel.Actual.EstadoActual);
    }
}
