#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class CBBProjectSetup
{
    private const string ScenePath = "Assets/Scenes/Main.unity";

    static CBBProjectSetup()
    {
        EditorApplication.delayCall += Setup;
    }

    private static void Setup()
    {
        if (Application.isPlaying || System.IO.File.Exists(ScenePath)) return;

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("CBB Game Manager").AddComponent<CBBPrototype>();
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        PlayerSettings.productName = "C・B・B";
        PlayerSettings.companyName = "CBB Prototype";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(ScenePath);
        Debug.Log("C・B・B prototype scene created. Press Play to begin.");
    }
}
#endif
