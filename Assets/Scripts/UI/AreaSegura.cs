using UnityEngine;

/// <summary>
/// Va en cada canvas de pantalla (#37). Acomoda a sus hijos directos dentro de la zona de juego 16:9 y la Safe Area
/// (ver Pantalla): así el HUD y los menús quedan sobre la imagen del juego y nunca debajo de una muesca ni en las
/// barras negras. No cambia la jerarquía: solo traslada las anclas de cada hijo al rectángulo de la zona.
/// Los fondos que deben cubrir toda la pantalla se quedan como están.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class AreaSegura : MonoBehaviour
{
    // Imágenes a pantalla completa que no se encogen (oscurecer, negro de la intro, portada)
    static readonly string[] Excluidos = { "Fondo", "Oscurecer", "Negro" };

    RectTransform[] hijos;
    Vector2[] anclaMin, anclaMax;                  // Las de diseño (relativas a la pantalla 16:9 completa)
    Rect aplicada = new Rect(0f, 0f, 1f, 1f);

    /// <summary>Rectángulo (0-1) en el que se acomodó el contenido.</summary>
    public Rect Area => aplicada;

    void Awake()
    {
        int n = transform.childCount;
        hijos = new RectTransform[n];
        anclaMin = new Vector2[n];
        anclaMax = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            var rt = transform.GetChild(i) as RectTransform;
            if (rt == null || System.Array.IndexOf(Excluidos, rt.name) >= 0) continue;
            hijos[i] = rt;
            anclaMin[i] = rt.anchorMin;
            anclaMax[i] = rt.anchorMax;
        }
        Actualizar();
    }

    void Update()
    {
        if (AjustePantalla.Viewport != aplicada) Actualizar();
    }

    void Actualizar()
    {
        aplicada = AjustePantalla.Viewport;
        for (int i = 0; i < hijos.Length; i++)
        {
            if (hijos[i] == null) continue;
            hijos[i].anchorMin = aplicada.min + Vector2.Scale(anclaMin[i], aplicada.size);
            hijos[i].anchorMax = aplicada.min + Vector2.Scale(anclaMax[i], aplicada.size);
        }
    }
}
