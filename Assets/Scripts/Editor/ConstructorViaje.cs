using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static ConfigurarSprint1;

/// <summary>
/// Escena de viaje entre el Nivel 1 y el 2 (#57): Mateo sale de Dolores montado en el alebrije siguiendo las huellas de
/// fuego del caballo del Charro, con el letrero "RUMBO A MINERAL DE POZOS". Va de las calles de Dolores a la cañada de
/// Pozos (tramos de fondo que ya existen) y al salir de cuadro carga el Nivel 2. La lleva el botón "¡RUMBO A POZOS!" de
/// la victoria del Nivel 1. Usa el Animator "Montado" que arma ConstructorMontura con el Nivel 1.
/// </summary>
public static partial class ConstructorJuego
{
    const string RutaViaje = "Assets/Scenes/Viaje_Pozos.unity";

    // Calles de Dolores → parroquia → cañada rumbo a Pozos (mismos traslapes que en sus niveles)
    static readonly (string archivo, int solapePx)[] SegmentosViaje =
    {
        ("lvl1_seg04.png", 0),
        ("lvl1_seg05.png", 828),
        ("lvl2_seg04.png", 596),
    };
    const float DegradadoPrimerTramo = 384f / 128f;    // El 1.º tramo trae su orilla izquierda transparente: no se muestra
    const float MitadAnchoCamara = 6f * 16f / 9f;      // Cámara de 12 de alto en 16:9

    static void CrearEscenaViaje()
    {
        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CrearFondos(SegmentosViaje, out float izquierda, out float derecha);
        CrearLuzGlobalSiHayURP();
        float camaraMin = izquierda + DegradadoPrimerTramo + MitadAnchoCamara;
        float camaraMax = derecha - MitadAnchoCamara;

        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        camGo.transform.position = new Vector3(camaraMin, 0f, -10f);
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        camGo.AddComponent<AudioListener>();

        // Mateo montado: entra por la izquierda y sale por la derecha. El arte mira a la izquierda (escala X negativa)
        float xInicio = camaraMin - MitadAnchoCamara - 1.5f;
        float xFin = camaraMax + MitadAnchoCamara + 2f;
        var montado = new GameObject("Mateo_Montado");
        montado.transform.position = new Vector3(xInicio, YSuelo, 0f);
        montado.transform.localScale = new Vector3(-EscalaMontura, EscalaMontura, 1f);
        var sr = montado.AddComponent<SpriteRenderer>();
        sr.sprite = Spr("Allies/montado_galope_00.png");
        sr.sortingOrder = 5;
        var anim = montado.AddComponent<Animator>();
        anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Art/Animations/Montado/Montado.controller");

        // Huellas de fuego del caballo del Charro, marcando el camino
        Sprite[] fuego = Cuadros("Effects/huella_fuego", 3);
        var padreHuellas = new GameObject("Huellas_Fuego").transform;
        var huellas = new System.Collections.Generic.List<SpriteRenderer>();
        for (float x = camaraMin - MitadAnchoCamara + 2f; x < xFin; x += 1.8f)
        {
            var h = new GameObject("Huella").AddComponent<SpriteRenderer>();
            h.transform.SetParent(padreHuellas, false);
            h.transform.position = new Vector3(x, YSuelo + 0.02f, 0f);
            h.sprite = fuego[huellas.Count % fuego.Length];
            h.sortingOrder = 3;
            huellas.Add(h);
        }

        // Letrero arriba y ayuda para saltar
        Canvas canvas = CrearCanvas("Viaje", 0);
        RectTransform caja = Centro(canvas.transform, "Letrero", new Vector2(0f, 340f), new Vector2(1600f, 200f));
        var grupo = caja.gameObject.AddComponent<CanvasGroup>();
        Texto(caja, "Titulo", "RUMBO A MINERAL DE POZOS", 44, Dorado, TextAnchor.MiddleCenter, new Vector2(0f, 35f), new Vector2(1600f, 70f));
        Texto(caja, "Detalle", "SIGUE LAS HUELLAS DE FUEGO DEL CHARRO", 22, Crema, TextAnchor.MiddleCenter, new Vector2(0f, -40f), new Vector2(1600f, 40f));
        Text ayuda = Texto(canvas.transform, "Ayuda", "ENTER SALTAR", 16, new Color(1f, 1f, 1f, 0.6f), TextAnchor.LowerRight,
                           new Vector2(-180f, 18f), new Vector2(320f, 30f), new Vector2(1f, 0f));
        SegunDispositivo(ayuda, ayuda.text, "A SALTAR", "TOCA PARA SALTAR");

        var viaje = new GameObject("Viaje").AddComponent<ViajeMontado>();
        var so = new SerializedObject(viaje);
        so.FindProperty("montado").objectReferenceValue = montado.transform;
        so.FindProperty("animator").objectReferenceValue = anim;
        so.FindProperty("camara").objectReferenceValue = camGo.transform;
        so.FindProperty("velocidad").floatValue = 8f;
        so.FindProperty("xFin").floatValue = xFin;
        so.FindProperty("camaraMin").floatValue = camaraMin;
        so.FindProperty("camaraMax").floatValue = camaraMax;
        so.FindProperty("letrero").objectReferenceValue = grupo;
        var lista = so.FindProperty("huellas");
        lista.arraySize = huellas.Count;
        for (int i = 0; i < huellas.Count; i++) lista.GetArrayElementAtIndex(i).objectReferenceValue = huellas[i];
        var cuadros = so.FindProperty("cuadrosFuego");
        cuadros.arraySize = fuego.Length;
        for (int i = 0; i < fuego.Length; i++) cuadros.GetArrayElementAtIndex(i).objectReferenceValue = fuego[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(escena, RutaViaje);
    }
}
