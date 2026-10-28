using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Reciclaje (object pooling) de lo que se crea y destruye a cada rato: cerámicas, balas, impactos, efectos y monedas.
/// En lugar de Instantiate/Destroy se apagan y se guardan para volver a usarlos, así no se acumula basura en memoria
/// y no hay tirones cuando el recolector la limpia (se nota sobre todo en laptops lentas y en WebGL).
///
/// Uso: Reciclaje.Sacar(prefab, pos, rot) en lugar de Instantiate, y Reciclaje.Guardar(go) o Reciclaje.Guardar(go, seg)
/// en lugar de Destroy. Guardar lo que no salió de aquí (p. ej. un recogible puesto en la escena) lo destruye como antes.
/// Quien reutilice un objeto debe dejarlo como nuevo en OnEnable (o al inicializarlo).
/// </summary>
public static class Reciclaje
{
    static readonly Dictionary<GameObject, Stack<GameObject>> libres = new Dictionary<GameObject, Stack<GameObject>>();

    /// <summary>Objetos creados y reutilizados desde que arrancó la escena (para las pruebas de rendimiento).</summary>
    public static int Creados { get; private set; }
    public static int Reutilizados { get; private set; }

    // Domain reload desactivado: lo estático sobrevive entre un Play y otro
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ReiniciarEstaticos()
    {
        Vaciar();
        SceneManager.sceneUnloaded -= AlDescargarEscena;
        SceneManager.sceneUnloaded += AlDescargarEscena;
    }

    // Lo guardado vive en la escena y muere con ella
    static void AlDescargarEscena(Scene _) => Vaciar();

    static void Vaciar()
    {
        libres.Clear();
        Creados = Reutilizados = 0;
    }

    public static T Sacar<T>(T prefab, Vector3 posicion, Quaternion rotacion) where T : Component =>
        Sacar(prefab.gameObject, posicion, rotacion).GetComponent<T>();

    public static GameObject Sacar(GameObject prefab, Vector3 posicion, Quaternion rotacion)
    {
        if (libres.TryGetValue(prefab, out var pila))
        {
            while (pila.Count > 0)
            {
                GameObject go = pila.Pop();
                if (go == null) continue;                      // Lo destruyó otra cosa mientras estaba guardado
                go.transform.SetPositionAndRotation(posicion, rotacion);
                go.transform.localScale = prefab.transform.localScale;   // Quien lo usó antes pudo cambiarla
                go.SetActive(true);
                Reutilizados++;
                return go;
            }
        }

        GameObject nuevo = Object.Instantiate(prefab, posicion, rotacion);
        nuevo.AddComponent<Reciclable>().Origen = prefab;
        Creados++;
        return nuevo;
    }

    public static void Guardar(GameObject go)
    {
        if (go == null || !go.activeSelf) return;            // Ya guardado (p. ej. dos choques en el mismo cuadro)
        var r = go.GetComponent<Reciclable>();
        if (r == null || r.Origen == null) { Object.Destroy(go); return; }

        go.SetActive(false);
        if (!libres.TryGetValue(r.Origen, out var pila)) libres[r.Origen] = pila = new Stack<GameObject>();
        pila.Push(go);
    }

    /// <summary>Lo guarda dentro de unos segundos (como Destroy(go, segundos)). Si se guarda antes, se cancela.</summary>
    public static void Guardar(GameObject go, float segundos)
    {
        var r = go.GetComponent<Reciclable>();
        if (r == null) { Object.Destroy(go, segundos); return; }
        r.GuardarEn(segundos);
    }
}

/// <summary>Marca de un objeto que salió de Reciclaje: de qué prefab viene y si tiene un guardado pendiente.</summary>
public class Reciclable : MonoBehaviour
{
    public GameObject Origen { get; set; }

    float guardarEn = -1f;

    public void GuardarEn(float segundos) => guardarEn = Time.time + segundos;

    void OnDisable() => guardarEn = -1f;

    void Update()
    {
        if (guardarEn > 0f && Time.time >= guardarEn)
        {
            guardarEn = -1f;
            Reciclaje.Guardar(gameObject);
        }
    }
}
