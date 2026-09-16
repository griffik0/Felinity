using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Always-visible bottom-center 7-slot toolbar HUD with item icons + selection ring.
/// </summary>
public class ToolbarUI : MonoBehaviour
{
    public static ToolbarUI Instance { get; private set; }

    [Header("Layout")]
    public float slotSize = 64f;
    public float slotSpacing = 8f;

    RectTransform _row;
    readonly List<ToolbarSlotView> _views = new List<ToolbarSlotView>();
    ToolbarSelection _selection;
    InventorySystem _inventory;

    class ToolbarSlotView
    {
        public RectTransform root;
        public Image background;
        public Image icon;
        public Image highlight;
        public TMP_Text countText;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureUi();
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
        Refresh();
    }

    void OnDisable()
    {
        Unbind();
    }

    void Start()
    {
        Bind();
        Refresh();
    }

    void Bind()
    {
        _inventory = InventorySystem.Instance;
        if (_inventory != null)
        {
            _inventory.OnInventoryChanged -= Refresh;
            _inventory.OnInventoryChanged += Refresh;
        }

        _selection = ToolbarSelection.Instance;
        if (_selection == null)
            _selection = Object.FindAnyObjectByType<ToolbarSelection>();

        if (_selection != null)
        {
            _selection.OnSelectedSlotChanged -= HandleSelectionChanged;
            _selection.OnSelectedSlotChanged += HandleSelectionChanged;
        }
    }

    void Unbind()
    {
        if (_inventory != null)
            _inventory.OnInventoryChanged -= Refresh;

        if (_selection != null)
            _selection.OnSelectedSlotChanged -= HandleSelectionChanged;
    }

    void HandleSelectionChanged(int _)
    {
        RefreshSelection();
    }

    public void Refresh()
    {
        EnsureUi();
        Bind();

        int selected = _selection != null ? _selection.CurrentSlotIndex : 0;
        var slots = _inventory != null ? _inventory.ToolbarSlots : null;

        for (int i = 0; i < _views.Count; i++)
        {
            ToolbarSlotView view = _views[i];
            InventorySlotEntry entry = slots != null && i < slots.Count ? slots[i] : null;

            bool filled = entry != null && !entry.IsEmpty;
            if (filled)
            {
                view.background.color = new Color(0.28f, 0.30f, 0.36f, 0.95f);

                if (view.icon != null)
                {
                    if (entry.item.icon != null)
                    {
                        view.icon.enabled = true;
                        view.icon.sprite = entry.item.icon;
                        view.icon.color = Color.white;
                        view.icon.preserveAspect = true;
                    }
                    else
                    {
                        view.icon.enabled = true;
                        view.icon.sprite = InventorySlotUI.GetFallbackIcon();
                        view.icon.color = new Color(0.35f, 0.37f, 0.42f, 1f);
                        view.icon.preserveAspect = true;
                    }
                }

                if (view.countText != null)
                {
                    bool show = entry.count >= 1;
                    view.countText.gameObject.SetActive(show);
                    if (show)
                        view.countText.text = $"x{entry.count}";
                }
            }
            else
            {
                view.background.color = new Color(0.16f, 0.17f, 0.20f, 0.92f);

                if (view.icon != null)
                {
                    view.icon.sprite = null;
                    view.icon.enabled = false;
                }

                if (view.countText != null)
                {
                    view.countText.text = string.Empty;
                    view.countText.gameObject.SetActive(false);
                }
            }

            if (view.highlight != null)
                view.highlight.enabled = i == selected;
        }
    }

    void RefreshSelection()
    {
        int selected = _selection != null ? _selection.CurrentSlotIndex : 0;
        for (int i = 0; i < _views.Count; i++)
        {
            if (_views[i].highlight != null)
                _views[i].highlight.enabled = i == selected;
        }
    }

    void EnsureUi()
    {
        if (_row != null && _views.Count == InventorySystem.ToolbarSize)
            return;

        Transform canvasTf = transform.Find("ToolbarCanvas");
        GameObject canvasGo;
        if (canvasTf == null)
        {
            canvasGo = new GameObject("ToolbarCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();
        }
        else
        {
            canvasGo = canvasTf.gameObject;
        }

        Transform rowTf = canvasGo.transform.Find("ToolbarRow");
        if (rowTf == null)
        {
            var rowGo = new GameObject("ToolbarRow", typeof(RectTransform));
            rowGo.transform.SetParent(canvasGo.transform, false);
            _row = rowGo.GetComponent<RectTransform>();
            _row.anchorMin = new Vector2(0.5f, 0f);
            _row.anchorMax = new Vector2(0.5f, 0f);
            _row.pivot = new Vector2(0.5f, 0f);
            _row.anchoredPosition = new Vector2(0f, 28f);

            float width = InventorySystem.ToolbarSize * slotSize + (InventorySystem.ToolbarSize - 1) * slotSpacing;
            _row.sizeDelta = new Vector2(width, slotSize);

            var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = slotSpacing;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
        }
        else
        {
            _row = rowTf.GetComponent<RectTransform>();
        }

        while (_views.Count < InventorySystem.ToolbarSize)
            _views.Add(CreateSlotView(_views.Count));
    }

    ToolbarSlotView CreateSlotView(int index)
    {
        var go = new GameObject($"ToolbarSlot_{index}", typeof(RectTransform));
        go.transform.SetParent(_row, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(slotSize, slotSize);

        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = slotSize;
        le.preferredHeight = slotSize;
        le.minWidth = slotSize;
        le.minHeight = slotSize;
        le.flexibleWidth = 0f;
        le.flexibleHeight = 0f;

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.16f, 0.17f, 0.20f, 0.92f);
        bg.raycastTarget = false;

        var highlightGo = new GameObject("Highlight", typeof(RectTransform));
        highlightGo.transform.SetParent(go.transform, false);
        var hRt = highlightGo.GetComponent<RectTransform>();
        hRt.anchorMin = Vector2.zero;
        hRt.anchorMax = Vector2.one;
        hRt.offsetMin = new Vector2(-3f, -3f);
        hRt.offsetMax = new Vector2(3f, 3f);
        var highlight = highlightGo.AddComponent<Image>();
        highlight.color = new Color(1f, 0.88f, 0.45f, 1f);
        highlight.raycastTarget = false;
        highlightGo.transform.SetAsFirstSibling();
        highlight.enabled = false;

        var fillGo = new GameObject("Fill", typeof(RectTransform));
        fillGo.transform.SetParent(go.transform, false);
        var fRt = fillGo.GetComponent<RectTransform>();
        fRt.anchorMin = Vector2.zero;
        fRt.anchorMax = Vector2.one;
        fRt.offsetMin = new Vector2(3f, 3f);
        fRt.offsetMax = new Vector2(-3f, -3f);
        var fill = fillGo.AddComponent<Image>();
        fill.color = new Color(0.16f, 0.17f, 0.20f, 0.92f);
        fill.raycastTarget = false;

        var iconGo = new GameObject("Icon", typeof(RectTransform));
        iconGo.transform.SetParent(go.transform, false);
        var iRt = iconGo.GetComponent<RectTransform>();
        iRt.anchorMin = new Vector2(0.5f, 0.5f);
        iRt.anchorMax = new Vector2(0.5f, 0.5f);
        iRt.pivot = new Vector2(0.5f, 0.5f);
        iRt.sizeDelta = new Vector2(slotSize * 0.62f, slotSize * 0.62f);
        var icon = iconGo.AddComponent<Image>();
        icon.raycastTarget = false;
        icon.preserveAspect = true;
        icon.enabled = false;

        var countGo = new GameObject("Count", typeof(RectTransform));
        countGo.transform.SetParent(go.transform, false);
        var cRt = countGo.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(1f, 0f);
        cRt.anchorMax = new Vector2(1f, 0f);
        cRt.pivot = new Vector2(1f, 0f);
        cRt.anchoredPosition = new Vector2(-4f, 2f);
        cRt.sizeDelta = new Vector2(48f, 20f);
        var count = countGo.AddComponent<TextMeshProUGUI>();
        count.fontSize = 15f;
        count.fontStyle = FontStyles.Bold;
        count.alignment = TextAlignmentOptions.BottomRight;
        count.color = Color.white;
        count.raycastTarget = false;
        count.textWrappingMode = TextWrappingModes.NoWrap;
        countGo.SetActive(false);

        bg.enabled = false;

        return new ToolbarSlotView
        {
            root = rt,
            background = fill,
            icon = icon,
            highlight = highlight,
            countText = count
        };
    }
}
