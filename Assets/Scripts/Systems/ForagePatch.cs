using System.Collections;
using UnityEngine;

/// <summary>
/// Placeholder forage patch — grants a random inventory item, then respawns.
/// </summary>
public class ForagePatch : MonoBehaviour, IInteractable
{
    public float respawnSeconds = 30f;
    public ItemData[] possibleDrops;

    bool _available = true;

    public string GetInteractionPrompt() => "Forage";

    public bool CanInteract(GameObject interactor) => _available;

    public void Interact(GameObject interactor)
    {
        if (!_available)
            return;

        ItemData found = PickDrop();
        if (found != null && InventorySystem.Instance != null)
        {
            InventorySystem.Instance.AddItem(found);
            string name = string.IsNullOrEmpty(found.itemName) ? found.name : found.itemName;
            Debug.Log($"Found {name}!");

            if (QuestManager.Instance != null)
                QuestManager.Instance.ReportItemCollected(found);
        }
        else
        {
            Debug.Log("Found some catnip!");
        }

        _available = false;
        SetPresent(false);

        if (QuestManager.Instance != null)
            QuestManager.Instance.ReportForage();

        StartCoroutine(RespawnRoutine());
    }

    ItemData PickDrop()
    {
        if (possibleDrops == null || possibleDrops.Length == 0)
            return null;

        int index = Random.Range(0, possibleDrops.Length);
        return possibleDrops[index];
    }

    IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnSeconds);
        _available = true;
        SetPresent(true);
    }

    void SetPresent(bool present)
    {
        foreach (Collider2D col in GetComponentsInChildren<Collider2D>(true))
            col.enabled = present;

        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            r.enabled = present;

        foreach (TMPro.TextMeshPro label in GetComponentsInChildren<TMPro.TextMeshPro>(true))
            label.enabled = present;
    }
}
