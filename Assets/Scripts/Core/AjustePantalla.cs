using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Cálculo de la zona de juego (#37). El juego se diseñó en 16:9 (la cámara ve 21.3 x 12 unidades y las arenas de
/// jefes miden una pantalla): en otras proporciones se muestra la misma zona 16:9 lo más grande posible dentro de la
/// Safe Area (muescas y bordes redondeados de los celulares) y lo que sobra queda en barras negras.
/// </summary>
public static class Pantalla
{
    public const float Aspecto = 16f / 9f;

    /// <summary>Rectángulo 16:9 en píxeles, centrado dentro del área segura.</summary>
    public static Rect AreaJuego(float ancho, float alto, Rect segura)
    {
        if (segura.width <= 0f || segura.height <= 0f) segura = new Rect(0f, 0f, ancho, alto);
        float w = segura.width, h = segura.height;
        if (w / h > Aspecto) w = h * Aspecto;      // Pantalla más ancha (21:9, celular acostado): barras a los lados
        else h = w / Aspecto;                      // Más alta (16:10, 4:3): barras arriba y abajo
        return new Rect(segura.x + (segura.width - w) * 0.5f, segura.y + (segura.height - h) * 0.5f, w, h);
    }

    public static Rect AreaJuego() => AreaJuego(Screen.width, Screen.height, Screen.safeArea);

    /// <summary>La zona de juego en coordenadas 0-1 (para Camera.rect y anclas de UI).</summary>
    public static Rect AreaNormalizada()
    {
        float w = Mathf.Max(1, Screen.width), h = Mathf.Max(1, Screen.height);
        Rect a = AreaJuego();
        return new Rect(a.x / w, a.y / h, a.width / w, a.height / h);
    }
}

/// <summary>
/// Se crea solo al arrancar y vive entre escenas. Pone el viewport 16:9 a las cámaras de cada escena y pinta de
/// negro las barras con una cámara de fondo que no dibuja nada (con URP, lo que queda fuera del viewport no se
/// limpia y se verían restos del cuadro anterior: los "bordes rotos").
/// </summary>
public class AjustePantalla : MonoBehaviour
{
    static AjustePantalla instancia;

    Camera fondo;
    Vector2Int tamano;
    Rect segura;
    Camera[] camaras = new Camera[8];

    public static Rect Viewport { get; private set; } = new Rect(0f, 0f, 1f, 1f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ReiniciarEstaticos()
    {
        instancia = null;
        Viewport = new Rect(0f, 0f, 1f, 1f);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Crear()
    {
        if (instancia != null) return;
        var go = new GameObject("AjustePantalla");
        DontDestroyOnLoad(go);
        instancia = go.AddComponent<AjustePantalla>();
    }

    void Awake()
    {
        fondo = gameObject.AddComponent<Camera>();
        fondo.clearFlags = CameraClearFlags.SolidColor;
        fondo.backgroundColor = Color.black;
        fondo.cullingMask = 0;
        fondo.depth = -100;                        // Antes que cualquier cámara del juego
        fondo.orthographic = true;
        Recalcular();
        SceneManager.sceneLoaded += AlCargar;
    }

    void OnDestroy() { SceneManager.sceneLoaded -= AlCargar; }

    void AlCargar(Scene escena, LoadSceneMode modo) { AplicarACamaras(); }

    void Recalcular()
    {
        tamano = new Vector2Int(Screen.width, Screen.height);
        segura = Screen.safeArea;
        Viewport = Pantalla.AreaNormalizada();
        fondo.enabled = Viewport.width < 0.999f || Viewport.height < 0.999f;   // En 16:9 exacto no hay barras que pintar
    }

    // Revisa cada cuadro: cambia de tamaño la ventana, gira el celular o aparece una cámara nueva (cinemáticas)
    void LateUpdate()
    {
        if (tamano.x != Screen.width || tamano.y != Screen.height || segura != Screen.safeArea) Recalcular();
        AplicarACamaras();
    }

    void AplicarACamaras()
    {
        if (Camera.allCamerasCount > camaras.Length) camaras = new Camera[Camera.allCamerasCount * 2];
        int n = Camera.GetAllCameras(camaras);
        for (int i = 0; i < n; i++)
        {
            var cam = camaras[i];
            if (cam != fondo && cam.targetTexture == null && cam.rect != Viewport) cam.rect = Viewport;
            camaras[i] = null;
        }
    }
}
