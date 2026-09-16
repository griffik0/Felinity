using System.Collections;
using UnityEngine;

/// <summary>
/// World pickup that chases the player in range, then grants inventory on touch.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
public class ItemPickup : MonoBehaviour
{
    public ItemData item;
    public int amount = 1;
    public float chaseSpeed = 2f;
    public float chaseRange = 1.1f;
    public float pickupRange = 0.4f;

    SpriteRenderer _renderer;
    CircleCollider2D _collider;
    Transform _player;
    bool _collecting;

    void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _collider = GetComponent<CircleCollider2D>();
        if (_collider != null)
        {
            _collider.isTrigger = true;
            if (_collider.radius < 0.05f)
                _collider.radius = 0.25f;
        }

        ApplyVisual();
    }

    void Start()
    {
        CachePlayer();
        ApplyVisual();
    }

    public void Setup(ItemData data, int count = 1)
    {
        item = data;
        amount = Mathf.Max(1, count);
        ApplyVisual();
    }

    void Update()
    {
        if (_collecting || item == null)
            return;

        if (_player == null)
        {
            CachePlayer();
            if (_player == null)
                return;
        }

        Vector2 toPlayer = (Vector2)_player.position - (Vector2)transform.position;
        float distance = toPlayer.magnitude;

        if (distance <= pickupRange)
        {
            Collect();
            return;
        }

        if (distance <= chaseRange && distance > 0.0001f)
        {
            Vector2 step = toPlayer.normalized * chaseSpeed * Time.deltaTime;
            if (step.magnitude > distance)
                transform.position = _player.position;
            else
                transform.position = (Vector2)transform.position + step;
        }
    }

    void Collect()
    {
        if (_collecting)
            return;

        _collecting = true;

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayItemPickupChime();

        if (InventorySystem.Instance != null && item != null)
            InventorySystem.Instance.AddItem(item, amount);

        string displayName = item == null
            ? "item"
            : (string.IsNullOrEmpty(item.itemName) ? item.name : item.itemName);

        if (PickupPopupUI.Instance != null)
            PickupPopupUI.Instance.ShowPopup(displayName, amount);

        StartCoroutine(PopAndDestroy());
    }

    IEnumerator PopAndDestroy()
    {
        if (_collider != null)
            _collider.enabled = false;

        Vector3 startScale = transform.localScale;
        float duration = 0.15f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
        }

        Destroy(gameObject);
    }

    void ApplyVisual()
    {
        if (_renderer == null)
            _renderer = GetComponent<SpriteRenderer>();
        if (_renderer == null)
            return;

        if (item != null && item.icon != null)
        {
            _renderer.sprite = item.icon;
            _renderer.color = Color.white;
        }
        else
        {
            _renderer.sprite = InventorySlotUI.GetFallbackIcon();
            _renderer.color = new Color(0.75f, 0.78f, 0.85f, 1f);
        }

        _renderer.sortingOrder = 8;
    }

    void CachePlayer()
    {
        var controller = Object.FindAnyObjectByType<PlayerController>();
        if (controller != null)
            _player = controller.transform;
    }
}
