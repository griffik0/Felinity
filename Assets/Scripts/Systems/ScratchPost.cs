using UnityEngine;

/// <summary>
/// Placeholder scratch post — grants CatCoins on interact.
/// </summary>
public class ScratchPost : MonoBehaviour, IInteractable
{
    public int coinsPerScratch = 2;

    public string GetInteractionPrompt() => "Scratch";

    public bool CanInteract(GameObject interactor) => true;

    public void Interact(GameObject interactor)
    {
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayScratch();

        CatCoins.Add(coinsPerScratch);
        Debug.Log($"Scratched the post! +{coinsPerScratch} CatCoins");

        if (QuestManager.Instance != null)
            QuestManager.Instance.ReportScratch();
    }
}
