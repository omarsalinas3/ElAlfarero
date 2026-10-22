using UnityEngine;

/// <summary>
/// Lo común a todo enemigo: lastima al tocarlo (DanoPorContacto) y al morir suena, deja un efecto y avisa al
/// GestorNivel para las estadísticas. El movimiento va aparte (IAEsqueleto); sin él se queda quieto (dummy del Sandbox).
/// </summary>
[RequireComponent(typeof(Health))]
public class EnemigoBasico : MonoBehaviour
{
    [SerializeField] SpriteAnimadoTemporal efectoMuerte;
    [SerializeField] Sfx sonidoMuerte = Sfx.MuerteEsqueleto;

    Health salud;

    void Awake() { salud = GetComponent<Health>(); }
    void OnEnable() { salud.OnMuerte += AlMorir; salud.OnDano += AlRecibirDano; }
    void OnDisable() { salud.OnMuerte -= AlMorir; salud.OnDano -= AlRecibirDano; }

    void AlRecibirDano(int cantidad)
    {
        if (!salud.EstaMuerto) GestorAudio.Reproducir(Sfx.Golpe);
    }

    void AlMorir()
    {
        GestorAudio.Reproducir(sonidoMuerte);
        if (efectoMuerte != null)
        {
            var fx = Instantiate(efectoMuerte, transform.position + Vector3.up * 1f, Quaternion.identity);
            fx.transform.localScale = Vector3.one * 1.6f;
        }
        GestorNivel.Actual?.RegistrarEnemigoDerrotado();
    }
}
