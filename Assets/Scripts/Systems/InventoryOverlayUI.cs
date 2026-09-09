using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Full-screen inventory overlay (Tab). Separate from the always-visible bottom-left InventoryUI.
/// Blocks player movement while open, but does not pause time / PauseManager.
/// </summary>
public class InventoryOverlayUI : MonoBehaviour
{
    public static InventoryOverlayUI Instance { get; private set; }

    [Header("Input")]
    public Key toggleKey = Key.Tab;

    [Header("UI")]
    public GameObject root;
    public TMP_Text titleText;
    public TMP_Text uniqueCountText;
    public RectTransform listContent;

    public bool IsOpen { get; private set; }

    InventorySystem _inventory;
    readonly List<TMP_Text> _rowPool = new List<TMP_Text>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureUi();
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
        if (kb == null)
            return;

        if (!kb[toggleKey].wasPressedThisFrame)
            return;

        // Don't open over the pause menu; still allow closing if somehow open while paused.
        if (!IsOpen && PauseManager.Instance != null && PauseManager.Instance.IsPaused)
            return;

        Toggle();
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

        int unique = 0;
        int row = 0;

        if (_inventory != null)
        {
            foreach (var pair in _inventory.Items)
            {
                if (pair.Key == null || pair.Value <= 0)
                    continue;

                unique++;
                string itemName = string.IsNullOrEmpty(pair.Key.itemName) ? pair.Key.name : pair.Key.itemName;
                string line = $"{itemName}  ·  {pair.Key.category}  ·  x{pair.Value}";
                GetOrCreateRow(row).text = line;
                GetOrCreateRow(row).gameObject.SetActive(true);
                row++;
            }
        }

        for (int i = row; i < _rowPool.Count; i++)
            _rowPool[i].gameObject.SetActive(false);

        if (uniqueCountText != null)
            uniqueCountText.text = unique == 0
                ? "0 unique items"
                : $"{unique} unique item{(unique == 1 ? "" : "s")}";

        if (row == 0)
        {
            TMP_Text empty = GetOrCreateRow(0);
            empty.text = "(empty)";
            empty.gameObject.SetActive(true);
        }

        if (listContent != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(listContent);
    }

    TMP_Text GetOrCreateRow(int index)
    {
        while (_rowPool.Count <= index)
        {
            var go = new GameObject($"ItemRow_{_rowPool.Count}", typeof(RectTransform));
            go.transform.SetParent(listContent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);

            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 32f;
            le.preferredHeight = 32f;
            le.flexibleWidth = 1f;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 22f;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.enableWordWrapping = false;
            tmp.raycastTarget = false;
            tmp.text = "";

            _rowPool.Add(tmp);
        }

        return _rowPool[index];
    }

    void EnsureUi()
    {
        if (root != null && listContent != null && titleText != null && uniqueCountText != null)
            return;

        // Own overlay canvas so sorting stays above GameUI but below PauseCanvas.
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
        }
        else
        {
            canvasGo = canvasTf.gameObject;
        }

        if (root == null)
        {
            var panel = CreateRect("Panel", canvasGo.transform);
            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(560f, 520f);
            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0.07f, 0.08f, 0.11f, 0.94f);
            root = panel;
        }

        if (titleText == null)
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

        if (uniqueCountText == null)
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

        // Hint line
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

        if (listContent == null)
        {
            var scrollGo = CreateRect("ScrollView", root.transform);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0f, 0f);
            scrollRt.anchorMax = new Vector2(1f, 1f);
            scrollRt.offsetMin = new Vector2(20f, 44f);
            scrollRt.offsetMax = new Vector2(-20f, -96f);

            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            var viewport = CreateRect("Viewport", scrollGo.transform);
            var viewportRt = viewport.GetComponent<RectTransform>();
            StretchFull(viewportRt);
            viewport.AddComponent<RectMask2D>();
            var viewportImg = viewport.AddComponent<Image>();
            viewportImg.color = new Color(1f, 1f, 1f, 0.02f);

            var content = CreateRect("Content", viewport.transform);
            listContent = content.GetComponent<RectTransform>();
            listContent.anchorMin = new Vector2(0f, 1f);
            listContent.anchorMax = new Vector2(1f, 1f);
            listContent.pivot = new Vector2(0.5f, 1f);
            listContent.anchoredPosition = Vector2.zero;
            listContent.sizeDelta = new Vector2(0f, 0f);

            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.spacing = 4f;
            vlg.padding = new RectOffset(8, 8, 4, 4);

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewportRt;
            scroll.content = listContent;
        }
    }

    static GameObject CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }
}
