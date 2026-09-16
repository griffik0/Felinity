using System.Collections;
using UnityEngine;

/// <summary>
/// Forage bush: spawns a world ItemPickup, then hides and respawns.
/// Tracks remaining respawn time for save/load.
/// </summary>
public class ForagePatch : MonoBehaviour, IInteractable
{
    public float respawnSeconds = 30f;
    public ItemData[] possibleDrops;
    public ItemPickup itemPickupPrefab;
    public float spawnJitter = 0.15f;

    [Tooltip("Stable id for save/load. Defaults to GameObject name.")]
    public string saveId;

    bool _available = true;
    float _respawnRemaining;
    Coroutine _respawnRoutine;

    public bool IsAvailable => _available;

    /// <summary>Seconds left until respawn; 0 if available.</summary>
    public float RespawnRemainingSeconds => _available ? 0f : Mathf.Max(0f, _respawnRemaining);

    public string SaveId
    {
        get
        {
            if (!string.IsNullOrEmpty(saveId))
                return saveId;
            return gameObject.name;
        }
    }

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

        BeginDepleted(respawnSeconds);

        if (QuestManager.Instance != null)
            QuestManager.Instance.ReportForage();
    }

    /// <summary>Restore depleted/available state from a save (remaining &lt;= 0 → available).</summary>
    public void ApplySavedRespawn(float remainingSeconds)
    {
        if (remainingSeconds <= 0.01f)
        {
            ForceAvailable();
            return;
        }

        BeginDepleted(remainingSeconds);
    }

    void BeginDepleted(float remaining)
    {
        _available = false;
        SetPresent(false);
        _respawnRemaining = Mathf.Max(0.01f, remaining);

        if (_respawnRoutine != null)
            StopCoroutine(_respawnRoutine);
        _respawnRoutine = StartCoroutine(RespawnRoutine());
    }

    void ForceAvailable()
    {
        if (_respawnRoutine != null)
        {
            StopCoroutine(_respawnRoutine);
            _respawnRoutine = null;
        }

        _respawnRemaining = 0f;
        _available = true;
        SetPresent(true);
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
        while (_respawnRemaining > 0f)
        {
            _respawnRemaining -= Time.deltaTime;
            yield return null;
        }

        _respawnRemaining = 0f;
        _available = true;
        SetPresent(true);
        _respawnRoutine = null;
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
