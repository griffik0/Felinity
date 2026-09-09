using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Singleton pause controller for MainWorld. Escape toggles Time.timeScale and Pause UI.
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
    public Button quitGameButton;
    public TMP_Text pausedLabel;

    public bool IsPaused { get; private set; }

    public event Action<bool> OnPauseStateChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (pausedLabel != null)
            pausedLabel.text = "Paused";

        WireButtons();
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

        // Inventory overlay takes priority: Esc closes it before opening pause.
        if (InventoryOverlayUI.Instance != null && InventoryOverlayUI.Instance.IsOpen)
        {
            InventoryOverlayUI.Instance.Close();
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
        SetPaused(false);
    }

    public void SetPaused(bool paused, bool notify = true)
    {
        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;

        if (pauseCanvas != null)
            pauseCanvas.SetActive(paused);

        if (notify)
            OnPauseStateChanged?.Invoke(IsPaused);
    }

    public void QuitToTitle()
    {
        SetPaused(false, notify: false);
        GameFlow.LoadTitle();
    }

    public void QuitGame()
    {
        SetPaused(false, notify: false);
        GameFlow.QuitGame();
    }

    void WireButtons()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(Resume);
            resumeButton.onClick.AddListener(Resume);
        }

        if (quitToTitleButton != null)
        {
            quitToTitleButton.onClick.RemoveListener(QuitToTitle);
            quitToTitleButton.onClick.AddListener(QuitToTitle);
        }

        if (quitGameButton != null)
        {
            quitGameButton.onClick.RemoveListener(QuitGame);
            quitGameButton.onClick.AddListener(QuitGame);
        }
    }
}
