using UnityEngine;

/// <summary>Tipos de partículas del juego (#30).</summary>
public enum TipoParticula { Polvo, Aterrizaje, Fragmentos, Astillas, Brillo, Chispas }

/// <summary>
/// Retroalimentación visual (#30): partículas de pixel, sacudida de cámara y pausa breve en golpes fuertes (hit stop).
/// Uso: Efectos.Particulas(TipoParticula.Polvo, pos), Efectos.GolpeFuerte(0.06f, 0.2f).
///
/// Las partículas son cuadritos con SpriteRenderer que se crean una sola vez (máximo fijo) y se reutilizan en anillo;
/// un solo Update las mueve a todas, sin un MonoBehaviour por partícula ni basura en memoria.
/// </summary>
public class Efectos : MonoBehaviour
{
    const int Maximo = 240;
    const float TamPixel = 1f / 16f;               // Un "pixel" de partícula: del tamaño del pixel art de Mateo

    static Efectos instancia;
    static Sprite cuadro;

    readonly SpriteRenderer[] sr = new SpriteRenderer[Maximo];
    readonly Vector2[] velocidad = new Vector2[Maximo];
    readonly float[] vida = new float[Maximo];
    readonly float[] vidaTotal = new float[Maximo];
    readonly float[] gravedad = new float[Maximo];
    readonly Color[] color = new Color[Maximo];
    int siguiente;
    float hitStopHasta;
    float escalaPrevia = 1f;                       // Algunas pruebas aceleran el juego: se regresa a la que había
    bool enHitStop;

    /// <summary>Partículas vivas ahora (para las pruebas).</summary>
    public static int Activas { get; private set; }
    public static int Creadas => instancia == null ? 0 : instancia.creadas;
    public static bool EnHitStop => instancia != null && instancia.enHitStop;
    int creadas;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ReiniciarEstaticos()
    {
        instancia = null;
        Activas = 0;
    }

    static Efectos Instancia
    {
        get
        {
            if (instancia == null)
            {
                // Vive en la escena: al cambiar de escena se va con ella y se crea otro con la siguiente
                instancia = new GameObject("Efectos").AddComponent<Efectos>();
            }
            return instancia;
        }
    }

    // ------------------------------------------------------------------ Partículas
    public static void Particulas(TipoParticula tipo, Vector2 pos, int direccion = 0)
    {
        var e = Instancia;
        switch (tipo)
        {
            case TipoParticula.Polvo:              // Al correr: un par de motas que se quedan atrás
                for (int i = 0; i < 2; i++)
                    e.Emitir(pos + new Vector2(Random.Range(-0.2f, 0.2f), 0.05f), new Vector2(-direccion * Random.Range(0.4f, 1.2f), Random.Range(0.3f, 0.9f)),
                             Random.Range(0.25f, 0.4f), 0.5f, ColorPolvo(), Random.Range(1, 3));
                break;
            case TipoParticula.Aterrizaje:         // Nube a los dos lados de los pies
                for (int i = 0; i < 10; i++)
                {
                    float lado = i % 2 == 0 ? 1f : -1f;
                    e.Emitir(pos + new Vector2(lado * Random.Range(0.1f, 0.4f), 0.05f), new Vector2(lado * Random.Range(1.2f, 3f), Random.Range(0.2f, 1.2f)),
                             Random.Range(0.3f, 0.5f), 1.5f, ColorPolvo(), Random.Range(2, 4));
                }
                break;
            case TipoParticula.Fragmentos:         // Cerámica rota: blanco y azul Talavera que saltan y caen
                for (int i = 0; i < 9; i++)
                    e.Emitir(pos, Random.insideUnitCircle * 4f + Vector2.up * 2.5f, Random.Range(0.5f, 0.8f), 14f,
                             i % 3 == 0 ? new Color(0.25f, 0.4f, 0.95f) : new Color(0.95f, 0.93f, 0.85f), Random.Range(2, 4));
                break;
            case TipoParticula.Astillas:           // Huacales, hornos y muros: madera y barro
                for (int i = 0; i < 14; i++)
                    e.Emitir(pos + Random.insideUnitCircle * 0.4f, Random.insideUnitCircle * 5f + Vector2.up * 3f, Random.Range(0.6f, 1f), 16f,
                             i % 2 == 0 ? new Color(0.55f, 0.33f, 0.16f) : new Color(0.72f, 0.45f, 0.25f), Random.Range(2, 5));
                break;
            case TipoParticula.Brillo:             // Al recoger: destellos dorados que suben
                for (int i = 0; i < 10; i++)
                    e.Emitir(pos + Random.insideUnitCircle * 0.35f, new Vector2(Random.Range(-0.8f, 0.8f), Random.Range(1.5f, 3.2f)), Random.Range(0.4f, 0.7f), -1f,
                             i % 2 == 0 ? new Color(1f, 0.85f, 0.3f) : Color.white, Random.Range(1, 3));
                break;
            case TipoParticula.Chispas:            // Golpe fuerte y explosión: chispas naranjas rápidas
                for (int i = 0; i < 12; i++)
                    e.Emitir(pos, Random.insideUnitCircle.normalized * Random.Range(3f, 7f), Random.Range(0.2f, 0.4f), 6f,
                             i % 2 == 0 ? new Color(1f, 0.6f, 0.15f) : new Color(1f, 0.9f, 0.4f), Random.Range(1, 3));
                break;
        }
    }

    static Color ColorPolvo() => Color.Lerp(new Color(0.62f, 0.52f, 0.4f, 0.9f), new Color(0.8f, 0.72f, 0.6f, 0.9f), Random.value);

    void Emitir(Vector2 pos, Vector2 vel, float duracion, float grav, Color c, int pixeles)
    {
        int i = siguiente;
        siguiente = (siguiente + 1) % Maximo;       // Si se llena, se recicla la más vieja
        if (sr[i] == null)
        {
            var go = new GameObject("Particula");
            go.transform.SetParent(transform, false);
            sr[i] = go.AddComponent<SpriteRenderer>();
            sr[i].sprite = Cuadro();
            sr[i].sortingOrder = 40;                // Delante de personajes y escenario
            creadas++;
        }
        if (vida[i] <= 0f) Activas++;
        sr[i].enabled = true;
        sr[i].transform.position = pos;
        sr[i].transform.localScale = Vector3.one * (pixeles * TamPixel);
        sr[i].color = c;
        velocidad[i] = vel;
        vida[i] = vidaTotal[i] = duracion;
        gravedad[i] = grav;
        color[i] = c;
    }

    static Sprite Cuadro()
    {
        if (cuadro != null) return cuadro;
        var tex = new Texture2D(1, 1) { filterMode = FilterMode.Point };
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        cuadro = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return cuadro;
    }

    void Update()
    {
        if (enHitStop && Time.unscaledTime >= hitStopHasta) TerminarHitStop();

        float dt = Time.deltaTime;
        for (int i = 0; i < Maximo; i++)
        {
            if (vida[i] <= 0f) continue;
            vida[i] -= dt;
            if (vida[i] <= 0f)
            {
                sr[i].enabled = false;
                Activas--;
                continue;
            }
            velocidad[i].y -= gravedad[i] * dt;
            var t = sr[i].transform;
            t.position += (Vector3)(velocidad[i] * dt);
            var c = color[i];
            c.a *= Mathf.Clamp01(vida[i] / (vidaTotal[i] * 0.5f));   // Se desvanece la segunda mitad de su vida
            sr[i].color = c;
        }
    }

    void OnDestroy()
    {
        if (instancia == this)
        {
            if (enHitStop) TerminarHitStop();
            instancia = null;
            Activas = 0;
        }
    }

    // ------------------------------------------------------------------ Sacudida y hit stop
    /// <summary>
    /// Golpe fuerte: congela el juego un instante (hit stop) y sacude la cámara. No pisa la pausa: si alguien pone
    /// timeScale en 0 mientras tanto, al terminar se respeta.
    /// </summary>
    public static void GolpeFuerte(float pausa, float sacudida, float duracionSacudida = 0.2f)
    {
        var cam = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (cam != null && sacudida > 0f) cam.Sacudir(duracionSacudida, sacudida);
        if (pausa <= 0f || Time.timeScale == 0f) return;

        var e = Instancia;
        if (!e.enHitStop)
        {
            e.enHitStop = true;
            e.escalaPrevia = Time.timeScale;
            Time.timeScale = 0.05f;
        }
        e.hitStopHasta = Mathf.Max(e.hitStopHasta, Time.unscaledTime + pausa);
    }

    void TerminarHitStop()
    {
        enHitStop = false;
        if (Mathf.Approximately(Time.timeScale, 0.05f)) Time.timeScale = escalaPrevia;   // Si se pausó en medio, se queda en pausa
    }
}
