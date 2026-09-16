using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Tracks the active toolbar hotbar index (0-6) via scroll wheel and keys 1-7.
/// </summary>
public class ToolbarSelection : MonoBehaviour
{
    public static ToolbarSelection Instance { get; private set; }

    public const int SlotCount = InventorySystem.ToolbarSize;

    [SerializeField]
    int currentSlotIndex;

    public int CurrentSlotIndex => currentSlotIndex;

    public event Action<int> OnSelectedSlotChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        currentSlotIndex = Mathf.Clamp(currentSlotIndex, 0, SlotCount - 1);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        // Don't change hotbar selection while the full inventory overlay is open.
        if (InventoryOverlayUI.Instance != null && InventoryOverlayUI.Instance.IsOpen)
            return;

        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
            return;

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (scroll > 0.01f)
                SetSelectedIndex(currentSlotIndex - 1);
            else if (scroll < -0.01f)
                SetSelectedIndex(currentSlotIndex + 1);
        }

        Keyboard kb = Keyboard.current;
        if (kb == null)
            return;

        if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) SetSelectedIndex(0);
        else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) SetSelectedIndex(1);
        else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) SetSelectedIndex(2);
        else if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) SetSelectedIndex(3);
        else if (kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame) SetSelectedIndex(4);
        else if (kb.digit6Key.wasPressedThisFrame || kb.numpad6Key.wasPressedThisFrame) SetSelectedIndex(5);
        else if (kb.digit7Key.wasPressedThisFrame || kb.numpad7Key.wasPressedThisFrame) SetSelectedIndex(6);
    }

    public void SetSelectedIndex(int index)
    {
        int wrapped = ((index % SlotCount) + SlotCount) % SlotCount;
        if (wrapped == currentSlotIndex)
            return;

        currentSlotIndex = wrapped;
        OnSelectedSlotChanged?.Invoke(currentSlotIndex);

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayToolbarSwitch();
    }
}
