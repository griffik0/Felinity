using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Bottom-of-screen dialogue box. Hidden until Play() is called.
/// </summary>
public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance { get; private set; }

    [Header("UI")]
    public GameObject root;
    public TMP_Text speakerText;
    public TMP_Text bodyText;
    public TMP_Text continuePromptText;
    public string continuePrompt = "[E] Continue";

    readonly List<DialogueLine> _queue = new List<DialogueLine>();
    int _index;
    Action _onComplete;
    bool _advanceArmed;

    public bool IsOpen { get; private set; }

    /// <summary>
    /// True on the frame dialogue closed (and until interact is released), so the same
    /// E/click that dismissed a line cannot immediately re-trigger Talk.
    /// </summary>
    public bool BlocksWorldInput { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        HideImmediate();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (!IsOpen)
        {
            // Keep blocking world interact until the dismiss key/button is released.
            if (BlocksWorldInput && !IsAdvanceHeld())
                BlocksWorldInput = false;
            return;
        }

        // Arm after one frame so the same E press that started talk doesn't skip the first line.
        if (!_advanceArmed)
        {
            _advanceArmed = true;
            return;
        }

        if (WasAdvancePressed())
            Advance();
    }

    public void Play(IList<DialogueLine> lines, Action onComplete = null)
    {
        if (lines == null || lines.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        _queue.Clear();
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i] != null && !string.IsNullOrWhiteSpace(lines[i].text))
                _queue.Add(lines[i]);
        }

        if (_queue.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        _index = 0;
        _onComplete = onComplete;
        _advanceArmed = false;
        IsOpen = true;

        if (root != null)
            root.SetActive(true);

        if (continuePromptText != null)
            continuePromptText.text = continuePrompt;

        ShowCurrent();
    }

    public void Advance()
    {
        if (!IsOpen)
            return;

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayDialogueAdvance();

        _index++;
        if (_index >= _queue.Count)
        {
            Close();
            return;
        }

        ShowCurrent();
    }

    void ShowCurrent()
    {
        DialogueLine line = _queue[_index];
        if (speakerText != null)
            speakerText.text = string.IsNullOrWhiteSpace(line.speakerName) ? string.Empty : line.speakerName;
        if (bodyText != null)
            bodyText.text = line.text ?? string.Empty;
    }

    void Close()
    {
        Action done = _onComplete;
        _onComplete = null;
        _queue.Clear();
        _index = 0;
        BlocksWorldInput = true;
        HideImmediate();
        done?.Invoke();
    }

    void HideImmediate()
    {
        IsOpen = false;
        _advanceArmed = false;
        if (root != null)
            root.SetActive(false);
    }

    static bool WasAdvancePressed()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null && (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
            return true;

        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            return true;

        return false;
    }

    static bool IsAdvanceHeld()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null && (kb.eKey.isPressed || kb.spaceKey.isPressed || kb.enterKey.isPressed))
            return true;

        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.isPressed)
            return true;

        return false;
    }
}
