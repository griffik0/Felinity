using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// One Stardew-style inventory grid slot: icon, stack count, hover tooltip.
/// </summary>
[RequireComponent(typeof(Image))]
public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    static Sprite _fallbackIcon;

    [Header("UI")]
    public Image iconImage;
    public Image backgroundImage;
    public TMP_Text countText;

    [Header("Colors")]
    public Color emptySlotColor = new Color(0.18f, 0.19f, 0.23f, 1f);
    public Color filledSlotColor = new Color(0.28f, 0.30f, 0.36f, 1f);
    public Color emptyIconColor = new Color(0.35f, 0.37f, 0.42f, 1f);
    public Color filledIconColor = Color.white;

    ItemData _item;
    int _count;
    InventoryOverlayUI _overlay;

    public ItemData Item => _item;
    public int Count => _count;
    public bool IsEmpty => _item == null || _count <= 0;

    void Awake()
    {
        EnsureRefs();
        Clear();
    }

    public void BindOverlay(InventoryOverlayUI overlay)
    {
        _overlay = overlay;
    }

    public void SetItem(ItemData item, int count)
    {
        EnsureRefs();

        if (item == null || count <= 0)
        {
            Clear();
            return;
        }

        _item = item;
        _count = count;

        if (backgroundImage != null)
            backgroundImage.color = filledSlotColor;

        if (iconImage != null)
        {
            iconImage.enabled = true;
            iconImage.sprite = item.icon != null ? item.icon : GetFallbackIcon();
            iconImage.color = item.icon != null ? filledIconColor : emptyIconColor;
            iconImage.preserveAspect = true;
        }

        if (countText != null)
        {
            // Always show xN for held stacks (including singles).
            bool showCount = count >= 1;
            countText.gameObject.SetActive(showCount);
            if (showCount)
                countText.text = $"x{count}";
        }
    }

    public void Clear()
    {
        EnsureRefs();

        _item = null;
        _count = 0;

        if (backgroundImage != null)
            backgroundImage.color = emptySlotColor;

        if (iconImage != null)
        {
            iconImage.enabled = true;
            iconImage.sprite = GetFallbackIcon();
            iconImage.color = emptyIconColor;
            iconImage.preserveAspect = true;
        }

        if (countText != null)
        {
            countText.text = string.Empty;
            countText.gameObject.SetActive(false);
        }

        if (_overlay != null)
            _overlay.HideTooltip(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (IsEmpty || _overlay == null)
            return;

        string name = string.IsNullOrEmpty(_item.itemName) ? _item.name : _item.itemName;
        string body = string.IsNullOrWhiteSpace(_item.description) ? null : _item.description.Trim();
        _overlay.ShowTooltip(this, name, body);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_overlay != null)
            _overlay.HideTooltip(this);
    }

    void EnsureRefs()
    {
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if (iconImage == null)
        {
            Transform iconTf = transform.Find("Icon");
            if (iconTf != null)
                iconImage = iconTf.GetComponent<Image>();
        }

        if (countText == null)
        {
            Transform countTf = transform.Find("Count");
            if (countTf != null)
                countText = countTf.GetComponent<TMP_Text>();
        }
    }

    public static Sprite GetFallbackIcon()
    {
        if (_fallbackIcon != null)
            return _fallbackIcon;

        var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        var pixels = new Color32[64];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(180, 180, 185, 255);
        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        _fallbackIcon = Sprite.Create(tex, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 8f);
        _fallbackIcon.name = "InventorySlotFallback";
        return _fallbackIcon;
    }
}
