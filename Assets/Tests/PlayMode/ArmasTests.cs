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
/// Pruebas de la issue #33: Barro Vidriado en abanico, Barro Negro en arco que explota, Maceta Escudo que detiene
/// 5 balas de frente, cambio de arma (1-4, rueda, bumpers) con munición limitada, cajas en huacales, HUD y sonidos.
/// </summary>
public class ArmasTests
{
    const int Vidriado = 1, Negro = 2, Maceta = 3;

    Keyboard teclado;
    Mouse mouse;
    Gamepad gamepad;
    PlayerController jugador;
    ArsenalJugador arsenal;

    [UnitySetUp]
    public IEnumerator Preparar()
    {
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        teclado = InputSystem.AddDevice<Keyboard>();
        mouse = InputSystem.AddDevice<Mouse>();
        gamepad = InputSystem.AddDevice<Gamepad>();
        GestorGuardado.UsarRuta(GuardadoPruebas.Ruta);
        GestorGuardado.Borrar();
        yield return null;
    }

    [TearDown]
    public void Limpiar()
    {
        InputSystem.RemoveDevice(teclado);
        InputSystem.RemoveDevice(mouse);
        InputSystem.RemoveDevice(gamepad);
        GestorGuardado.Borrar();
    }

    IEnumerator Cargar(string escena)
    {
        yield return SceneManager.LoadSceneAsync(escena, LoadSceneMode.Single);
        yield return new WaitForSeconds(0.6f);
        jugador = Object.FindFirstObjectByType<PlayerController>();
        arsenal = jugador.GetComponent<ArsenalJugador>();
    }

    IEnumerator Pulsar(params Key[] teclas)
    {
        InputSystem.QueueStateEvent(teclado, new KeyboardState(teclas));
        yield return null;
        yield return null;
        InputSystem.QueueStateEvent(teclado, new KeyboardState());
        yield return null;
    }

    static BalaEnemiga Bala(Vector2 posicion, Vector2 direccion)
    {
        var go = new GameObject("Bala_Prueba") { layer = LayerMask.NameToLayer("EnemyProjectile") };
        go.transform.position = posicion;
        go.AddComponent<Rigidbody2D>();
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.1f;
        var bala = go.AddComponent<BalaEnemiga>();
        bala.Disparar(direccion, 14f, 10);
        return bala;
    }

    [UnityTest]
    public IEnumerator BarroVidriado_LanzaUnAbanicoDeCinco()
    {
        yield return Cargar("Sandbox");
        Assert.IsTrue(arsenal.Recoger(arsenal.Arma(Vidriado), 20));
        Assert.AreEqual(Vidriado, arsenal.Indice, "La caja nueva se equipa sola");

        yield return Pulsar(Key.Space);
        var jarritos = Object.FindObjectsByType<Projectile>().Where(p => p.name.StartsWith("Proyectil_Vidriado")).ToArray();
        Assert.AreEqual(5, jarritos.Length, "Cinco jarritos por lanzamiento");
        float[] angulos = jarritos.Select(p => Vector2.SignedAngle(Vector2.right, p.GetComponent<Rigidbody2D>().linearVelocity)).ToArray();
        Assert.AreEqual(40f, angulos.Max() - angulos.Min(), 6f, "Repartidos en abanico de ~40°");
        Assert.AreEqual(19, arsenal.MunicionActual, "Cada lanzamiento gasta uno");
    }

    [UnityTest]
    public IEnumerator BarroNegro_VuelaEnArcoYExplota()
    {
        yield return Cargar("Sandbox");
        var dummy = Object.FindObjectsByType<Health>().First(h => h.GetComponent<PlayerController>() == null);
        dummy.transform.position = jugador.transform.position + new Vector3(4.5f, 0f, 0f);
        Physics2D.SyncTransforms();
        int vidaDummy = dummy.VidaActual;
        arsenal.Recoger(arsenal.Arma(Negro), 5);

        yield return Pulsar(Key.Space);
        var granada = Object.FindObjectsByType<Projectile>().First(p => p.name.StartsWith("Proyectil_BarroNegro"));
        float yInicial = granada.transform.position.y, yMax = yInicial;
        for (float t = 0f; t < 2.5f && dummy.VidaActual == vidaDummy; t += Time.deltaTime)
        {
            if (granada.gameObject.activeSelf) yMax = Mathf.Max(yMax, granada.transform.position.y);
            yield return null;
        }
        Assert.Greater(yMax, yInicial + 0.5f, "Sube antes de caer: va en arco");
        Assert.AreEqual(vidaDummy - arsenal.Arma(Negro).danoExplosion, dummy.VidaActual, "La explosión lo alcanza");
        Assert.IsTrue(Object.FindObjectsByType<SpriteAnimadoTemporal>().Any(e => e.name.StartsWith("Explosion_BarroNegro")), "Se ve la explosión");
    }

    [UnityTest]
    public IEnumerator MacetaEscudo_DetieneBalasDeFrenteYSeRompeAlQuinto()
    {
        yield return Cargar("Sandbox");
        var salud = jugador.GetComponent<Health>();
        var escudo = jugador.GetComponentInChildren<EscudoMaceta>();
        arsenal.Recoger(arsenal.Arma(Maceta), 5);
        yield return null;
        Assert.IsTrue(escudo.Activo, "Mateo carga la maceta");
        Assert.IsTrue(escudo.GetComponentInChildren<SpriteRenderer>().enabled);

        Vector2 pecho = (Vector2)jugador.transform.position + Vector2.up * 1.2f;
        int vida = salud.VidaActual;
        for (int i = 0; i < 5; i++)
        {
            Bala(pecho + Vector2.right * 4f, Vector2.left);   // De frente (Mateo mira a la derecha)
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(vida, salud.VidaActual, $"La bala {i + 1} se estrella en la maceta");
        }
        Assert.AreEqual(0, arsenal.Indice, "Al quinto golpe se rompe y vuelve a la Talavera");
        Assert.IsFalse(escudo.Activo);

        Bala(pecho + Vector2.right * 4f, Vector2.left);
        yield return new WaitForSeconds(0.5f);
        Assert.Less(salud.VidaActual, vida, "Sin maceta la bala ya lo lastima");
    }

    [UnityTest]
    public IEnumerator MacetaEscudo_NoCubreLaEspalda()
    {
        yield return Cargar("Sandbox");
        var salud = jugador.GetComponent<Health>();
        arsenal.Recoger(arsenal.Arma(Maceta), 5);
        int vida = salud.VidaActual;
        Bala((Vector2)jugador.transform.position + new Vector2(-4f, 1.2f), Vector2.right);
        yield return new WaitForSeconds(0.5f);
        Assert.Less(salud.VidaActual, vida);
        Assert.AreEqual(5, arsenal.MunicionDe(Maceta), "La maceta no recibió el golpe");
    }

    [UnityTest]
    public IEnumerator CambioDeArma_TeclasRuedaYBumpersSoloConMunicion()
    {
        yield return Cargar("Sandbox");
        Assert.AreEqual(0, arsenal.Indice, "Empieza con la Talavera");
        yield return Pulsar(Key.Digit2);
        Assert.AreEqual(0, arsenal.Indice, "Sin Barro Vidriado no se puede elegir");

        arsenal.Recoger(arsenal.Arma(Vidriado), 20);
        arsenal.Recoger(arsenal.Arma(Negro), 5);
        yield return Pulsar(Key.Digit1);
        Assert.AreEqual(0, arsenal.Indice);
        yield return Pulsar(Key.Digit3);
        Assert.AreEqual(Negro, arsenal.Indice, "Tecla 3: Barro Negro");

        // Rueda hacia abajo: la siguiente que se tenga (la maceta no, así que da la vuelta a la Talavera)
        InputSystem.QueueStateEvent(mouse, new MouseState { scroll = new Vector2(0f, -120f) });
        yield return null;
        InputSystem.QueueStateEvent(mouse, new MouseState());
        yield return null;
        Assert.AreEqual(0, arsenal.Indice, "Rueda: siguiente arma con munición");

        InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.RightShoulder));
        yield return null;
        InputSystem.QueueStateEvent(gamepad, new GamepadState());
        yield return null;
        Assert.AreEqual(Vidriado, arsenal.Indice, "Bumper derecho: siguiente");

        // Al acabarse la munición regresa a la Talavera
        arsenal.Seleccionar(Negro);
        for (int i = 0; i < 5; i++) arsenal.Gastar();
        Assert.AreEqual(0, arsenal.Indice);
        Assert.IsFalse(arsenal.Disponible(Negro));
    }

    [UnityTest]
    public IEnumerator Huacal_SueltaLaCajaQueDesbloqueaElArmaYSeVeEnElHud()
    {
        yield return Cargar("Nivel1_Dolores");
        jugador.GetComponent<Health>().HacerInvulnerable(999f);
        var huacal = Object.FindObjectsByType<Destructible>().First(d => d.name.StartsWith("Huacal_Vidriado"));
        huacal.GetComponent<Health>().RecibirDano(9999);
        yield return new WaitForSeconds(0.3f);

        var caja = Object.FindObjectsByType<Recogible>().First(r => r.Tipo == TipoRecogible.Arma);
        Assert.AreEqual(CeramicaData.TipoCeramica.BarroVidriado, caja.Arma.tipo);
        jugador.GetComponent<Rigidbody2D>().position = caja.transform.position;
        yield return new WaitForSeconds(0.3f);

        Assert.AreEqual(Vidriado, arsenal.Indice, "Al recogerla se equipa");
        CollectionAssert.Contains(GestorGuardado.Datos.armasDesbloqueadas, "BarroVidriado", "Queda desbloqueada en la partida");
        var textos = Object.FindObjectsByType<Text>();
        Assert.AreEqual(arsenal.MunicionActual.ToString(), textos.First(t => t.name == "Municion").text, "El HUD muestra la munición");
        Assert.AreEqual(caja.Arma.icono, Object.FindObjectsByType<Image>().First(i => i.name == "IconoArma").sprite, "y el icono del arma");

        // El siguiente nivel (o al reintentar) empieza con una caja de lo desbloqueado, pero con la Talavera en la mano
        yield return Cargar("Nivel1_Dolores");
        Assert.AreEqual(0, arsenal.Indice);
        Assert.AreEqual(arsenal.Arma(Vidriado).municionPorCaja, arsenal.MunicionDe(Vidriado));
        Assert.AreEqual("∞", Object.FindObjectsByType<Text>().First(t => t.name == "Municion").text);
    }

    [Test]
    public void Sonidos_CadaArmaTieneLosSuyos()
    {
        var biblioteca = Resources.Load<BibliotecaAudio>("BibliotecaAudio");
        foreach (var id in new[] { Sfx.CambioArma, Sfx.RecogerArma, Sfx.LanzarVidriado, Sfx.Explosion, Sfx.BloqueoEscudo, Sfx.RomperEscudo })
            Assert.IsTrue(biblioteca.efectos.Any(e => e.id == id && e.clips.Length > 0), $"Falta el sonido {id}");
    }
}
