#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Always start Play Mode from TitleScreen so MainWorld work doesn't skip title + character creation.
/// </summary>
[InitializeOnLoad]
public static class PlayModeStartScene
{
    const string TitleScenePath = "Assets/Scenes/TitleScreen.unity";

    static PlayModeStartScene()
    {
        Ensure();
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
            Ensure();
    }

    static void Ensure()
    {
        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(TitleScenePath);
        if (sceneAsset == null)
            return;

        if (EditorSceneManager.playModeStartScene != sceneAsset)
            EditorSceneManager.playModeStartScene = sceneAsset;
    }

    [MenuItem("Felinity/Use TitleScreen as Play Mode Start")]
    public static void MenuEnsure()
    {
        Ensure();
        var current = EditorSceneManager.playModeStartScene;
        string path = current != null ? AssetDatabase.GetAssetPath(current) : "(none)";
        Debug.Log($"[PlayModeStartScene] Play Mode starts from: {path}");
    }
}
#endif
