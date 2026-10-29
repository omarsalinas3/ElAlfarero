using System.Collections;
using UnityEngine;

/// <summary>
/// Música y efectos de todo el juego. Se crea solo la primera vez que alguien lo usa y sobrevive entre escenas.
/// Uso: GestorAudio.Reproducir(Sfx.Salto);  GestorAudio.CambiarMusica(Musica.Nivel1);
/// Los volúmenes se guardan en PlayerPrefs (menú de opciones).
/// </summary>
public class GestorAudio : MonoBehaviour
{
    const string ClaveMusica = "vol_musica", ClaveEfectos = "vol_efectos";

    static GestorAudio instancia;
    static bool cerrando;

    BibliotecaAudio biblioteca;
    AudioSource fuenteMusica, fuenteMusicaB, fuenteEfectos;
    Musica musicaActual = Musica.Ninguna;
    float volumenBaseMusica = 0.6f;
    float atenuacion = 1f;
    readonly float[] ultimoUso = new float[System.Enum.GetValues(typeof(Sfx)).Length];

    public static float VolumenMusica
    {
        get => PlayerPrefs.GetFloat(ClaveMusica, 0.7f);
        set { PlayerPrefs.SetFloat(ClaveMusica, Mathf.Clamp01(value)); Instancia?.AplicarVolumenMusica(); }
    }

    public static float VolumenEfectos
    {
        get => PlayerPrefs.GetFloat(ClaveEfectos, 0.8f);
        set => PlayerPrefs.SetFloat(ClaveEfectos, Mathf.Clamp01(value));
    }

    static GestorAudio Instancia
    {
        get
        {
            if (instancia == null && !cerrando && Application.isPlaying)
            {
                var go = new GameObject("GestorAudio");
                DontDestroyOnLoad(go);
                instancia = go.AddComponent<GestorAudio>();
            }
            // Un objeto destruido no es null para C# ("?." lo dejaría pasar): se devuelve null de verdad
            return instancia != null ? instancia : null;
        }
    }

    /// <summary>
    /// El proyecto tiene Enter Play Mode Options sin recarga de dominio: los static sobreviven entre un Play y otro.
    /// Sin esto, "cerrando" seguía en true desde el Play anterior y se usaba el gestor ya destruido.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ReiniciarEstaticos()
    {
        instancia = null;
        cerrando = false;
    }

    void Awake()
    {
        biblioteca = Resources.Load<BibliotecaAudio>("BibliotecaAudio");
        if (biblioteca == null) Debug.LogWarning("GestorAudio: no existe Resources/BibliotecaAudio; el juego correrá sin sonido.");

        fuenteMusica = CrearFuente(true);
        fuenteMusicaB = CrearFuente(true);
        fuenteEfectos = CrearFuente(false);
    }

    void OnApplicationQuit() { cerrando = true; }

    AudioSource CrearFuente(bool loop)
    {
        var f = gameObject.AddComponent<AudioSource>();
        f.playOnAwake = false;
        f.loop = loop;
        f.ignoreListenerPause = true;
        return f;
    }

    // ------------------------------------------------------------------ Efectos
    public static void Reproducir(Sfx id, float variacionTono = 0.05f) => Instancia?.ReproducirInterno(id, variacionTono);

    void ReproducirInterno(Sfx id, float variacionTono)
    {
        var e = biblioteca != null ? biblioteca.Buscar(id) : null;
        if (e == null || e.clips == null || e.clips.Length == 0) return;

        // Evita que el mismo sonido se apile en el mismo instante (ej. varios impactos juntos)
        int i = (int)id;
        if (Time.unscaledTime - ultimoUso[i] < 0.03f) return;
        ultimoUso[i] = Time.unscaledTime;

        AudioClip clip = e.clips[Random.Range(0, e.clips.Length)];
        fuenteEfectos.pitch = 1f + Random.Range(-variacionTono, variacionTono);
        fuenteEfectos.PlayOneShot(clip, e.volumen * VolumenEfectos);
    }

    // ------------------------------------------------------------------ Música
    public static void CambiarMusica(Musica id, float fundido = 0.8f) => Instancia?.CambiarMusicaInterno(id, fundido);
    public static void DetenerMusica(float fundido = 0.6f) => Instancia?.CambiarMusicaInterno(Musica.Ninguna, fundido);

    /// <summary>Baja la música mientras el juego está en pausa.</summary>
    public static void Atenuar(bool atenuada)
    {
        var g = Instancia;
        if (g == null) return;
        g.atenuacion = atenuada ? 0.35f : 1f;
        g.AplicarVolumenMusica();
    }

    void CambiarMusicaInterno(Musica id, float fundido)
    {
        if (id == musicaActual) return;
        var entrada = id == Musica.Ninguna || biblioteca == null ? null : biblioteca.Buscar(id);
        if (id != Musica.Ninguna && (entrada == null || entrada.clip == null)) return;   // Sin pista asignada: sigue la actual

        musicaActual = id;
        StopAllCoroutines();
        (fuenteMusica, fuenteMusicaB) = (fuenteMusicaB, fuenteMusica);   // B = la que se apaga
        if (entrada != null && entrada.clip != null)
        {
            volumenBaseMusica = entrada.volumen;
            fuenteMusica.clip = entrada.clip;
            fuenteMusica.volume = 0f;
            fuenteMusica.Play();
        }
        StartCoroutine(Fundido(fundido));
    }

    IEnumerator Fundido(float duracion)
    {
        float inicioB = fuenteMusicaB.volume;
        for (float t = 0f; t < duracion; t += Time.unscaledDeltaTime)
        {
            float k = t / duracion;
            fuenteMusicaB.volume = inicioB * (1f - k);
            if (fuenteMusica.isPlaying) fuenteMusica.volume = ObjetivoMusica() * k;
            yield return null;
        }
        fuenteMusicaB.Stop();
        AplicarVolumenMusica();
    }

    float ObjetivoMusica() => volumenBaseMusica * VolumenMusica * atenuacion;

    void AplicarVolumenMusica()
    {
        if (fuenteMusica != null && fuenteMusica.isPlaying) fuenteMusica.volume = ObjetivoMusica();
    }
}
