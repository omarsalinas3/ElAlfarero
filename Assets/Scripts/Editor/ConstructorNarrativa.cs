using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static ConfigurarSprint1;

/// <summary>
/// Narrativa (#35): el cuadro de diálogo con retrato del HUD, los diálogos al empezar cada nivel (Don Toribio en
/// Dolores, el Charro Negro y Carmen en Pozos) y la cinemática de viñetas entre el Nivel 1 y el viaje a Pozos, que
/// reutiliza SecuenciaIntro.
/// </summary>
public static partial class ConstructorJuego
{
    const string RutaTransicion = "Assets/Scenes/Transicion_Pozos.unity";

    const string Toribio = "DON TORIBIO", Carmen = "CARMEN", Charro = "EL CHARRO NEGRO";

    static readonly (string quien, string texto)[] DialogoDolores =
    {
        (Toribio, "¡Mateo, muchacho! El Charro Negro se llevó a Carmen. Lo vi cruzar la plaza rumbo a la parroquia."),
        (Toribio, "Sus bandidos tomaron las calles y hasta los mineros muertos se levantaron de la mina."),
        (Toribio, "Llévate mis piezas de Talavera consagrada: rompen cualquier hechizo. ¡Corre a la parroquia y no te detengas!"),
    };

    static readonly (string quien, string texto)[] DialogoPozos =
    {
        (Charro, "¿Liberaste a mi alebrije, alfarero? Bienvenido a Mineral de Pozos... tu tumba."),
        (Carmen, "¡Mateo! ¡No le creas! ¡Sigue las huellas de fuego!"),
        (Charro, "Mi Jinete te espera en la capilla. Él ya no tiene cabeza que perder... tú sí."),
    };

    static readonly (string img, string txt)[] VinetasTransicion =
    {
        ("transicion_01", "Libre del hechizo, el Alebrije se inclinó ante Mateo. La Talavera consagrada había roto la magia del Charro."),
        ("transicion_02", "Pero de Carmen solo quedaba su rebozo. El Charro Negro la arrastraba por el desierto, rumbo al pueblo fantasma de Mineral de Pozos."),
        ("transicion_03", "Mateo montó al Alebrije y siguió las huellas de fuego. Las leyendas del Bajío lo estaban esperando."),
    };

    static Sprite Retrato(string quien) => quien switch
    {
        Toribio => Spr("UI/retrato_don_toribio.png"),
        Carmen => Spr("UI/retrato_carmen.png"),
        _ => Spr("UI/retrato_charro.png"),
    };

    /// <summary>Cuadro de diálogo arriba, debajo del objetivo: abajo lo taparían Mateo y los controles táctiles.</summary>
    static void CrearCuadroDialogo(Transform canvas)
    {
        var arriba = new Vector2(0.5f, 1f);
        var caja = Hijo(canvas, "Dialogo", arriba, arriba, new Vector2(0.5f, 1f), new Vector2(0f, -205f), new Vector2(1200f, 190f));
        var grupo = caja.gameObject.AddComponent<CanvasGroup>();
        grupo.alpha = 0f;
        var fondo = caja.gameObject.AddComponent<Image>();
        fondo.sprite = sprPanel;
        fondo.type = Image.Type.Sliced;
        fondo.pixelsPerUnitMultiplier = 0.5f;
        fondo.raycastTarget = false;

        Image marco = Imagen(caja, "MarcoRetrato", sprPanel, new Vector2(-505f, 0f), new Vector2(170f, 170f));
        marco.type = Image.Type.Sliced;
        marco.preserveAspect = false;
        marco.color = new Color(0.25f, 0.12f, 0.06f);
        Image retrato = Imagen(caja, "Retrato", null, new Vector2(-505f, 0f), new Vector2(150f, 150f));
        Text nombre = Texto(caja, "Nombre", "", 22, Dorado, TextAnchor.MiddleLeft, new Vector2(35f, 60f), new Vector2(900f, 36f));
        Text texto = Texto(caja, "Texto", "", 20, Crema, TextAnchor.UpperLeft, new Vector2(35f, -20f), new Vector2(900f, 110f));
        texto.lineSpacing = 1.2f;
        Text ayuda = Texto(caja, "Ayuda", "ENTER SIGUE   RETROCESO SALTA", 12, new Color(1f, 1f, 1f, 0.6f), TextAnchor.MiddleRight,
                           new Vector2(330f, -78f), new Vector2(500f, 24f));
        SegunDispositivo(ayuda, ayuda.text, "SELECT SIGUE   MANTEN SELECT SALTA", " ");

        var cuadro = caja.gameObject.AddComponent<CuadroDialogo>();
        var so = new SerializedObject(cuadro);
        so.FindProperty("grupo").objectReferenceValue = grupo;
        so.FindProperty("retrato").objectReferenceValue = retrato;
        so.FindProperty("nombre").objectReferenceValue = nombre;
        so.FindProperty("texto").objectReferenceValue = texto;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>Diálogo que arranca solo al empezar el nivel.</summary>
    static void CrearDialogoInicio((string quien, string texto)[] lineas, float espera)
    {
        var d = new GameObject("DialogoInicio").AddComponent<DialogoInicio>();
        var so = new SerializedObject(d);
        so.FindProperty("espera").floatValue = espera;
        var lista = so.FindProperty("lineas");
        lista.arraySize = lineas.Length;
        for (int i = 0; i < lineas.Length; i++)
        {
            var e = lista.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("personaje").stringValue = lineas[i].quien;
            e.FindPropertyRelative("retrato").objectReferenceValue = Retrato(lineas[i].quien);
            e.FindPropertyRelative("texto").stringValue = lineas[i].texto;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ================================================================== Viñetas (intro y transición)
    /// <summary>Escena de viñetas con texto letra por letra (SecuenciaIntro): la intro y la transición al Nivel 2.</summary>
    static void CrearEscenaVinetas(string ruta, string nombre, (string img, string txt)[] vinetas, string escenaSiguiente)
    {
        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CrearCamara();
        CrearEventSystem();
        Canvas canvas = CrearCanvas(nombre, 0);
        Transform c = canvas.transform;
        Estirar(c, "Negro").gameObject.AddComponent<Image>().color = Color.black;

        Image imagen = Imagen(c, "Ilustracion", Spr($"Intro/{vinetas[0].img}.png"), new Vector2(0f, 110f), new Vector2(1500f, 844f));
        imagen.preserveAspect = true;
        var grupo = imagen.gameObject.AddComponent<CanvasGroup>();
        grupo.alpha = 0f;

        var caja = Centro(c, "CajaTexto", new Vector2(0f, -400f), new Vector2(1640f, 230f));
        var imgCaja = caja.gameObject.AddComponent<Image>();
        imgCaja.sprite = sprPanel;
        imgCaja.type = Image.Type.Sliced;
        imgCaja.pixelsPerUnitMultiplier = 0.5f;
        Text texto = Texto(caja, "Texto", "", 26, Crema, TextAnchor.UpperLeft, new Vector2(0f, -5f), new Vector2(1520f, 170f));
        Text ayuda = Texto(c, "Ayuda", "ENTER SIGUIENTE   ESC SALTAR", 16, new Color(1f, 1f, 1f, 0.6f), TextAnchor.LowerRight,
                           new Vector2(-280f, 18f), new Vector2(520f, 30f), new Vector2(1f, 0f));
        SegunDispositivo(ayuda, ayuda.text, "A SIGUIENTE   START SALTAR", "TOCA PARA SEGUIR");

        var secuencia = canvas.gameObject.AddComponent<SecuenciaIntro>();
        var so = new SerializedObject(secuencia);
        var lista = so.FindProperty("vinetas");
        lista.arraySize = vinetas.Length;
        for (int i = 0; i < vinetas.Length; i++)
        {
            var e = lista.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("imagen").objectReferenceValue = Spr($"Intro/{vinetas[i].img}.png");
            e.FindPropertyRelative("texto").stringValue = vinetas[i].txt;
        }
        so.FindProperty("imagen").objectReferenceValue = imagen;
        so.FindProperty("grupoImagen").objectReferenceValue = grupo;
        so.FindProperty("texto").objectReferenceValue = texto;
        so.FindProperty("escenaSiguiente").stringValue = escenaSiguiente;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(escena, ruta);
    }

    /// <summary>Del Nivel 1 al viaje montado: el alebrije liberado, el Charro se lleva a Carmen y Mateo la sigue.</summary>
    static void CrearEscenaTransicion() =>
        CrearEscenaVinetas(RutaTransicion, "Transicion", VinetasTransicion, Escenas.Viaje);

    // ================================================================== Entre niveles (#92)
    const string RutaTransicionPanteon = "Assets/Scenes/Transicion_Panteon.unity";
    const string RutaTransicionTuneles = "Assets/Scenes/Transicion_Tuneles.unity";

    static readonly (string img, string txt)[] VinetasPanteon =
    {
        ("panteon_01", "El Jinete sin Cabeza cayó y su caballo se deshizo en humo. En el patio de la capilla quedó un listón de Carmen."),
        ("panteon_02", "Las huellas de fuego bajaban del pueblo fantasma hacia el río, rumbo a un panteón que nadie se atreve a cruzar de noche."),
        ("panteon_03", "Entre la niebla se oía un llanto: «¡Ay, mis hijos!». Mateo apretó su Talavera y entró al panteón."),
    };

    // Al liberar a La Llorona baja el río y aparece la entrada a los túneles
    static readonly (string img, string txt)[] VinetasTuneles =
    {
        ("tuneles_01", "Libre del hechizo, La Llorona dejó de llorar. Por primera vez en siglos, el río guardó silencio."),
        ("tuneles_02", "Las aguas bajaron y en la orilla apareció una boca de piedra: la entrada a los túneles de Guanajuato."),
        ("tuneles_03", "Desde lo hondo llegaba una risa... y la voz de Carmen. Mateo bajó a los túneles: el amanecer ya estaba cerca."),
    };

    static void CrearEscenasTransicionNiveles()
    {
        CrearEscenaVinetas(RutaTransicionPanteon, "TransicionPanteon", VinetasPanteon, Escenas.Nivel3);
        CrearEscenaVinetas(RutaTransicionTuneles, "TransicionTuneles", VinetasTuneles, Escenas.Nivel4);
    }
}
