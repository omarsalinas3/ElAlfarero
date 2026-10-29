using System.Linq;
using UnityEditor;
using UnityEngine;
using static ConfigurarSprint1;

/// <summary>
/// Estampida de nahuales (#53) en la cañada del Nivel 2: prefab del nahual y la zona del evento.
/// Cuadros recortados de Art/Sprites/Referencias/ref_nahual.png (8 de carrera, ya con transparencia); el arte mira a la derecha.
/// </summary>
public static partial class ConstructorJuego
{
    const string RutaPrefabNahual = "Assets/Prefabs/Enemies/Nahual.prefab";

    static Nahual CrearPrefabNahual()
    {
        Sprite[] cuadros = Enumerable.Range(0, 8).Select(i => Spr($"Enemies/nahual_correr_{i:00}.png")).ToArray();

        var go = new GameObject("Nahual") { layer = CapaProyectilEnemigo };   // Como las balas: solo toca a Mateo, las cerámicas lo atraviesan
        var visual = new GameObject("Visual") { layer = CapaProyectilEnemigo };
        visual.transform.SetParent(go.transform, false);
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = cuadros[0];
        sr.sortingOrder = 7;                                   // Delante de Mateo: pasan encima de él

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.useFullKinematicContacts = true;
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        Vector2 tam = sr.sprite.bounds.size;
        col.size = new Vector2(tam.x * 0.6f, tam.y * 0.55f);   // Cuerpo y cabeza, sin la cola ni las patas estiradas
        col.offset = new Vector2(tam.x * 0.08f, tam.y * 0.4f);

        var dano = go.AddComponent<DanoPorContacto>();
        var soD = new SerializedObject(dano);
        soD.FindProperty("dano").intValue = 15;
        soD.FindProperty("empuje").floatValue = 9f;
        soD.FindProperty("capasObjetivo").FindPropertyRelative("m_Bits").intValue = 1 << CapaPlayer;
        soD.ApplyModifiedPropertiesWithoutUndo();

        var nahual = go.AddComponent<Nahual>();
        var so = new SerializedObject(nahual);
        so.FindProperty("sprite").objectReferenceValue = sr;
        var lista = so.FindProperty("cuadros");
        lista.arraySize = cuadros.Length;
        for (int i = 0; i < cuadros.Length; i++) lista.GetArrayElementAtIndex(i).objectReferenceValue = cuadros[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        return Guardar(go, RutaPrefabNahual).GetComponent<Nahual>();
    }

    /// <summary>
    /// Zona de la estampida centrada en la cañada. Manadas cada vez más difíciles: primero uno por uno (se salta),
    /// luego por los dos lados y al final una fila pegada que obliga a evadir.
    /// </summary>
    static Estampida CrearEstampida(float centro)
    {
        Nahual prefab = CrearPrefabNahual();
        var go = new GameObject("Zona_Estampida");
        go.transform.position = new Vector3(centro, YSuelo + 2f, 0f);
        go.AddComponent<BoxCollider2D>().size = new Vector2(1f, 8f);
        var estampida = go.AddComponent<Estampida>();

        // (por la derecha, retraso, velocidad)
        var manadas = new[]
        {
            new[] { (true, 0f, 15f), (true, 1.1f, 15f), (true, 1.1f, 17f) },
            new[] { (true, 0f, 17f), (false, 0.9f, 17f), (true, 0.9f, 19f), (false, 0.6f, 19f) },
            new[] { (true, 0f, 21f), (true, 0.22f, 21f), (true, 0.22f, 21f), (false, 1.2f, 20f), (true, 0.5f, 22f) },
        };

        var so = new SerializedObject(estampida);
        so.FindProperty("prefabNahual").objectReferenceValue = prefab;
        so.FindProperty("ySuelo").floatValue = YSuelo;
        var lista = so.FindProperty("manadas");
        lista.arraySize = manadas.Length;
        for (int i = 0; i < manadas.Length; i++)
        {
            var nahuales = lista.GetArrayElementAtIndex(i).FindPropertyRelative("nahuales");
            nahuales.arraySize = manadas[i].Length;
            for (int j = 0; j < manadas[i].Length; j++)
            {
                var e = nahuales.GetArrayElementAtIndex(j);
                e.FindPropertyRelative("porLaDerecha").boolValue = manadas[i][j].Item1;
                e.FindPropertyRelative("retraso").floatValue = manadas[i][j].Item2;
                e.FindPropertyRelative("velocidad").floatValue = manadas[i][j].Item3;
            }
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        return estampida;
    }
}
