using UnityEngine;

/// <summary>
/// Nahual de la estampida (#53): cruza la pantalla a toda velocidad en línea recta y lastima al tocar (DanoPorContacto).
/// No se puede matar: es un espíritu del Charro, las cerámicas lo atraviesan. Se esquiva saltando o evadiendo.
/// Sale de Reciclaje y vuelve ahí al salir de la pantalla. El arte mira a la derecha.
/// </summary>
public class Nahual : MonoBehaviour
{
    [SerializeField] SpriteRenderer sprite;
    [SerializeField] Sprite[] cuadros;
    [SerializeField] float cuadrosPorSegundo = 16f;
    [SerializeField] float distanciaMaxima = 40f;     // Por si nadie lo detiene: se guarda tras recorrer esto

    float velocidad, recorrido, tiempo;
    int direccion = -1;

    public int Direccion => direccion;
    public float Velocidad => velocidad;

    /// <summary>Lo echa a correr hacia la dirección dada (1 derecha, -1 izquierda).</summary>
    public void Correr(int dir, float rapidez)
    {
        direccion = dir >= 0 ? 1 : -1;
        velocidad = rapidez;
        Vector3 s = transform.localScale;
        transform.localScale = new Vector3(Mathf.Abs(s.x) * direccion, s.y, s.z);
    }

    void OnEnable()
    {
        recorrido = 0f;
        tiempo = Random.value;                         // Que la manada no corra al mismo paso
    }

    void Update()
    {
        // Durante la pausa o una cinemática se quedan quietos como los demás enemigos
        var gestor = GestorNivel.Actual;
        if (gestor != null && gestor.EstadoActual != GestorNivel.Estado.Jugando && gestor.EstadoActual != GestorNivel.Estado.GameOver) return;

        float paso = velocidad * Time.deltaTime;
        transform.position += Vector3.right * (direccion * paso);
        recorrido += paso;

        tiempo += Time.deltaTime;
        if (sprite != null && cuadros != null && cuadros.Length > 0)
            sprite.sprite = cuadros[(int)(tiempo * cuadrosPorSegundo) % cuadros.Length];

        if (recorrido >= distanciaMaxima) Reciclaje.Guardar(gameObject);
    }
}
