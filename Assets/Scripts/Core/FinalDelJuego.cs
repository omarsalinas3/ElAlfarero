using UnityEngine;

/// <summary>
/// Final del juego (#91): va en la escena de viñetas del rescate de Carmen al amanecer. Al empezar deja la partida
/// marcada como terminada (aunque el jugador se salte las viñetas) y pide que el menú, al que llevan las viñetas,
/// abra directo en los créditos.
/// </summary>
public class FinalDelJuego : MonoBehaviour
{
    [SerializeField] Musica musica = Musica.Intro;

    void Start()
    {
        GestorGuardado.MarcarJuegoTerminado();
        MenuPrincipal.PedirCreditos();
        GestorAudio.CambiarMusica(musica, 1f);
    }
}
