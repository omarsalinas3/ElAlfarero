using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Nombres de las escenas del juego (deben estar en Build Settings).</summary>
public static class Escenas
{
    public const string Splash = "Splash";
    public const string Menu = "MenuPrincipal";
    public const string Intro = "Intro";
    public const string Nivel1 = "Nivel1_Dolores";
    public const string Nivel2 = "Nivel2_Pozos";

    // En orden: el número de nivel del guardado es su posición + 1
    static readonly string[] Niveles = { Nivel1, Nivel2 };

    /// <summary>Escena del nivel n (1 = Dolores). Si ese nivel aún no existe, el último que sí.</summary>
    public static string Nivel(int n) => Niveles[Mathf.Clamp(n, 1, Niveles.Length) - 1];
}

/// <summary>
/// Cambia de escena con un fundido a negro. Uso: CambioEscena.Cargar(Escenas.Nivel1).
/// Se crea solo y sobrevive entre escenas; siempre devuelve el tiempo a velocidad normal (por si se venía de una pausa).
/// </summary>
public class CambioEscena : MonoBehaviour
{
    static CambioEscena instancia;
    Image cortina;
    bool cargando;

    public static bool Cargando => instancia != null && instancia.cargando;

    public static void Cargar(string escena, float duracion = 0.45f)
    {
        if (instancia == null)
        {
            var go = new GameObject("CambioEscena");
            DontDestroyOnLoad(go);
            instancia = go.AddComponent<CambioEscena>();
            instancia.CrearCortina();
        }
        if (!instancia.cargando) instancia.StartCoroutine(instancia.Transicion(escena, duracion));
    }

    public static void Reiniciar() => Cargar(SceneManager.GetActiveScene().name);

    void CrearCortina()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        var img = new GameObject("Cortina").AddComponent<Image>();
        img.transform.SetParent(transform, false);
        img.rectTransform.anchorMin = Vector2.zero;
        img.rectTransform.anchorMax = Vector2.one;
        img.rectTransform.offsetMin = img.rectTransform.offsetMax = Vector2.zero;
        img.color = new Color(0f, 0f, 0f, 0f);
        img.raycastTarget = false;
        cortina = img;
    }

    IEnumerator Transicion(string escena, float duracion)
    {
        cargando = true;
        cortina.raycastTarget = true;                       // bloquea clics durante el fundido
        yield return Fundir(0f, 1f, duracion);

        Time.timeScale = 1f;
        AsyncOperation op = SceneManager.LoadSceneAsync(escena);
        while (!op.isDone) yield return null;

        yield return Fundir(1f, 0f, duracion);
        cortina.raycastTarget = false;
        cargando = false;
    }

    IEnumerator Fundir(float desde, float hasta, float duracion)
    {
        for (float t = 0f; t < duracion; t += Time.unscaledDeltaTime)
        {
            cortina.color = new Color(0f, 0f, 0f, Mathf.Lerp(desde, hasta, t / duracion));
            yield return null;
        }
        cortina.color = new Color(0f, 0f, 0f, hasta);
    }
}
