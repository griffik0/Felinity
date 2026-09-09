using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Shared scene/quit helpers for title and pause UI.
/// </summary>
public static class GameFlow
{
    public const string TitleSceneName = "TitleScreen";
    public const string CharacterCreationSceneName = "CharacterCreation";
    public const string MainSceneName = "MainWorld";

    /// <summary>Game title shown on the title screen. Change this string to rename the game.</summary>
    public const string GameTitle = "Felinity";

    public static void LoadTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(TitleSceneName);
    }

    public static void LoadCharacterCreation()
    {
        Time.timeScale = 1f;
        PlayerProfile.EnsureExists();
        SceneManager.LoadScene(CharacterCreationSceneName);
    }

    public static void LoadMainWorld()
    {
        Time.timeScale = 1f;
        PlayerProfile.EnsureExists();
        SceneManager.LoadScene(MainSceneName);
        if (PlayerProfile.Instance != null)
            PlayerProfile.Instance.NotifyMainWorldLoaded();
    }

    public static void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
