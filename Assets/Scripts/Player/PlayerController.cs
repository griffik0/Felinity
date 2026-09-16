using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Top-down 8-directional player movement via Rigidbody2D,
/// plus E / left-click interaction with nearby IInteractables.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;

    [Header("Footsteps")]
    public float footstepInterval = 0.35f;

    [Header("Interaction")]
    public float interactionRadius = 1.25f;

    /// <summary>
    /// True while the player has non-zero movement input. Useful for animation.
    /// </summary>
    public bool IsMoving { get; private set; }

    /// <summary>
    /// Current movement input after diagonal normalization (zero when idle).
    /// </summary>
    public Vector2 MoveInput => _moveInput;

    /// <summary>
    /// Closest valid interactable currently inside InteractionRange.
    /// </summary>
    public IInteractable CurrentInteractable { get; private set; }

    Rigidbody2D _rb;
    Vector2 _moveInput;
    readonly List<IInteractable> _inRange = new List<IInteractable>();
    CircleCollider2D _interactionTrigger;
    float _footstepTimer;
    bool _nextFootstepIsB;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;

        EnsureInteractionRange();
    }

    void Update()
    {
        DialogueUI dialogue = DialogueUI.Instance;
        bool inventoryOpen = InventoryOverlayUI.Instance != null && InventoryOverlayUI.Instance.IsOpen;

        // Full inventory overlay (and dialogue) act as modal screens: no move/interact.
        if ((dialogue != null && dialogue.IsOpen) || inventoryOpen)
        {
            _moveInput = Vector2.zero;
            IsMoving = false;
            _footstepTimer = 0f;
            CurrentInteractable = null;
            return;
        }

        _moveInput = ReadMoveInput();
        if (_moveInput.sqrMagnitude > 1f)
            _moveInput.Normalize();

        IsMoving = _moveInput.sqrMagnitude > 0.0001f;
        UpdateFootsteps();
        RefreshClosestInteractable();

        // Ignore the same E/click that just dismissed dialogue (prevents soft-lock re-Talk).
        if (WasInteractPressed() &&
            (dialogue == null || !dialogue.BlocksWorldInput))
            OnInteract();
    }

    void UpdateFootsteps()
    {
        if (!IsMoving)
        {
            _footstepTimer = 0f;
            return;
        }

        _footstepTimer += Time.deltaTime;
        if (_footstepTimer < footstepInterval)
            return;

        _footstepTimer = 0f;
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayFootstep(_nextFootstepIsB);
        _nextFootstepIsB = !_nextFootstepIsB;
    }

    void FixedUpdate()
    {
        if (!IsMoving)
            return;

        Vector2 next = _rb.position + _moveInput * moveSpeed * Time.fixedDeltaTime;
        _rb.MovePosition(next);
    }

    /// <summary>
    /// Interact with the closest valid IInteractable in range.
    /// </summary>
    public void OnInteract()
    {
        RefreshClosestInteractable();
        if (CurrentInteractable == null)
            return;

        if (!CurrentInteractable.CanInteract(gameObject))
            return;

        CurrentInteractable.Interact(gameObject);
        RefreshClosestInteractable();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        IInteractable interactable = other.GetComponentInParent<IInteractable>();
        if (interactable == null)
            return;

        if (!_inRange.Contains(interactable))
            _inRange.Add(interactable);

        RefreshClosestInteractable();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        IInteractable interactable = other.GetComponentInParent<IInteractable>();
        if (interactable == null)
            return;

        _inRange.Remove(interactable);
        RefreshClosestInteractable();
    }

    void RefreshClosestInteractable()
    {
        CurrentInteractable = null;
        float bestDist = float.MaxValue;

        for (int i = _inRange.Count - 1; i >= 0; i--)
        {
            IInteractable candidate = _inRange[i];
            if (candidate == null)
            {
                _inRange.RemoveAt(i);
                continue;
            }

            Component asComponent = candidate as Component;
            if (asComponent == null || !asComponent.gameObject.activeInHierarchy)
            {
                _inRange.RemoveAt(i);
                continue;
            }

            if (!candidate.CanInteract(gameObject))
                continue;

            float dist = Vector2.Distance(transform.position, asComponent.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                CurrentInteractable = candidate;
            }
        }
    }

    void EnsureInteractionRange()
    {
        Transform child = transform.Find("InteractionRange");
        GameObject rangeGo;
        if (child == null)
        {
            rangeGo = new GameObject("InteractionRange");
            rangeGo.transform.SetParent(transform, false);
            rangeGo.transform.localPosition = Vector3.zero;
        }
        else
        {
            rangeGo = child.gameObject;
        }

        _interactionTrigger = rangeGo.GetComponent<CircleCollider2D>();
        if (_interactionTrigger == null)
            _interactionTrigger = rangeGo.AddComponent<CircleCollider2D>();

        _interactionTrigger.isTrigger = true;
        _interactionTrigger.radius = interactionRadius;
    }

    static bool WasInteractPressed()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.eKey.wasPressedThisFrame)
            return true;

        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            return true;

        return false;
    }

    static Vector2 ReadMoveInput()
    {
        Vector2 input = Vector2.zero;

        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1f;
        }

        return input;
    }
}
