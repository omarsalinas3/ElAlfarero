using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static ConfigurarSprint1;

/// <summary>
/// Menú Alfarero > Construir animaciones de enemigos.
/// Genera clips y Animator Controllers de los 4 bandidos (revólver, rifle, escopeta, machete) y del alebrije
/// a partir de los cuadros recortados de las hojas de Flow. Lo llama también ConstructorJuego.
///
/// Bandidos: Apuntar (reposo) / Correr por "Velocidad", "Atacar" y "Golpe" como triggers, "Muerto" como bool.
/// Alebrije: Reposo / Caminar por "Velocidad", "Muerto" como bool; Salto, Aterrizaje, Zarpazo y Rugido se
/// reproducen desde la IA del jefe con Animator.Play (son poses de la pelea y de la entrada desde las torres).
/// Minero esqueleto: Reposo / Caminar por "Velocidad", "Atacar" (golpe con el pico) como trigger, "Muerto" como bool.
/// Bruja de Fuego: Volar siempre; "Lanzar" y "Golpe" como triggers, "Picada" y "Muerto" como bool.
/// </summary>
public static class ConstructorAnimaciones
{
    const string Carpeta = "Assets/Art/Animations/";
    static readonly string[] Armas = { "revolver", "rifle", "escopeta", "machete" };

    [MenuItem("Alfarero/Construir animaciones de enemigos")]
    public static void Construir()
    {
        Directory.CreateDirectory(Carpeta + "Bandidos");
        Directory.CreateDirectory(Carpeta + "Alebrije");
        Directory.CreateDirectory(Carpeta + "Esqueleto");
        Directory.CreateDirectory(Carpeta + "Bruja");
        AssetDatabase.Refresh();

        foreach (string arma in Armas) CrearBandido(arma);
        CrearAlebrije();
        CrearEsqueleto();
        CrearBruja();

        AssetDatabase.SaveAssets();
        Debug.Log("Animaciones de bandidos, alebrije, esqueleto y bruja construidas.");
    }

    static void CrearBruja()
    {
        const string pre = "Enemies/bruja";
        AnimationClip volar = CrearClip("Bruja/Bruja_Volar", Cuadros(pre + "_volar", 4), 8f, true);
        AnimationClip lanzar = CrearClip("Bruja/Bruja_Lanzar", Cuadros(pre + "_lanzar", 4), 8f, false);   // 0.5 s: la bola sale a la mitad
        AnimationClip picada = CrearClip("Bruja/Bruja_Picada", Cuadros(pre + "_picada", 2), 10f, true);
        Sprite golpeCuadro = Cuadros(pre + "_golpe", 1)[0];
        AnimationClip golpe = CrearClip("Bruja/Bruja_Golpe", new[] { golpeCuadro, golpeCuadro }, 8f, false);   // 0.25 s
        AnimationClip morir = CrearClip("Bruja/Bruja_Muerte", Cuadros(pre + "_muerte", 4), 6f, false);

        AnimatorController ctrl = NuevoControlador("Bruja/Bruja.controller");
        ctrl.AddParameter("Lanzar", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Golpe", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Picada", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("Muerto", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
        AnimatorState sVolar = sm.AddState("Volar"); sVolar.motion = volar;
        AnimatorState sLanzar = sm.AddState("Lanzar"); sLanzar.motion = lanzar;
        AnimatorState sPicada = sm.AddState("Picada"); sPicada.motion = picada;
        AnimatorState sGolpe = sm.AddState("Golpe"); sGolpe.motion = golpe;
        AnimatorState sMuerte = sm.AddState("Muerte"); sMuerte.motion = morir;
        sm.defaultState = sVolar;

        TransicionGlobal(sm, sLanzar).AddCondition(AnimatorConditionMode.If, 0, "Lanzar");
        TransicionGlobal(sm, sGolpe).AddCondition(AnimatorConditionMode.If, 0, "Golpe");
        Transicion(sVolar, sPicada).AddCondition(AnimatorConditionMode.If, 0, "Picada");
        Transicion(sPicada, sVolar).AddCondition(AnimatorConditionMode.IfNot, 0, "Picada");
        AlTerminar(sLanzar, sVolar);
        AlTerminar(sGolpe, sVolar);
        MuerteGana(sm, sMuerte);
        EditorUtility.SetDirty(ctrl);
    }

    static void CrearEsqueleto()
    {
        const string pre = "Enemies/esqueleto";
        Sprite[] caminar = Cuadros(pre + "_caminar", 4);

        AnimationClip reposo = CrearClip("Esqueleto/Esqueleto_Reposo", new[] { caminar[0] }, 1f, true);
        AnimationClip andar = CrearClip("Esqueleto/Esqueleto_Caminar", caminar, 7f, true);
        // 4 cuadros en ~0.55 s: levanta el pico mientras se prepara (aviso) y lo baja durante la embestida
        AnimationClip golpe = CrearClip("Esqueleto/Esqueleto_Golpe", Cuadros(pre + "_golpe", 4), 7.5f, false);
        AnimationClip morir = CrearClip("Esqueleto/Esqueleto_Muerte", Cuadros(pre + "_muerte", 4), 7f, false);

        AnimatorController ctrl = NuevoControlador("Esqueleto/Esqueleto.controller");
        ctrl.AddParameter("Velocidad", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Atacar", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Muerto", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
        AnimatorState sReposo = sm.AddState("Reposo"); sReposo.motion = reposo;
        AnimatorState sCaminar = sm.AddState("Caminar"); sCaminar.motion = andar;
        AnimatorState sGolpe = sm.AddState("Golpe"); sGolpe.motion = golpe;
        AnimatorState sMuerte = sm.AddState("Muerte"); sMuerte.motion = morir;
        sm.defaultState = sReposo;

        Transicion(sReposo, sCaminar).AddCondition(AnimatorConditionMode.Greater, 0.1f, "Velocidad");
        Transicion(sCaminar, sReposo).AddCondition(AnimatorConditionMode.Less, 0.1f, "Velocidad");
        TransicionGlobal(sm, sGolpe).AddCondition(AnimatorConditionMode.If, 0, "Atacar");
        AlTerminar(sGolpe, sReposo);
        MuerteGana(sm, sMuerte);
        EditorUtility.SetDirty(ctrl);
    }

    static void CrearBandido(string arma)
    {
        string nombre = "Bandido_" + char.ToUpper(arma[0]) + arma.Substring(1);
        string pre = $"Enemies/bandido_{arma}";
        Sprite[] ataque = Cuadros(pre + "_ataque", 3);

        AnimationClip apuntar = CrearClip($"Bandidos/{nombre}_Apuntar", new[] { ataque[0] }, 1f, true);
        AnimationClip correr = CrearClip($"Bandidos/{nombre}_Correr", Cuadros(pre + "_correr", 4), 10f, true);
        AnimationClip atacar = CrearClip($"Bandidos/{nombre}_Atacar", ataque, 12f, false);
        Sprite golpeCuadro = Cuadros(pre + "_golpe", 1)[0];
        AnimationClip golpe = CrearClip($"Bandidos/{nombre}_Golpe", new[] { golpeCuadro, golpeCuadro }, 4f, false);   // 0.25 s
        AnimationClip morir = CrearClip($"Bandidos/{nombre}_Muerte", Cuadros(pre + "_muerte", 4), 8f, false);

        AnimatorController ctrl = NuevoControlador($"Bandidos/{nombre}.controller");
        ctrl.AddParameter("Velocidad", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Atacar", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Golpe", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Muerto", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
        AnimatorState sApuntar = sm.AddState("Apuntar"); sApuntar.motion = apuntar;
        AnimatorState sCorrer = sm.AddState("Correr"); sCorrer.motion = correr;
        AnimatorState sAtacar = sm.AddState("Atacar"); sAtacar.motion = atacar;
        AnimatorState sGolpe = sm.AddState("Golpe"); sGolpe.motion = golpe;
        AnimatorState sMuerte = sm.AddState("Muerte"); sMuerte.motion = morir;
        sm.defaultState = sApuntar;

        Transicion(sApuntar, sCorrer).AddCondition(AnimatorConditionMode.Greater, 0.1f, "Velocidad");
        Transicion(sCorrer, sApuntar).AddCondition(AnimatorConditionMode.Less, 0.1f, "Velocidad");
        TransicionGlobal(sm, sAtacar).AddCondition(AnimatorConditionMode.If, 0, "Atacar");
        TransicionGlobal(sm, sGolpe).AddCondition(AnimatorConditionMode.If, 0, "Golpe");
        AlTerminar(sAtacar, sApuntar);
        AlTerminar(sGolpe, sApuntar);
        MuerteGana(sm, sMuerte);
        EditorUtility.SetDirty(ctrl);
    }

    static void CrearAlebrije()
    {
        const string pre = "Bosses/alebrije";
        Sprite[] caminar = Cuadros(pre + "_caminar", 4);

        AnimationClip reposo = CrearClip("Alebrije/Alebrije_Reposo", new[] { caminar[0] }, 1f, true);
        AnimationClip andar = CrearClip("Alebrije/Alebrije_Caminar", caminar, 7f, true);
        AnimationClip salto = CrearClip("Alebrije/Alebrije_Salto", Cuadros(pre + "_salto", 1), 1f, true);
        AnimationClip aterrizaje = CrearClip("Alebrije/Alebrije_Aterrizaje", Cuadros(pre + "_aterrizaje", 1), 1f, false);
        AnimationClip zarpazo = CrearClip("Alebrije/Alebrije_Zarpazo", Cuadros(pre + "_zarpazo", 1), 1f, false);
        AnimationClip rugido = CrearClip("Alebrije/Alebrije_Rugido", Cuadros(pre + "_rugido", 1), 1f, true);
        AnimationClip morir = CrearClip("Alebrije/Alebrije_Muerte", Cuadros(pre + "_muerte", 4), 5f / 3f, false);   // 0.6 s por cuadro: que se vea deshacerse

        AnimatorController ctrl = NuevoControlador("Alebrije/Alebrije.controller");
        ctrl.AddParameter("Velocidad", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Muerto", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
        AnimatorState sReposo = sm.AddState("Reposo"); sReposo.motion = reposo;
        AnimatorState sCaminar = sm.AddState("Caminar"); sCaminar.motion = andar;
        sm.AddState("Salto").motion = salto;
        sm.AddState("Aterrizaje").motion = aterrizaje;
        sm.AddState("Zarpazo").motion = zarpazo;
        sm.AddState("Rugido").motion = rugido;
        AnimatorState sMuerte = sm.AddState("Muerte"); sMuerte.motion = morir;
        sm.defaultState = sReposo;

        Transicion(sReposo, sCaminar).AddCondition(AnimatorConditionMode.Greater, 0.1f, "Velocidad");
        Transicion(sCaminar, sReposo).AddCondition(AnimatorConditionMode.Less, 0.1f, "Velocidad");
        MuerteGana(sm, sMuerte);
        EditorUtility.SetDirty(ctrl);
    }

    // ------------------------------------------------------------------ Utilidades
    static AnimatorController NuevoControlador(string nombre)
    {
        string ruta = Carpeta + nombre;
        AssetDatabase.DeleteAsset(ruta);
        return AnimatorController.CreateAnimatorControllerAtPath(ruta);
    }

    static AnimatorStateTransition TransicionGlobal(AnimatorStateMachine sm, AnimatorState hasta)
    {
        AnimatorStateTransition t = Transicion(sm, hasta);
        t.AddCondition(AnimatorConditionMode.IfNot, 0, "Muerto");
        return t;
    }

    /// <summary>Al acabar el clip (no en bucle) regresa al estado indicado.</summary>
    static void AlTerminar(AnimatorState desde, AnimatorState hasta)
    {
        AnimatorStateTransition t = desde.AddTransition(hasta);
        t.hasExitTime = true; t.exitTime = 1f; t.duration = 0f;
    }

    static void MuerteGana(AnimatorStateMachine sm, AnimatorState muerte)
    {
        Transicion(sm, muerte).AddCondition(AnimatorConditionMode.If, 0, "Muerto");
    }
}
