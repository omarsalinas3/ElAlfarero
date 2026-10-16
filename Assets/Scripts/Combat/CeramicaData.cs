using UnityEngine;

/// <summary>
/// Estadísticas de un tipo de cerámica (munición).
/// Se crea desde el menú: clic derecho en Project > Create > Alfarero > Ceramica.
/// Así podemos balancear Talavera / Barro Negro / Barro Vidriado sin tocar código.
/// (La durabilidad del escudo de maceta se agrega en el Sprint 3.)
/// </summary>
[CreateAssetMenu(fileName = "Ceramica_", menuName = "Alfarero/Ceramica")]
public class CeramicaData : ScriptableObject
{
    public enum TipoCeramica { Talavera, BarroNegro, BarroVidriado }

    [Header("Identidad")]
    public string nombre = "Talavera";
    public TipoCeramica tipo = TipoCeramica.Talavera;
    public Sprite icono;                       // Para el HUD (Sprint 3)

    [Header("Proyectil")]
    public Projectile prefabProyectil;         // Prefab con Projectile + Rigidbody2D + Collider2D (trigger)
    public int dano = 10;
    public float velocidadProyectil = 18f;
    public float tiempoVida = 1.5f;            // Segundos antes de autodestruirse (alcance)
    public LayerMask capasImpacto;             // Qué capas detienen / dañan al proyectil (Ground + Enemy)
    public SpriteAnimadoTemporal prefabImpacto; // Estallido al golpear (opcional)

    [Header("Disparo")]
    public float disparosPorSegundo = 8f;      // Talavera: ráfagas rápidas
    public int proyectilesPorDisparo = 1;      // Barro Vidriado: varios fragmentos
    public float dispersionGrados = 2f;        // Desvío aleatorio máximo (± grados)
}
