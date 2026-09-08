using UnityEngine;

/// <summary>
/// Objects the player can interact with via the interaction range.
/// </summary>
public interface IInteractable
{
    string GetInteractionPrompt();

    void Interact(GameObject interactor);

    bool CanInteract(GameObject interactor) => true;
}
