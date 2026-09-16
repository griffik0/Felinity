using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Singleton pause controller for MainWorld. Escape toggles Time.timeScale and Pause UI.
/// Buttons: Resume, Quit to Title (with save prompt), Save and Quit.
/// </summary>
public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("Input")]
    public Key pauseKey = Key.Escape;

    [Header("UI")]
    public GameObject pauseCanvas;
    public Button resumeButton;
    public Button quitToTitleButton;
    public Button saveAndQuitButton;
    public TMP_Text pausedLabel;

    [Header("Quit-to-Title confirm (built at runtime if null)")]
    public GameObject savePromptRoot;
    public TMP_Text savePromptText;
    public Button savePromptYesButton;
    public Button savePromptNoButton;

    // Legacy serialized name from older scenes.
    [SerializeField, HideInInspector]
    Button quitGameButton;

    public bool IsPaused { get; private set; }

    public event Action<bool> OnPauseStateChanged;

    bool _promptOpen;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (saveAndQuitButton == null && quitGameButton != null)
            saveAndQuitButton = quitGameButton;

        if (pausedLabel != null)
            pausedLabel.text = "Paused";

        RelabelPauseButtons();
        EnsureSavePrompt();
        WireButtons();
        HideSavePrompt();
        SetPaused(false, notify: false);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null)
            return;

        if (!kb[pauseKey].wasPressedThisFrame)
            return;

        if (InventoryOverlayUI.Instance != null && InventoryOverlayUI.Instance.IsOpen)
        {
            InventoryOverlayUI.Instance.Close();
            return;
        }

        if (_promptOpen)
        {
            HideSavePrompt();
            return;
        }

        TogglePause();
    }

    public void TogglePause()
    {
        SetPaused(!IsPaused);
    }

    public void Resume()
    {
        HideSavePrompt();
        SetPaused(false);
    }

    public void SetPaused(bool paused, bool notify = true)
    {
        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;

        if (pauseCanvas != null)
            pauseCanvas.SetActive(paused);

        if (!paused)
            HideSavePrompt();

        if (notify)
        {
            OnPauseStateChanged?.Invoke(IsPaused);

            if (SFXManager.Instance != null)
            {
                if (paused)
                    SFXManager.Instance.PlayPauseOpen();
                else
                    SFXManager.Instance.PlayPauseClose();
            }
        }
    }

    /// <summary>Commit save, then quit the application.</summary>
    public void SaveAndQuit()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.CommitSave();
        SetPaused(false, notify: false);
        GameFlow.QuitGame();
    }

    /// <summary>Quit to title after committing a save.</summary>
    public void QuitToTitleWithSave()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.CommitSave();
        SetPaused(false, notify: false);
        GameFlow.LoadTitle();
    }

    /// <summary>Quit to title without keeping autosaves — restore last committed save.</summary>
    public void QuitToTitleWithoutSave()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.DiscardAutosavesAndRestoreCommitted();
        SetPaused(false, notify: false);
        GameFlow.LoadTitle();
    }

    void RelabelPauseButtons()
    {
        SetButtonLabel(resumeButton, "Resume");
        SetButtonLabel(quitToTitleButton, "Quit to Title");
        SetButtonLabel(saveAndQuitButton, "Save and Quit");
    }

    static void SetButtonLabel(Button button, string text)
    {
        if (button == null)
            return;
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            label.text = text;
    }

    void WireButtons()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(OnResumeClicked);
        }

        if (quitToTitleButton != null)
        {
            quitToTitleButton.onClick.RemoveAllListeners();
            quitToTitleButton.onClick.AddListener(OnQuitToTitleClicked);
        }

        if (saveAndQuitButton != null)
        {
            saveAndQuitButton.onClick.RemoveAllListeners();
            saveAndQuitButton.onClick.AddListener(OnSaveAndQuitClicked);
        }

        if (savePromptYesButton != null)
        {
            savePromptYesButton.onClick.RemoveAllListeners();
            savePromptYesButton.onClick.AddListener(OnSavePromptYes);
        }

        if (savePromptNoButton != null)
        {
            savePromptNoButton.onClick.RemoveAllListeners();
            savePromptNoButton.onClick.AddListener(OnSavePromptNo);
        }
    }

    void OnResumeClicked()
    {
        PlayUiClick();
        Resume();
    }

    void OnQuitToTitleClicked()
    {
        PlayUiClick();
        ShowSavePrompt();
    }

    void OnSaveAndQuitClicked()
    {
        PlayUiClick();
        SaveAndQuit();
    }

    void OnSavePromptYes()
    {
        PlayUiClick();
        HideSavePrompt();
        QuitToTitleWithSave();
    }

    void OnSavePromptNo()
    {
        PlayUiClick();
        HideSavePrompt();
        QuitToTitleWithoutSave();
    }

    void ShowSavePrompt()
    {
        EnsureSavePrompt();
        _promptOpen = true;
        if (savePromptRoot != null)
            savePromptRoot.SetActive(true);

        SetMainButtonsInteractable(false);
    }

    void HideSavePrompt()
    {
        _promptOpen = false;
        if (savePromptRoot != null)
            savePromptRoot.SetActive(false);

        SetMainButtonsInteractable(true);
    }

    void SetMainButtonsInteractable(bool interactable)
    {
        if (resumeButton != null) resumeButton.interactable = interactable;
        if (quitToTitleButton != null) quitToTitleButton.interactable = interactable;
        if (saveAndQuitButton != null) saveAndQuitButton.interactable = interactable;
    }

    void EnsureSavePrompt()
    {
        if (savePromptRoot != null)
            return;

        Transform parent = pauseCanvas != null ? pauseCanvas.transform : transform;
        var root = new GameObject("SavePrompt", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        var rootRt = root.GetComponent<RectTransform>();
        StretchFull(rootRt);

        var dim = root.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.65f);
        dim.raycastTarget = true;

        var panel = new GameObject("Panel", typeof(RectTransform));
        panel.transform.SetParent(root.transform, false);
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(460f, 220f);
        var panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0.10f, 0.11f, 0.14f, 0.98f);

        var promptGo = new GameObject("PromptText", typeof(RectTransform));
        promptGo.transform.SetParent(panel.transform, false);
        var promptRt = promptGo.GetComponent<RectTransform>();
        promptRt.anchorMin = new Vector2(0f, 0.45f);
        promptRt.anchorMax = new Vector2(1f, 1f);
        promptRt.offsetMin = new Vector2(20f, 0f);
        promptRt.offsetMax = new Vector2(-20f, -16f);
        savePromptText = promptGo.AddComponent<TextMeshProUGUI>();
        savePromptText.fontSize = 26f;
        savePromptText.alignment = TextAlignmentOptions.Center;
        savePromptText.color = Color.white;
        savePromptText.text = "Save before returning to title?";
        savePromptText.raycastTarget = false;

        savePromptYesButton = MakePromptButton("YesButton", panel.transform, "Yes", new Vector2(-100f, -60f));
        savePromptNoButton = MakePromptButton("NoButton", panel.transform, "No", new Vector2(100f, -60f));

        savePromptRoot = root;
        root.SetActive(false);
    }

    static Button MakePromptButton(string name, Transform parent, string label, Vector2 anchoredPos)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(140f, 48f);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.22f, 0.24f, 0.30f, 1f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var textGo = new GameObject("Label", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var textRt = textGo.GetComponent<RectTransform>();
        StretchFull(textRt);
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 22f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        return btn;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void PlayUiClick()
    {
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayButtonClick();
    }
}
