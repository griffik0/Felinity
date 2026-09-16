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
        LayoutContinueBetweenStartAndQuit();
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
        LayoutContinueBetweenStartAndQuit();
        RefreshContinueVisibility();
    }

    public void OnStartGame()
    {
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayButtonClick();

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
        }

        TMP_Text label = clone.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            label.text = "Continue";

        continueButton = clone.GetComponent<Button>();
        clone.SetActive(false);
    }

    void LayoutContinueBetweenStartAndQuit()
    {
        if (startButton == null || quitButton == null)
            return;

        var startRt = startButton.GetComponent<RectTransform>();
        var quitRt = quitButton.GetComponent<RectTransform>();
        if (startRt == null || quitRt == null)
            return;

        // Keep a clear vertical gap between all three buttons.
        const float spacing = 100f;
        Vector2 center = (startRt.anchoredPosition + quitRt.anchoredPosition) * 0.5f;
        // Prefer a fixed column around the current Start X.
        float x = startRt.anchoredPosition.x;

        startRt.anchoredPosition = new Vector2(x, center.y + spacing);
        quitRt.anchoredPosition = new Vector2(x, center.y - spacing);

        if (continueButton == null)
            return;

        var continueRt = continueButton.GetComponent<RectTransform>();
        if (continueRt == null)
            return;

        continueRt.anchoredPosition = new Vector2(x, center.y);
        continueRt.sizeDelta = startRt.sizeDelta;
    }
}
