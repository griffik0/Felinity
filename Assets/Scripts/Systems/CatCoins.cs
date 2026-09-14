using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Static CatCoin wallet with a live UI counter.
/// </summary>
public static class CatCoins
{
    public static event Action<int> OnBalanceChanged;

    static int _balance;

    public static int CatCoinBalance
    {
        get => _balance;
        set
        {
            if (_balance == value)
                return;

            _balance = value;
            OnBalanceChanged?.Invoke(_balance);
        }
    }

    public static void Add(int amount)
    {
        CatCoinBalance += amount;
    }
}

/// <summary>
/// Top-right HUD: CatCoin icon + live balance number.
/// </summary>
public class CatCoinsUI : MonoBehaviour
{
    [Header("UI")]
    public Image iconImage;
    public TMP_Text balanceText;
    public Sprite coinSprite;

    void Awake()
    {
        EnsureUi();
    }

    void OnEnable()
    {
        EnsureUi();
        CatCoins.OnBalanceChanged += Refresh;
        Refresh(CatCoins.CatCoinBalance);
    }

    void OnDisable()
    {
        CatCoins.OnBalanceChanged -= Refresh;
    }

    void Refresh(int balance)
    {
        if (balanceText != null)
            balanceText.text = balance.ToString();
    }

    void EnsureUi()
    {
        if (iconImage != null && balanceText != null)
        {
            if (iconImage.sprite == null && coinSprite != null)
                iconImage.sprite = coinSprite;
            return;
        }

        // Migrate old text-only root into icon + number layout at runtime once.
        TMP_Text legacyRootText = GetComponent<TMP_Text>();
        float fontSize = legacyRootText != null ? legacyRootText.fontSize : 28f;
        Color fontColor = legacyRootText != null ? legacyRootText.color : Color.white;
        TMP_FontAsset font = legacyRootText != null ? legacyRootText.font : null;
        if (legacyRootText != null)
            legacyRootText.enabled = false;

        var rootRt = GetComponent<RectTransform>();
        if (rootRt != null)
        {
            rootRt.anchorMin = new Vector2(1f, 1f);
            rootRt.anchorMax = new Vector2(1f, 1f);
            rootRt.pivot = new Vector2(1f, 1f);
            rootRt.anchoredPosition = new Vector2(-24f, -24f);
            rootRt.sizeDelta = new Vector2(180f, 40f);
        }

        var layout = GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
            layout = gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleRight;
        layout.spacing = 8f;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var fitter = GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        if (iconImage == null)
        {
            Transform iconTf = transform.Find("Icon");
            GameObject iconGo;
            if (iconTf == null)
            {
                iconGo = new GameObject("Icon", typeof(RectTransform));
                iconGo.transform.SetParent(transform, false);
            }
            else
            {
                iconGo = iconTf.gameObject;
            }

            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.sizeDelta = new Vector2(32f, 32f);
            iconImage = iconGo.GetComponent<Image>();
            if (iconImage == null)
                iconImage = iconGo.AddComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            iconImage.color = Color.white;

            var iconLe = iconGo.GetComponent<LayoutElement>();
            if (iconLe == null)
                iconLe = iconGo.AddComponent<LayoutElement>();
            iconLe.preferredWidth = 32f;
            iconLe.preferredHeight = 32f;
            iconLe.minWidth = 32f;
            iconLe.minHeight = 32f;
        }

        if (iconImage.sprite == null && coinSprite != null)
            iconImage.sprite = coinSprite;

        if (balanceText == null)
        {
            Transform textTf = transform.Find("BalanceText");
            GameObject textGo;
            if (textTf == null)
            {
                textGo = new GameObject("BalanceText", typeof(RectTransform));
                textGo.transform.SetParent(transform, false);
            }
            else
            {
                textGo = textTf.gameObject;
            }

            var textRt = textGo.GetComponent<RectTransform>();
            textRt.sizeDelta = new Vector2(100f, 36f);
            balanceText = textGo.GetComponent<TextMeshProUGUI>();
            if (balanceText == null)
                balanceText = textGo.AddComponent<TextMeshProUGUI>();
            if (font != null)
                balanceText.font = font;
            balanceText.fontSize = fontSize;
            balanceText.color = fontColor;
            balanceText.alignment = TextAlignmentOptions.MidlineLeft;
            balanceText.raycastTarget = false;
            balanceText.textWrappingMode = TextWrappingModes.NoWrap;
            balanceText.text = CatCoins.CatCoinBalance.ToString();

            var textLe = textGo.GetComponent<LayoutElement>();
            if (textLe == null)
                textLe = textGo.AddComponent<LayoutElement>();
            textLe.minWidth = 24f;
            textLe.preferredHeight = 36f;
        }

        if (iconImage != null)
            iconImage.transform.SetSiblingIndex(0);
        if (balanceText != null)
            balanceText.transform.SetSiblingIndex(1);
    }
}
