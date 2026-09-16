using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Full-screen inventory overlay (Tab): toolbar strip + general bag grid.
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
    public RectTransform toolbarRoot;
    public RectTransform gridRoot;
    public RectTransform tooltipRoot;
    public TMP_Text tooltipTitleText;
    public TMP_Text tooltipBodyText;

    public bool IsOpen { get; private set; }

    InventorySystem _inventory;
    readonly List<InventorySlotUI> _toolbarSlots = new List<InventorySlotUI>();
    readonly List<InventorySlotUI> _generalSlots = new List<InventorySlotUI>();
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
            if (!IsOpen && PauseManager.Instance != null && PauseManager.Instance.IsPaused)
                return;

            Toggle();
        }

        if (_tooltipOwner != null)
            UpdateTooltipPosition();
    }

    public void Toggle() => SetOpen(!IsOpen);
    public void Open() => SetOpen(true);
    public void Close() => SetOpen(false);

    public void SetOpen(bool open, bool refresh = true)
    {
        bool changed = IsOpen != open;
        IsOpen = open;

        if (root != null)
            root.SetActive(open);

        if (!open)
            HideTooltip(null);

        if (open && refresh)
            Refresh();

        if (changed && SFXManager.Instance != null)
        {
            if (open)
                SFXManager.Instance.PlayInventoryOpen();
            else
                SFXManager.Instance.PlayInventoryClose();
        }
    }

    void Bind()
    {
        if (_inventory == null)
            _inventory = InventorySystem.Instance;

        if (_inventory != null)
        {
            _inventory.OnInventoryChanged -= Refresh;
            _inventory.OnInventoryChanged += Refresh;
        }
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

        // Toolbar strip (no equipped/selection highlight in the overlay).
        var toolbar = _inventory != null ? _inventory.ToolbarSlots : null;
        for (int i = 0; i < _toolbarSlots.Count; i++)
        {
            InventorySlotEntry entry = toolbar != null && i < toolbar.Count ? toolbar[i] : null;
            if (entry != null && !entry.IsEmpty)
                _toolbarSlots[i].SetItem(entry.item, entry.count);
            else
                _toolbarSlots[i].Clear();
        }

        // General bag only
        var general = _inventory != null ? _inventory.GeneralSlots : null;
        int filledGeneral = 0;
        for (int i = 0; i < _generalSlots.Count; i++)
        {
            InventorySlotEntry entry = general != null && i < general.Count ? general[i] : null;
            if (entry != null && !entry.IsEmpty)
            {
                _generalSlots[i].SetItem(entry.item, entry.count);
                filledGeneral++;
            }
            else
            {
                _generalSlots[i].Clear();
            }
        }

        int unique = _inventory != null ? _inventory.Items.Count : 0;
        if (uniqueCountText != null)
        {
            uniqueCountText.text = unique == 0
                ? "0 unique items"
                : $"{unique} unique item{(unique == 1 ? "" : "s")}  ·  bag {filledGeneral}/{InventorySystem.GeneralSize}";
        }
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

        tooltipRoot.anchoredPosition = local + new Vector2(18f, -18f);
    }

    void EnsureSlots()
    {
        const float slotSize = 72f;

        // Prefer the row under ToolbarSection so layout sizing applies.
        if (toolbarRoot != null && toolbarRoot.name == "ToolbarSection")
        {
            Transform row = toolbarRoot.Find("ToolbarRow");
            if (row != null)
                toolbarRoot = row as RectTransform;
        }

        while (_toolbarSlots.Count < InventorySystem.ToolbarSize)
        {
            int i = _toolbarSlots.Count;
            InventorySlotUI slot = CreateSlot($"ToolbarSlot_{i:00}", toolbarRoot);
            _toolbarSlots.Add(slot);
        }

        for (int i = 0; i < _toolbarSlots.Count; i++)
        {
            InventorySlotUI slot = _toolbarSlots[i];
            NormalizeToolbarSlot(slot, slotSize);
            slot.BindOverlay(this);
        }

        int generalCapacity = Mathf.Max(1, columns) * Mathf.Max(1, rows);
        generalCapacity = Mathf.Max(generalCapacity, InventorySystem.GeneralSize);
        while (_generalSlots.Count < generalCapacity)
        {
            InventorySlotUI slot = CreateSlot($"BagSlot_{_generalSlots.Count:00}", gridRoot);
            _generalSlots.Add(slot);
        }

        for (int i = 0; i < _generalSlots.Count; i++)
        {
            bool active = i < InventorySystem.GeneralSize;
            _generalSlots[i].gameObject.SetActive(active);
            _generalSlots[i].BindOverlay(this);
        }
    }

    static void NormalizeToolbarSlot(InventorySlotUI slot, float slotSize)
    {
        if (slot == null)
            return;

        // Overlay toolbar must not show equipped/selection rings.
        for (int c = slot.transform.childCount - 1; c >= 0; c--)
        {
            Transform child = slot.transform.GetChild(c);
            if (child.name == "SelectRing" || child.name == "Highlight")
                Object.Destroy(child.gameObject);
        }

        var rt = slot.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(slotSize, slotSize);
            rt.localScale = Vector3.one;
        }

        var le = slot.GetComponent<LayoutElement>();
        if (le == null)
            le = slot.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = slotSize;
        le.preferredHeight = slotSize;
        le.minWidth = slotSize;
        le.minHeight = slotSize;
        le.flexibleWidth = 0f;
        le.flexibleHeight = 0f;
        le.ignoreLayout = false;

        // Keep icon centered and readable at 72px.
        Transform iconTf = slot.transform.Find("Icon");
        if (iconTf != null)
        {
            var iconRt = iconTf as RectTransform;
            if (iconRt != null)
            {
                iconRt.anchorMin = new Vector2(0.5f, 0.5f);
                iconRt.anchorMax = new Vector2(0.5f, 0.5f);
                iconRt.pivot = new Vector2(0.5f, 0.5f);
                iconRt.anchoredPosition = Vector2.zero;
                iconRt.sizeDelta = new Vector2(44f, 44f);
                iconRt.localScale = Vector3.one;
            }

            var iconImg = iconTf.GetComponent<Image>();
            if (iconImg != null)
            {
                iconImg.preserveAspect = true;
                iconImg.type = Image.Type.Simple;
                iconImg.raycastTarget = false;
            }
        }
    }

    InventorySlotUI CreateSlot(string name, Transform parent)
    {
        InventorySlotUI slot;
        if (slotPrefab != null)
        {
            slot = Instantiate(slotPrefab, parent);
            slot.name = name;
        }
        else
        {
            slot = BuildSlotRuntime(name, parent);
        }

        slot.BindOverlay(this);
        slot.Clear();
        return slot;
    }

    void EnsureUi()
    {
        if (root != null && gridRoot != null && toolbarRoot != null &&
            titleText != null && uniqueCountText != null && tooltipRoot != null)
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

        if (root == null)
        {
            Transform existingPanel = canvasGo.transform.Find("Panel");
            if (existingPanel != null)
                root = existingPanel.gameObject;
            else
            {
                var panel = CreateRect("Panel", canvasGo.transform);
                var panelRt = panel.GetComponent<RectTransform>();
                panelRt.anchorMin = new Vector2(0.5f, 0.5f);
                panelRt.anchorMax = new Vector2(0.5f, 0.5f);
                panelRt.pivot = new Vector2(0.5f, 0.5f);
                panelRt.sizeDelta = new Vector2(640f, 620f);
                var bg = panel.AddComponent<Image>();
                bg.color = new Color(0.07f, 0.08f, 0.11f, 0.94f);
                root = panel;
            }
        }

        // Enlarge existing panel a bit for toolbar + bag.
        var rootRt = root.GetComponent<RectTransform>();
        if (rootRt != null && rootRt.sizeDelta.y < 600f)
            rootRt.sizeDelta = new Vector2(Mathf.Max(rootRt.sizeDelta.x, 640f), 620f);

        if (titleText == null)
        {
            Transform titleTf = root.transform.Find("Title");
            if (titleTf != null)
                titleText = titleTf.GetComponent<TMP_Text>();
            else
            {
                var titleGo = CreateRect("Title", root.transform);
                var titleRt = titleGo.GetComponent<RectTransform>();
                titleRt.anchorMin = new Vector2(0f, 1f);
                titleRt.anchorMax = new Vector2(1f, 1f);
                titleRt.pivot = new Vector2(0.5f, 1f);
                titleRt.anchoredPosition = new Vector2(0f, -14f);
                titleRt.sizeDelta = new Vector2(-40f, 36f);
                titleText = titleGo.AddComponent<TextMeshProUGUI>();
                titleText.fontSize = 32f;
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
                uniqueCountText = countTf.GetComponent<TMP_Text>();
            else
            {
                var countGo = CreateRect("UniqueCount", root.transform);
                var countRt = countGo.GetComponent<RectTransform>();
                countRt.anchorMin = new Vector2(0f, 1f);
                countRt.anchorMax = new Vector2(1f, 1f);
                countRt.pivot = new Vector2(0.5f, 1f);
                countRt.anchoredPosition = new Vector2(0f, -48f);
                countRt.sizeDelta = new Vector2(-40f, 24f);
                uniqueCountText = countGo.AddComponent<TextMeshProUGUI>();
                uniqueCountText.fontSize = 18f;
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

        if (toolbarRoot == null)
        {
            Transform existing = root.transform.Find("ToolbarSection");
            if (existing != null)
            {
                Transform row = existing.Find("ToolbarRow");
                toolbarRoot = row != null
                    ? row.GetComponent<RectTransform>()
                    : existing.GetComponent<RectTransform>();
            }
            else
            {
                var section = CreateRect("ToolbarSection", root.transform);
                toolbarRoot = section.GetComponent<RectTransform>();
                toolbarRoot.anchorMin = new Vector2(0f, 1f);
                toolbarRoot.anchorMax = new Vector2(1f, 1f);
                toolbarRoot.pivot = new Vector2(0.5f, 1f);
                toolbarRoot.anchoredPosition = new Vector2(0f, -72f);
                toolbarRoot.sizeDelta = new Vector2(-40f, 78f);

                var labelGo = CreateRect("ToolbarLabel", section.transform);
                var labelRt = labelGo.GetComponent<RectTransform>();
                labelRt.anchorMin = new Vector2(0f, 1f);
                labelRt.anchorMax = new Vector2(1f, 1f);
                labelRt.pivot = new Vector2(0.5f, 1f);
                labelRt.anchoredPosition = Vector2.zero;
                labelRt.sizeDelta = new Vector2(0f, 18f);
                var label = labelGo.AddComponent<TextMeshProUGUI>();
                label.fontSize = 15f;
                label.alignment = TextAlignmentOptions.Center;
                label.color = new Color(0.7f, 0.74f, 0.82f, 1f);
                label.text = "Toolbar";
                label.raycastTarget = false;

                var rowGo = CreateRect("ToolbarRow", section.transform);
                var rowRt = rowGo.GetComponent<RectTransform>();
                rowRt.anchorMin = new Vector2(0.5f, 0f);
                rowRt.anchorMax = new Vector2(0.5f, 0f);
                rowRt.pivot = new Vector2(0.5f, 0f);
                rowRt.anchoredPosition = new Vector2(0f, 2f);
                rowRt.sizeDelta = new Vector2(7 * 72f + 6 * 8f, 72f);
                var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 8f;
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.childControlWidth = true;
                hlg.childControlHeight = true;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;

                // Parent slots under the row, not the section.
                toolbarRoot = rowRt;
            }
        }

        if (gridRoot == null)
        {
            Transform existingGrid = root.transform.Find("SlotGrid");
            if (existingGrid != null)
            {
                gridRoot = existingGrid.GetComponent<RectTransform>();
                // Modest gap under the toolbar strip.
                gridRoot.offsetMax = new Vector2(-28f, -160f);
                gridRoot.offsetMin = new Vector2(28f, 48f);
            }
            else
            {
                var gridGo = CreateRect("SlotGrid", root.transform);
                gridRoot = gridGo.GetComponent<RectTransform>();
                gridRoot.anchorMin = new Vector2(0f, 0f);
                gridRoot.anchorMax = new Vector2(1f, 1f);
                gridRoot.offsetMin = new Vector2(28f, 48f);
                gridRoot.offsetMax = new Vector2(-28f, -160f);

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
        else
        {
            // Existing grid: leave a small gap under the toolbar.
            gridRoot.offsetMax = new Vector2(gridRoot.offsetMax.x, Mathf.Min(gridRoot.offsetMax.y, -160f));
        }

        if (root.transform.Find("BagLabel") == null)
        {
            var bagLabelGo = CreateRect("BagLabel", root.transform);
            var bagRt = bagLabelGo.GetComponent<RectTransform>();
            bagRt.anchorMin = new Vector2(0f, 1f);
            bagRt.anchorMax = new Vector2(1f, 1f);
            bagRt.pivot = new Vector2(0.5f, 1f);
            bagRt.anchoredPosition = new Vector2(0f, -152f);
            bagRt.sizeDelta = new Vector2(-40f, 18f);
            var bagLabel = bagLabelGo.AddComponent<TextMeshProUGUI>();
            bagLabel.fontSize = 15f;
            bagLabel.alignment = TextAlignmentOptions.Center;
            bagLabel.color = new Color(0.7f, 0.74f, 0.82f, 1f);
            bagLabel.text = "Bag";
            bagLabel.raycastTarget = false;
        }

        if (tooltipRoot == null)
        {
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

            var tipTitleGo = CreateRect("TooltipTitle", tipGo.transform);
            tooltipTitleText = tipTitleGo.AddComponent<TextMeshProUGUI>();
            tooltipTitleText.fontSize = 20f;
            tooltipTitleText.fontStyle = FontStyles.Bold;
            tooltipTitleText.color = new Color(1f, 0.88f, 0.55f, 1f);
            tooltipTitleText.alignment = TextAlignmentOptions.Left;
            tooltipTitleText.raycastTarget = false;
            tooltipTitleText.textWrappingMode = TextWrappingModes.NoWrap;

            var tipBodyGo = CreateRect("TooltipBody", tipGo.transform);
            tooltipBodyText = tipBodyGo.AddComponent<TextMeshProUGUI>();
            tooltipBodyText.fontSize = 16f;
            tooltipBodyText.color = new Color(0.85f, 0.87f, 0.92f, 1f);
            tooltipBodyText.alignment = TextAlignmentOptions.Left;
            tooltipBodyText.raycastTarget = false;
            tooltipBodyText.textWrappingMode = TextWrappingModes.Normal;

            tipGo.SetActive(false);
        }
    }

    public static InventorySlotUI BuildSlotRuntime(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 72f;
        le.preferredHeight = 72f;
        le.minWidth = 72f;
        le.minHeight = 72f;

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
        iconRt.sizeDelta = new Vector2(44f, 44f);
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
