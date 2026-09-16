using System.Collections;
using UnityEngine;

/// <summary>
/// Forage bush: spawns a world ItemPickup, then hides and respawns.
/// </summary>
public class ForagePatch : MonoBehaviour, IInteractable
{
    public float respawnSeconds = 30f;
    public ItemData[] possibleDrops;
    public ItemPickup itemPickupPrefab;
    public float spawnJitter = 0.15f;

    bool _available = true;

    public string GetInteractionPrompt() => "Forage";

    public bool CanInteract(GameObject interactor) => _available;

    public void Interact(GameObject interactor)
    {
        if (!_available)
            return;

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayForage();

        ItemData found = PickDrop();
        if (found != null)
            SpawnPickup(found, 1);
        else
            Debug.Log("Nothing useful in this bush.");

        _available = false;
        SetPresent(false);

        if (QuestManager.Instance != null)
            QuestManager.Instance.ReportForage();

        StartCoroutine(RespawnRoutine());
    }

    void SpawnPickup(ItemData found, int amount)
    {
        Vector3 spawnPos = transform.position;
        if (spawnJitter > 0f)
        {
            spawnPos += new Vector3(
                Random.Range(-spawnJitter, spawnJitter),
                Random.Range(-spawnJitter, spawnJitter),
                0f);
        }

        ItemPickup pickup;
        if (itemPickupPrefab != null)
        {
            pickup = Instantiate(itemPickupPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            // Runtime fallback so foraging still works if the prefab ref was never wired.
            var go = new GameObject("ItemPickup");
            go.transform.position = spawnPos;
            go.transform.localScale = new Vector3(1.35f, 1.35f, 1f);
            go.AddComponent<SpriteRenderer>();
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.25f;
            pickup = go.AddComponent<ItemPickup>();
        }

        pickup.Setup(found, amount);

        string name = string.IsNullOrEmpty(found.itemName) ? found.name : found.itemName;
        Debug.Log($"Foraged {name} — pickup spawned.");
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
