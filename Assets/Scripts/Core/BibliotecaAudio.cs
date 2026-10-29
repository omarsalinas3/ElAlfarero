using System;
using UnityEngine;

/// <summary>Efectos de sonido del juego. Para agregar uno: añadirlo aquí y asignarle clips en BibliotecaAudio.</summary>
public enum Sfx
{
    Salto, Evadir, Lanzar, ImpactoCeramica, Golpe, DanoJugador, MuerteEsqueleto,
    RomperHuacal, RomperHorno, RecogerGuacamaya, Moneda, RecogerCaguama, BeberCaguama,
    MenuMover, MenuAceptar, Pausa, JingleGameOver, JingleVictoria, JingleMision,
    Disparo, Escopetazo, Machetazo, RugidoAlebrije,
    AullidoNahual, Estampida
}

public enum Musica { Ninguna, Menu, Intro, Nivel1, Nivel2 }

/// <summary>
/// Tabla de sonidos del juego (un solo asset en Resources/BibliotecaAudio). El código pide sonidos por nombre (Sfx / Musica)
/// y aquí se decide qué archivo suena, así cambiar un sonido no requiere tocar scripts.
/// </summary>
[CreateAssetMenu(fileName = "BibliotecaAudio", menuName = "Alfarero/Biblioteca de audio")]
public class BibliotecaAudio : ScriptableObject
{
    [Serializable]
    public class EntradaSfx
    {
        public Sfx id;
        public AudioClip[] clips;                 // Si hay varios se elige uno al azar (variedad)
        [Range(0f, 1f)] public float volumen = 1f;
    }

    [Serializable]
    public class EntradaMusica
    {
        public Musica id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volumen = 0.6f;
    }

    public EntradaSfx[] efectos = Array.Empty<EntradaSfx>();
    public EntradaMusica[] musicas = Array.Empty<EntradaMusica>();

    // Tablas indexadas por id: se arman una vez y luego cada búsqueda es directa (sin crear basura cada disparo)
    EntradaSfx[] porSfx;
    EntradaMusica[] porMusica;

    public EntradaSfx Buscar(Sfx id)
    {
        if (porSfx == null)
        {
            porSfx = new EntradaSfx[Enum.GetValues(typeof(Sfx)).Length];
            foreach (var e in efectos) porSfx[(int)e.id] = e;
        }
        return porSfx[(int)id];
    }

    public EntradaMusica Buscar(Musica id)
    {
        if (porMusica == null)
        {
            porMusica = new EntradaMusica[Enum.GetValues(typeof(Musica)).Length];
            foreach (var e in musicas) porMusica[(int)e.id] = e;
        }
        return porMusica[(int)id];
    }
}
