using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Texto de los créditos (autores, herramientas, recursos y licencias). Es la misma lista que Docs/CREDITOS.md:
/// si se cambia aquí, cambiarla allá también.
/// </summary>
public static class TextoCreditos
{
    public const string Contenido =
        "EL ALFARERO DE DOLORES\n\n\n" +

        "== EQUIPO ==\n\n" +
        "OMAR SALINAS\n" +
        "PROGRAMACION DE SISTEMAS Y NIVELES\n" +
        "GUARDADO, MENUS, HUD, ARMAS, NIVEL 2\n" +
        "BRUJA DE FUEGO, PRUEBAS Y BUILDS\n\n" +
        "NOE CHAVERO\n" +
        "PROGRAMACION DE IA Y JEFES\n" +
        "ARTE EN FLOW, NARRATIVA, TUTORIAL\n" +
        "EFECTOS, CONTROLES Y DOCUMENTACION\n\n\n" +

        "== MATERIA ==\n\n" +
        "CREACION DE VIDEOJUEGOS\n" +
        "UTNG  ·  GIDS6102\n" +
        "PROFESOR GABRIEL BARRON\n\n\n" +

        "== HERRAMIENTAS ==\n\n" +
        "UNITY 6.6 (6000.6)\n" +
        "UNIVERSAL RENDER PIPELINE 2D\n" +
        "INPUT SYSTEM\n" +
        "GOOGLE FLOW: SPRITES Y FONDOS\n" +
        "GOOGLE FLOW MUSIC: MUSICA\n" +
        "PYTHON + PILLOW: RECORTE DE SPRITES\n" +
        "GIT, GITHUB Y GIT LFS\n\n\n" +

        "== RECURSOS Y LICENCIAS ==\n\n" +
        "FUENTE PRESS START 2P\n" +
        "CODEMAN38  ·  SIL OPEN FONT LICENSE 1.1\n\n" +
        "MUSICA GENERADA CON GOOGLE FLOW MUSIC\n" +
        "PARA ESTE PROYECTO\n\n" +
        "EFECTOS DE SONIDO SINTETIZADOS\n" +
        "POR CODIGO PARA ESTE PROYECTO\n\n" +
        "SPRITES, FONDOS E ILUSTRACIONES\n" +
        "GENERADOS CON GOOGLE FLOW\n" +
        "Y RETOCADOS POR EL EQUIPO\n\n\n" +

        "== LEYENDAS ==\n\n" +
        "EL ALEBRIJE, LA BRUJA DE FUEGO,\n" +
        "LOS NAHUALES, EL JINETE SIN CABEZA,\n" +
        "LA LLORONA Y EL CHARRO NEGRO\n" +
        "SON TRADICION ORAL DE MEXICO\n\n\n" +

        "GRACIAS POR JUGAR";
}

/// <summary>
/// Desplaza los créditos hacia arriba solos, dentro de una máscara; al terminar vuelven a empezar.
/// Mantener abajo (o el stick) los acelera y arriba los regresa. Usa tiempo real.
/// </summary>
public class CreditosDesplazables : MonoBehaviour
{
    [SerializeField] RectTransform contenido;      // Texto alto que se mueve
    [SerializeField] RectTransform ventana;        // Área visible (con RectMask2D)
    [SerializeField] float velocidad = 60f;        // Unidades de canvas por segundo
    [SerializeField] float multiplicadorRapido = 5f;

    public float Posicion => contenido.anchoredPosition.y;

    void OnEnable() { Reiniciar(); }

    /// <summary>Pone el texto justo debajo del área visible.</summary>
    public void Reiniciar()
    {
        if (contenido != null) contenido.anchoredPosition = new Vector2(0f, -ventana.rect.height);
    }

    void Update()
    {
        if (contenido == null) return;
        float eje = LeerEje();
        float v = velocidad * (eje < -0.3f ? multiplicadorRapido : eje > 0.3f ? -multiplicadorRapido : 1f);
        var p = contenido.anchoredPosition;
        p.y += v * Time.unscaledDeltaTime;

        float alto = contenido.rect.height;
        if (p.y > alto) p.y = -ventana.rect.height;                 // Terminó: vuelve a empezar desde abajo
        p.y = Mathf.Max(p.y, -ventana.rect.height);
        contenido.anchoredPosition = p;
    }

    static float LeerEje()
    {
        float eje = 0f;
        var k = Keyboard.current;
        if (k != null)
        {
            if (k.downArrowKey.isPressed || k.sKey.isPressed) eje -= 1f;
            if (k.upArrowKey.isPressed || k.wKey.isPressed) eje += 1f;
        }
        var g = Gamepad.current;
        if (g != null) eje += g.leftStick.y.ReadValue() + g.dpad.y.ReadValue();
        return Mathf.Clamp(eje, -1f, 1f);
    }
}
