using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;
using static ConfigurarSprint1;

/// <summary>
/// Controles por plataforma (#36): los controles táctiles en pantalla de cada nivel y los textos de ayuda que
/// cambian según el dispositivo (teclado, gamepad o táctil).
/// Los botones táctiles usan OnScreenStick/OnScreenButton del Input System: alimentan un gamepad virtual con las
/// mismas rutas que un control de verdad, así que el juego no necesita lógica aparte para el dedo.
/// </summary>
public static partial class ConstructorJuego
{
    // (nombre, etiqueta, ruta del gamepad virtual, posición desde la esquina inferior derecha, tamaño)
    static readonly (string nombre, string etiqueta, string ruta, Vector2 pos, float tam)[] BotonesTactiles =
    {
        ("Lanza",   "LANZA",   "<Gamepad>/buttonWest",     new Vector2(-170f, 170f), 190f),   // Mantener: ráfaga; montado, zarpazo
        ("Salto",   "SALTO",   "<Gamepad>/buttonSouth",    new Vector2(-380f, 120f), 150f),
        ("Evade",   "EVADE",   "<Gamepad>/buttonEast",     new Vector2(-150f, 380f), 150f),
        ("Caguama", "CAGUAMA", "<Gamepad>/buttonNorth",    new Vector2(-350f, 300f), 130f),
        ("Arma",    "ARMA",    "<Gamepad>/rightShoulder",  new Vector2(-560f, 90f),  120f),
        ("Monta",   "MONTA",   "<Gamepad>/leftStickPress", new Vector2(-540f, 250f), 120f),
    };

    static void CrearControlesTactiles()
    {
        Canvas canvas = CrearCanvas("ControlesTactiles", 20);   // Sobre el HUD, debajo de pausa y pantallas finales
        canvas.gameObject.AddComponent<CanvasGroup>();
        var contenido = Estirar(canvas.transform, "Controles");
        Sprite boton = Spr("UI/tactil_boton.png"), baseStick = Spr("UI/tactil_base.png"), palanca = Spr("UI/tactil_palanca.png");

        // Joystick a la izquierda: la palanca se arrastra dentro del aro y mueve el stick izquierdo
        var abajoIzq = new Vector2(0f, 0f);
        Image aro = Imagen(contenido, "Joystick", baseStick, new Vector2(250f, 230f), new Vector2(300f, 300f), abajoIzq);
        aro.color = new Color(1f, 1f, 1f, 0.75f);
        Image mango = Imagen(aro.transform, "Palanca", palanca, Vector2.zero, new Vector2(130f, 130f));
        mango.raycastTarget = true;
        var stick = mango.gameObject.AddComponent<OnScreenStick>();
        stick.controlPath = "<Gamepad>/leftStick";
        stick.movementRange = 95f;

        var abajoDer = new Vector2(1f, 0f);
        foreach (var b in BotonesTactiles)
        {
            Image img = Imagen(contenido, b.nombre, boton, b.pos, new Vector2(b.tam, b.tam), abajoDer);
            img.raycastTarget = true;
            img.color = new Color(1f, 1f, 1f, 0.8f);
            Texto(img.transform, "Etiqueta", b.etiqueta, b.tam > 140f ? 22 : 16, Crema, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(b.tam, 40f));
            img.gameObject.AddComponent<OnScreenButton>().controlPath = b.ruta;
        }

        // Pausa arriba al centro, debajo del objetivo
        Image pausa = Imagen(contenido, "Pausa", boton, new Vector2(0f, -230f), new Vector2(90f, 90f), new Vector2(0.5f, 1f));
        pausa.raycastTarget = true;
        pausa.color = new Color(1f, 1f, 1f, 0.7f);
        Texto(pausa.transform, "Etiqueta", "II", 26, Crema, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(90f, 40f));
        pausa.gameObject.AddComponent<OnScreenButton>().controlPath = "<Gamepad>/start";

        var tactiles = canvas.gameObject.AddComponent<ControlesTactiles>();
        Asignar(tactiles, "contenido", contenido.gameObject);
    }

    /// <summary>Pone a un texto de ayuda sus versiones para teclado, gamepad y táctil.</summary>
    static void SegunDispositivo(Text texto, string teclado, string gamepad, string tactil)
    {
        var t = texto.gameObject.AddComponent<TextoSegunDispositivo>();
        var so = new SerializedObject(t);
        so.FindProperty("teclado").stringValue = teclado;
        so.FindProperty("gamepad").stringValue = gamepad;
        so.FindProperty("tactil").stringValue = tactil;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
