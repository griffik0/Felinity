using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Full-screen inventory overlay (Tab). Stardew-style slot grid.
/// Pickup feedback uses PickupPopupUI; this is the Tab-toggle bag.
/// Blocks player movement while open, but does not pause time / PauseManager.
/// </summary>
public class InventoryOverlayUI : MonoBehaviour
{
    public const int DefaultColumns = 6;
    public const int DefaultRows = 4;

    public static InventoryOverlayUI Instance { get; private set; }

    [Header("Input")]
    public Key toggleKey = Key.Tab;

    [Header("Grid")]
    public int columns = DefaultColumns;
    public int rows = DefaultRows;
    public InventorySlotUI slotPrefab;

    [Header("UI")]
    public GameObject root;
    public TMP_Text titleText;
    public TMP_Text uniqueCountText;
    public RectTransform gridRoot;
    public RectTransform tooltipRoot;
    public TMP_Text tooltipTitleText;
    public TMP_Text tooltipBodyText;

    public bool IsOpen { get; private set; }

    InventorySystem _inventory;
    readonly List<InventorySlotUI> _slots = new List<InventorySlotUI>();
    InventorySlotUI _tooltipOwner;
    Canvas _canvas;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureUi();
        EnsureSlots();
        SetOpen(false, refresh: false);
    }

    void OnDestroy()
    {
        Unbind();
        if (Instance == this)
            Instance = null;
    }

    void OnEnable()
    {
        Bind();
        if (IsOpen)
            Refresh();
    }

    void OnDisable()
    {
        Unbind();
        HideTooltip(null);
    }

    void Start()
    {
        Bind();
        if (IsOpen)
            Refresh();
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null && kb[toggleKey].wasPressedThisFrame)
        {
            // Don't open over the pause menu; still allow closing if somehow open while paused.
            if (!IsOpen && PauseManager.Instance != null && PauseManager.Instance.IsPaused)
                return;

            Toggle();
        }

        if (_tooltipOwner != null)
            UpdateTooltipPosition();
    }

    public void Toggle()
    {
        SetOpen(!IsOpen);
    }

    public void Open()
    {
        SetOpen(true);
    }

    public void Close()
    {
        SetOpen(false);
    }

    public void SetOpen(bool open, bool refresh = true)
    {
        IsOpen = open;

        if (root != null)
            root.SetActive(open);

        if (!open)
            HideTooltip(null);

        if (open && refresh)
            Refresh();
    }

    void Bind()
    {
        if (_inventory == null)
            _inventory = InventorySystem.Instance;

        if (_inventory == null)
            return;

        _inventory.OnInventoryChanged -= Refresh;
        _inventory.OnInventoryChanged += Refresh;
    }

    void Unbind()
    {
        if (_inventory != null)
            _inventory.OnInventoryChanged -= Refresh;
    }

    public void Refresh()
    {
        if (_inventory == null)
            _inventory = InventorySystem.Instance;

        EnsureUi();
        EnsureSlots();

        var entries = new List<KeyValuePair<ItemData, int>>();
        if (_inventory != null)
        {
            foreach (var pair in _inventory.Items)
            {
                if (pair.Key == null || pair.Value <= 0)
                    continue;
                entries.Add(pair);
            }
        }

        int unique = entries.Count;
        int capacity = _slots.Count;

        for (int i = 0; i < capacity; i++)
        {
            if (i < unique)
                _slots[i].SetItem(entries[i].Key, entries[i].Value);
            else
                _slots[i].Clear();
        }

        if (uniqueCountText != null)
            uniqueCountText.text = unique == 0
                ? "0 unique items"
                : $"{unique} unique item{(unique == 1 ? "" : "s")}";
    }

    public void ShowTooltip(InventorySlotUI owner, string title, string description)
    {
        if (tooltipRoot == null || owner == null)
            return;

        _tooltipOwner = owner;
        tooltipRoot.gameObject.SetActive(true);

        if (tooltipTitleText != null)
            tooltipTitleText.text = title ?? string.Empty;

        if (tooltipBodyText != null)
        {
            bool hasBody = !string.IsNullOrWhiteSpace(description);
            tooltipBodyText.gameObject.SetActive(hasBody);
            tooltipBodyText.text = hasBody ? description : string.Empty;
        }

        UpdateTooltipPosition();
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRoot);
    }

    public void HideTooltip(InventorySlotUI owner)
    {
        if (owner != null && _tooltipOwner != null && owner != _tooltipOwner)
            return;

        _tooltipOwner = null;
        if (tooltipRoot != null)
            tooltipRoot.gameObject.SetActive(false);
    }

    void UpdateTooltipPosition()
    {
        if (tooltipRoot == null || _canvas == null)
            return;

        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;

        Vector2 screen = mouse.position.ReadValue();
        Camera cam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
        RectTransform parent = tooltipRoot.parent as RectTransform;
        if (parent == null)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, cam, out Vector2 local))
            return;

        // Offset so the box sits beside the cursor instead of under it.
        tooltipRoot.anchoredPosition = local + new Vector2(18f, -18f);
    }

    void EnsureSlots()
    {
        int capacity = Mathf.Max(1, columns) * Mathf.Max(1, rows);
        while (_slots.Count < capacity)
        {
            InventorySlotUI slot = CreateSlot(_slots.Count);
            _slots.Add(slot);
        }

        for (int i = 0; i < _slots.Count; i++)
        {
            bool active = i < capacity;
            _slots[i].gameObject.SetActive(active);
            _slots[i].BindOverlay(this);
        }
    }

    InventorySlotUI CreateSlot(int index)
    {
        InventorySlotUI slot;
        if (slotPrefab != null)
        {
            slot = Instantiate(slotPrefab, gridRoot);
            slot.name = $"Slot_{index:00}";
        }
        else
        {
            slot = BuildSlotRuntime($"Slot_{index:00}", gridRoot);
        }

        slot.BindOverlay(this);
        slot.Clear();
        return slot;
    }

    void EnsureUi()
    {
        if (root != null && gridRoot != null && titleText != null && uniqueCountText != null && tooltipRoot != null)
        {
            _canvas = GetComponentInChildren<Canvas>(true);
            return;
        }

        Transform canvasTf = transform.Find("InventoryOverlayCanvas");
        GameObject canvasGo;
        if (canvasTf == null)
        {
            canvasGo = new GameObject("InventoryOverlayCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();
            _canvas = canvas;
        }
        else
        {
            canvasGo = canvasTf.gameObject;
            _canvas = canvasGo.GetComponent<Canvas>();
        }

        // Drop the old text-list ScrollView if a previous version left one around.
        Transform legacyScroll = root != null
            ? root.transform.Find("ScrollView")
            : canvasGo.transform.Find("Panel/ScrollView");
        if (legacyScroll != null)
            Destroy(legacyScroll.gameObject);

        if (root == null)
        {
            Transform existingPanel = canvasGo.transform.Find("Panel");
            if (existingPanel != null)
            {
                root = existingPanel.gameObject;
            }
            else
            {
                var panel = CreateRect("Panel", canvasGo.transform);
                var panelRt = panel.GetComponent<RectTransform>();
                panelRt.anchorMin = new Vector2(0.5f, 0.5f);
                panelRt.anchorMax = new Vector2(0.5f, 0.5f);
                panelRt.pivot = new Vector2(0.5f, 0.5f);
                panelRt.sizeDelta = new Vector2(620f, 520f);
                var bg = panel.AddComponent<Image>();
                bg.color = new Color(0.07f, 0.08f, 0.11f, 0.94f);
                root = panel;
            }
        }

        if (titleText == null)
        {
            Transform titleTf = root.transform.Find("Title");
            if (titleTf != null)
            {
                titleText = titleTf.GetComponent<TMP_Text>();
            }
            else
            {
                var titleGo = CreateRect("Title", root.transform);
                var titleRt = titleGo.GetComponent<RectTransform>();
                titleRt.anchorMin = new Vector2(0f, 1f);
                titleRt.anchorMax = new Vector2(1f, 1f);
                titleRt.pivot = new Vector2(0.5f, 1f);
                titleRt.anchoredPosition = new Vector2(0f, -16f);
                titleRt.sizeDelta = new Vector2(-40f, 40f);
                titleText = titleGo.AddComponent<TextMeshProUGUI>();
                titleText.fontSize = 34f;
                titleText.fontStyle = FontStyles.Bold;
                titleText.alignment = TextAlignmentOptions.Center;
                titleText.color = new Color(1f, 0.88f, 0.55f, 1f);
                titleText.text = "Inventory";
                titleText.raycastTarget = false;
            }
        }

        if (uniqueCountText == null)
        {
            Transform countTf = root.transform.Find("UniqueCount");
            if (countTf != null)
            {
                uniqueCountText = countTf.GetComponent<TMP_Text>();
            }
            else
            {
                var countGo = CreateRect("UniqueCount", root.transform);
                var countRt = countGo.GetComponent<RectTransform>();
                countRt.anchorMin = new Vector2(0f, 1f);
                countRt.anchorMax = new Vector2(1f, 1f);
                countRt.pivot = new Vector2(0.5f, 1f);
                countRt.anchoredPosition = new Vector2(0f, -56f);
                countRt.sizeDelta = new Vector2(-40f, 28f);
                uniqueCountText = countGo.AddComponent<TextMeshProUGUI>();
                uniqueCountText.fontSize = 20f;
                uniqueCountText.alignment = TextAlignmentOptions.Center;
                uniqueCountText.color = new Color(0.75f, 0.78f, 0.85f, 1f);
                uniqueCountText.text = "0 unique items";
                uniqueCountText.raycastTarget = false;
            }
        }

        if (root.transform.Find("Hint") == null)
        {
            var hintGo = CreateRect("Hint", root.transform);
            var hintRt = hintGo.GetComponent<RectTransform>();
            hintRt.anchorMin = new Vector2(0f, 0f);
            hintRt.anchorMax = new Vector2(1f, 0f);
            hintRt.pivot = new Vector2(0.5f, 0f);
            hintRt.anchoredPosition = new Vector2(0f, 12f);
            hintRt.sizeDelta = new Vector2(-40f, 24f);
            var hint = hintGo.AddComponent<TextMeshProUGUI>();
            hint.fontSize = 16f;
            hint.alignment = TextAlignmentOptions.Center;
            hint.color = new Color(0.55f, 0.58f, 0.65f, 1f);
            hint.text = "[Tab] Close   ·   [Esc] Close";
            hint.raycastTarget = false;
        }

        if (gridRoot == null)
        {
            Transform existingGrid = root.transform.Find("SlotGrid");
            if (existingGrid != null)
            {
                gridRoot = existingGrid.GetComponent<RectTransform>();
            }
            else
            {
                var gridGo = CreateRect("SlotGrid", root.transform);
                gridRoot = gridGo.GetComponent<RectTransform>();
                gridRoot.anchorMin = new Vector2(0f, 0f);
                gridRoot.anchorMax = new Vector2(1f, 1f);
                gridRoot.offsetMin = new Vector2(28f, 48f);
                gridRoot.offsetMax = new Vector2(-28f, -96f);

                var grid = gridGo.AddComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(72f, 72f);
                grid.spacing = new Vector2(10f, 10f);
                grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
                grid.startAxis = GridLayoutGroup.Axis.Horizontal;
                grid.childAlignment = TextAnchor.UpperCenter;
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = Mathf.Max(1, columns);
            }
        }

        if (tooltipRoot == null)
        {
            // Parent to canvas so tooltip can follow the cursor outside the panel.
            var tipGo = CreateRect("Tooltip", canvasGo.transform);
            tooltipRoot = tipGo.GetComponent<RectTransform>();
            tooltipRoot.anchorMin = new Vector2(0.5f, 0.5f);
            tooltipRoot.anchorMax = new Vector2(0.5f, 0.5f);
            tooltipRoot.pivot = new Vector2(0f, 1f);
            tooltipRoot.sizeDelta = new Vector2(240f, 0f);

            var tipBg = tipGo.AddComponent<Image>();
            tipBg.color = new Color(0.05f, 0.06f, 0.08f, 0.96f);
            tipBg.raycastTarget = false;

            var vlg = tipGo.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(12, 12, 10, 10);
            vlg.spacing = 4f;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var fitter = tipGo.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var titleGo = CreateRect("TooltipTitle", tipGo.transform);
            tooltipTitleText = titleGo.AddComponent<TextMeshProUGUI>();
            tooltipTitleText.fontSize = 20f;
            tooltipTitleText.fontStyle = FontStyles.Bold;
            tooltipTitleText.color = new Color(1f, 0.88f, 0.55f, 1f);
            tooltipTitleText.alignment = TextAlignmentOptions.Left;
            tooltipTitleText.raycastTarget = false;
            tooltipTitleText.textWrappingMode = TextWrappingModes.NoWrap;
            var titleLe = titleGo.AddComponent<LayoutElement>();
            titleLe.minWidth = 160f;

            var bodyGo = CreateRect("TooltipBody", tipGo.transform);
            tooltipBodyText = bodyGo.AddComponent<TextMeshProUGUI>();
            tooltipBodyText.fontSize = 16f;
            tooltipBodyText.color = new Color(0.85f, 0.87f, 0.92f, 1f);
            tooltipBodyText.alignment = TextAlignmentOptions.Left;
            tooltipBodyText.raycastTarget = false;
            tooltipBodyText.textWrappingMode = TextWrappingModes.Normal;
            var bodyLe = bodyGo.AddComponent<LayoutElement>();
            bodyLe.preferredWidth = 220f;

            tipGo.SetActive(false);
        }
    }

    public static InventorySlotUI BuildSlotRuntime(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.18f, 0.19f, 0.23f, 1f);
        bg.raycastTarget = true;

        var slot = go.AddComponent<InventorySlotUI>();
        slot.backgroundImage = bg;

        var iconGo = CreateRect("Icon", go.transform);
        var iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.5f);
        iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.sizeDelta = new Vector2(48f, 48f);
        var icon = iconGo.AddComponent<Image>();
        icon.raycastTarget = false;
        icon.preserveAspect = true;
        slot.iconImage = icon;

        var countGo = CreateRect("Count", go.transform);
        var countRt = countGo.GetComponent<RectTransform>();
        countRt.anchorMin = new Vector2(1f, 0f);
        countRt.anchorMax = new Vector2(1f, 0f);
        countRt.pivot = new Vector2(1f, 0f);
        countRt.anchoredPosition = new Vector2(-4f, 2f);
        countRt.sizeDelta = new Vector2(48f, 22f);
        var count = countGo.AddComponent<TextMeshProUGUI>();
        count.fontSize = 16f;
        count.fontStyle = FontStyles.Bold;
        count.alignment = TextAlignmentOptions.BottomRight;
        count.color = Color.white;
        count.raycastTarget = false;
        count.textWrappingMode = TextWrappingModes.NoWrap;
        slot.countText = count;
        countGo.SetActive(false);

        return slot;
    }

    static GameObject CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }
}
