using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bottom-left stacked "+N Item" popups. Multiple can show at once;
/// when a lower entry fades out, ones above slide down.
/// </summary>
public class PickupPopupUI : MonoBehaviour
{
    public static PickupPopupUI Instance { get; private set; }

    [Header("Timing")]
    public float fadeInSeconds = 0.15f;
    public float holdSeconds = 1.5f;
    public float fadeOutSeconds = 0.5f;

    [Header("Layout")]
    public float lineHeight = 36f;
    public float lineSpacing = 4f;
    public int maxVisible = 8;

    RectTransform _stackRoot;
    readonly List<PopupEntry> _entries = new List<PopupEntry>();

    class PopupEntry
    {
        public GameObject go;
        public RectTransform rect;
        public CanvasGroup group;
        public TMP_Text text;
        public Coroutine life;
        public float targetY;
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
        if (Instance == this)
            Instance = null;
    }

    public void ShowPopup(string itemName, int amount)
    {
        if (string.IsNullOrWhiteSpace(itemName))
            itemName = "item";

        EnsureUi();

        // Cap stack size: drop the oldest (bottom) if we overflow.
        while (_entries.Count >= Mathf.Max(1, maxVisible))
        {
            PopupEntry oldest = _entries[0];
            if (oldest.life != null)
                StopCoroutine(oldest.life);
            DestroyEntry(oldest);
        }

        PopupEntry entry = CreateEntry($"+{Mathf.Max(1, amount)} {itemName}");
        _entries.Add(entry);
        RelayoutImmediate();
        entry.life = StartCoroutine(RunEntryLife(entry));
    }

    IEnumerator RunEntryLife(PopupEntry entry)
    {
        // Fade in
        yield return FadeEntry(entry, 0f, 1f, fadeInSeconds);
        // Hold
        yield return new WaitForSecondsRealtime(holdSeconds);
        // Fade out
        yield return FadeEntry(entry, entry.group != null ? entry.group.alpha : 1f, 0f, fadeOutSeconds);

        DestroyEntry(entry);
        RelayoutAnimated();
    }

    IEnumerator FadeEntry(PopupEntry entry, float from, float to, float duration)
    {
        if (entry == null || entry.group == null)
            yield break;

        float length = Mathf.Max(0.01f, duration);
        float elapsed = 0f;
        entry.group.alpha = from;
        while (elapsed < length)
        {
            if (entry.go == null)
                yield break;
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / length);
            entry.group.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        if (entry.group != null)
            entry.group.alpha = to;
    }

    void DestroyEntry(PopupEntry entry)
    {
        if (entry == null)
            return;

        _entries.Remove(entry);
        if (entry.go != null)
            Destroy(entry.go);
    }

    void RelayoutImmediate()
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            float y = 24f + i * (lineHeight + lineSpacing);
            _entries[i].targetY = y;
            if (_entries[i].rect != null)
            {
                Vector2 pos = _entries[i].rect.anchoredPosition;
                pos.y = y;
                _entries[i].rect.anchoredPosition = pos;
            }
        }
    }

    void RelayoutAnimated()
    {
        // After a bottom entry is removed, slide remaining entries down to fill the gap.
        StartCoroutine(AnimateLayout());
    }

    IEnumerator AnimateLayout()
    {
        // Index 0 = bottom (oldest remaining / lowest), higher index = stacked on top.
        for (int i = 0; i < _entries.Count; i++)
            _entries[i].targetY = 24f + i * (lineHeight + lineSpacing);

        float duration = 0.2f;
        float elapsed = 0f;
        var starts = new float[_entries.Count];
        for (int i = 0; i < _entries.Count; i++)
            starts[i] = _entries[i].rect != null ? _entries[i].rect.anchoredPosition.y : _entries[i].targetY;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            // Clamp to starts.Length — new pickups can grow _entries mid-animation.
            int count = Mathf.Min(_entries.Count, starts.Length);
            for (int i = 0; i < count; i++)
            {
                if (_entries[i].rect == null)
                    continue;
                Vector2 pos = _entries[i].rect.anchoredPosition;
                pos.y = Mathf.Lerp(starts[i], _entries[i].targetY, t);
                _entries[i].rect.anchoredPosition = pos;
            }

            yield return null;
        }

        RelayoutImmediate();
    }

    PopupEntry CreateEntry(string label)
    {
        var go = new GameObject("PickupLine", typeof(RectTransform));
        go.transform.SetParent(_stackRoot, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(24f, 24f + _entries.Count * (lineHeight + lineSpacing));
        rt.sizeDelta = new Vector2(420f, lineHeight);

        var group = go.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        var text = go.AddComponent<TextMeshProUGUI>();
        text.fontSize = 28f;
        text.fontStyle = FontStyles.Bold;
        text.color = new Color(1f, 0.92f, 0.55f, 1f);
        text.alignment = TextAlignmentOptions.BottomLeft;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.text = label;

        return new PopupEntry
        {
            go = go,
            rect = rt,
            group = group,
            text = text,
            targetY = rt.anchoredPosition.y
        };
    }

    void EnsureUi()
    {
        if (_stackRoot != null)
            return;

        Transform canvasTf = transform.Find("PickupPopupCanvas");
        GameObject canvasGo;
        if (canvasTf == null)
        {
            canvasGo = new GameObject("PickupPopupCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 35;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();
        }
        else
        {
            canvasGo = canvasTf.gameObject;
        }

        // Hide legacy single-line popup if an older build left one around.
        Transform legacy = canvasGo.transform.Find("PopupText");
        if (legacy != null)
            legacy.gameObject.SetActive(false);

        Transform stackTf = canvasGo.transform.Find("StackRoot");
        if (stackTf == null)
        {
            var stackGo = new GameObject("StackRoot", typeof(RectTransform));
            stackGo.transform.SetParent(canvasGo.transform, false);
            _stackRoot = stackGo.GetComponent<RectTransform>();
        }
        else
        {
            _stackRoot = stackTf.GetComponent<RectTransform>();
        }

        StretchFull(_stackRoot);
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
