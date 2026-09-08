using TMPro;
using UnityEngine;

/// <summary>
/// Shows "[E] &lt;prompt&gt;" above the player when an interactable is in range.
/// </summary>
public class InteractionPromptUI : MonoBehaviour
{
    public PlayerController player;
    public TMP_Text promptText;
    public Vector3 worldOffset = new Vector3(0f, 0.85f, 0f);

    Camera _camera;

    void Awake()
    {
        if (promptText == null)
            promptText = GetComponent<TMP_Text>();

        if (player == null)
            player = FindFirstObjectByType<PlayerController>();

        _camera = Camera.main;
        Hide();
    }

    void LateUpdate()
    {
        if (promptText == null || player == null)
            return;

        IInteractable target = player.CurrentInteractable;
        if (target == null || !target.CanInteract(player.gameObject))
        {
            Hide();
            return;
        }

        promptText.enabled = true;
        promptText.text = $"[E] {target.GetInteractionPrompt()}";

        Vector3 worldPos = player.transform.position + worldOffset;
        if (_camera == null)
            _camera = Camera.main;

        if (_camera != null)
            promptText.rectTransform.position = _camera.WorldToScreenPoint(worldPos);
    }

    void Hide()
    {
        if (promptText != null)
            promptText.enabled = false;
    }
}
