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
    public Button continueButton;
    public Button quitButton;

    void Awake()
    {
        if (titleText != null)
            titleText.text = GameFlow.GameTitle;

        EnsureContinueButton();
        RefreshContinueVisibility();

        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartGame);
            startButton.onClick.AddListener(OnStartGame);
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnContinue);
            continueButton.onClick.AddListener(OnContinue);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(OnQuit);
            quitButton.onClick.AddListener(OnQuit);
        }
    }

    void OnEnable()
    {
        RefreshContinueVisibility();
    }

    public void OnStartGame()
    {
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayButtonClick();

        // Fresh run: discard any existing save so Continue won't restore the old one.
        if (SaveManager.Instance != null)
            SaveManager.Instance.BeginNewGame();

        GameFlow.LoadCharacterCreation();
    }

    public void OnContinue()
    {
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayButtonClick();

        if (SaveManager.Instance == null || !SaveManager.Instance.HasSaveFile())
            return;

        SaveManager.Instance.ContinueGame();
    }

    public void OnQuit()
    {
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayButtonClick();
        GameFlow.QuitGame();
    }

    void RefreshContinueVisibility()
    {
        bool hasSave = SaveManager.Instance != null && SaveManager.Instance.HasSaveFile();
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(hasSave);
            continueButton.interactable = hasSave;
        }
    }

    void EnsureContinueButton()
    {
        if (continueButton != null || startButton == null)
            return;

        // Clone Start Game so layout/style match without hand-editing the scene.
        GameObject clone = Instantiate(startButton.gameObject, startButton.transform.parent);
        clone.name = "ContinueButton";

        var rt = clone.GetComponent<RectTransform>();
        var startRt = startButton.GetComponent<RectTransform>();
        if (rt != null && startRt != null)
        {
            rt.anchorMin = startRt.anchorMin;
            rt.anchorMax = startRt.anchorMax;
            rt.pivot = startRt.pivot;
            rt.sizeDelta = startRt.sizeDelta;
            // Sit between Start and Quit: nudge Start up, Continue in the old Start spot, Quit stays.
            Vector2 startPos = startRt.anchoredPosition;
            float gap = Mathf.Max(72f, startRt.sizeDelta.y + 16f);
            startRt.anchoredPosition = startPos + new Vector2(0f, gap * 0.5f);
            rt.anchoredPosition = startPos - new Vector2(0f, gap * 0.5f);
        }

        TMP_Text label = clone.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            label.text = "Continue";

        continueButton = clone.GetComponent<Button>();
        clone.SetActive(false);
    }
}
