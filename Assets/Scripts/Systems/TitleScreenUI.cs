using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Wires TitleScreen buttons. Place on the TitleScreen Canvas (or any GO in that scene).
/// </summary>
public class TitleScreenUI : MonoBehaviour
{
    [Tooltip("Optional. If set, overwritten with GameFlow.GameTitle on Awake.")]
    public TMP_Text titleText;

    public Button startButton;
    public Button quitButton;

    void Awake()
    {
        if (titleText != null)
            titleText.text = GameFlow.GameTitle;

        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartGame);
            startButton.onClick.AddListener(OnStartGame);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(OnQuit);
            quitButton.onClick.AddListener(OnQuit);
        }
    }

    public void OnStartGame()
    {
        GameFlow.LoadCharacterCreation();
    }

    public void OnQuit()
    {
        GameFlow.QuitGame();
    }
}
