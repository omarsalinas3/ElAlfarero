using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static ConfigurarSprint1;

/// <summary>
/// Tutorial integrado al inicio del Nivel 1 (#28): letreros de madera en la zona segura (antes del primer minero) que
/// explican el objetivo y cada control. Cada letrero tiene el texto para teclado y para gamepad (CartelTutorial).
/// </summary>
public static partial class ConstructorJuego
{
    // (x, texto con teclado, texto con gamepad). La zona segura va del inicio (x = -6) al primer minero (x = 9)
    static readonly (float x, string teclado, string gamepad)[] CartelesTutorial =
    {
        (-6.5f, "LLEGA A LA PARROQUIA\nY RESCATA A CARMEN\n\nMUEVETE: A D O FLECHAS", "LLEGA A LA PARROQUIA\nY RESCATA A CARMEN\n\nMUEVETE: STICK IZQUIERDO"),
        (-2.5f, "SALTA: W O FLECHA ARRIBA\n\nEVADE: SHIFT\n(INVENCIBLE UN INSTANTE)", "SALTA: A\n\nEVADE: B\n(INVENCIBLE UN INSTANTE)"),
        (1f, "LANZA: ESPACIO O CLIC\nAPUNTA: MOUSE\nARRIBA: ESPACIO + FLECHA ARRIBA", "LANZA: X O GATILLO DERECHO\nAPUNTA: STICK DERECHO"),
        (4.5f, "ROMPE HUACALES Y HORNOS:\nTRAEN MONEDAS Y CURACION\n\nCAGUAMA: E O Q\n(5 S INVENCIBLE)", "ROMPE HUACALES Y HORNOS:\nTRAEN MONEDAS Y CURACION\n\nCAGUAMA: Y\n(5 S INVENCIBLE)"),
    };

    static void CrearTutorial()
    {
        var padre = new GameObject("Tutorial").transform;
        foreach (var c in CartelesTutorial) CrearCartel(padre, c.x, c.teclado, c.gamepad);
    }

    static CartelTutorial CrearCartel(Transform padre, float x, string teclado, string gamepad)
    {
        var go = new GameObject("Cartel");
        go.transform.SetParent(padre, false);
        go.transform.position = new Vector3(x, YSuelo + 4.6f, 0f);   // Sobre la cabeza de Mateo, sin tapar el suelo

        // Canvas en el mundo: 1 unidad = 100 px de referencia
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 4;                                    // Delante del fondo, detrás de personajes
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(340f, 170f);
        rt.localScale = Vector3.one * 0.01f;
        var grupo = go.AddComponent<CanvasGroup>();

        var tabla = Estirar(go.transform, "Tabla").gameObject.AddComponent<Image>();
        tabla.sprite = Spr("UI/panel.png");
        tabla.type = Image.Type.Sliced;
        tabla.pixelsPerUnitMultiplier = 3f;                         // Borde fino a esta escala
        tabla.raycastTarget = false;
        Text texto = Texto(go.transform, "Texto", teclado, 14, Crema, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(310f, 150f));

        var cartel = go.AddComponent<CartelTutorial>();
        var so = new SerializedObject(cartel);
        so.FindProperty("texto").objectReferenceValue = texto;
        so.FindProperty("grupo").objectReferenceValue = grupo;
        so.FindProperty("textoTeclado").stringValue = teclado;
        so.FindProperty("textoGamepad").stringValue = gamepad;
        so.ApplyModifiedPropertiesWithoutUndo();
        return cartel;
    }
}
