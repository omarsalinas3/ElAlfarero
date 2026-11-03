using UnityEngine;

/// <summary>
/// Estadísticas de un tipo de cerámica (arma). Las arma ConstructorJuego (Talavera, Barro Vidriado, Barro Negro y
/// Maceta Escudo); también se pueden crear desde el menú: clic derecho en Project > Create > Alfarero > Ceramica.
/// La Talavera no se acaba; las demás traen munición limitada que se rellena con sus cajas (#33).
/// </summary>
[CreateAssetMenu(fileName = "Ceramica_", menuName = "Alfarero/Ceramica")]
public class CeramicaData : ScriptableObject
{
    // Agregar solo al final: el guardado usa el nombre (ToString) y los assets el número
    public enum TipoCeramica { Talavera, BarroNegro, BarroVidriado, MacetaEscudo }

    [Header("Identidad")]
    public string nombre = "Talavera";
    public TipoCeramica tipo = TipoCeramica.Talavera;
    public Sprite icono;                       // Para el HUD

    [Header("Proyectil")]
    public Projectile prefabProyectil;         // Prefab con Projectile + Rigidbody2D + Collider2D (trigger)
    public int dano = 10;
    public float velocidadProyectil = 18f;
    public float tiempoVida = 1.5f;            // Segundos antes de autodestruirse (alcance); con explosión, la mecha
    public LayerMask capasImpacto;             // Qué capas detienen / dañan al proyectil (Ground + Enemy)
    public SpriteAnimadoTemporal prefabImpacto; // Estallido al golpear (opcional)

    [Header("Disparo")]
    public float disparosPorSegundo = 8f;      // Talavera: ráfagas rápidas
    public int proyectilesPorDisparo = 1;      // Barro Vidriado: varios a la vez
    public float dispersionGrados = 2f;        // Desvío aleatorio máximo (± grados)
    public float aperturaAbanico = 0f;         // > 0: los proyectiles salen repartidos parejo en este ángulo
    public Sfx sonidoLanzar = Sfx.Lanzar;

    [Header("Arco y explosión (Barro Negro)")]
    public float gravedad = 0f;                // > 0: vuela en arco como granada
    public float elevacionGrados = 0f;         // Se lanza un poco hacia arriba para que el arco llegue lejos
    public float radioExplosion = 0f;          // > 0: al chocar (o al acabarse la mecha) daña todo en este radio
    public int danoExplosion = 0;
    public SpriteAnimadoTemporal prefabExplosion;

    [Header("Munición")]
    public int municionMaxima = 0;             // 0 = infinita (Talavera)
    public int municionPorCaja = 10;           // Lo que da su caja; también con lo que se empieza cada nivel si ya se tiene

    public bool Infinita => municionMaxima <= 0;
    public bool EsEscudo => tipo == TipoCeramica.MacetaEscudo;
    /// <summary>Nombre con el que se guarda en armasDesbloqueadas.</summary>
    public string Clave => tipo.ToString();
}
