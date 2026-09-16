using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Character Creation UI: coat left/right cycle, name field, confirm → MainWorld.
/// </summary>
public class CharacterCreationUI : MonoBehaviour
{
    [Header("Preview")]
    public CatAppearance previewAppearance;
    public PlayerFacing previewFacing;

    [Header("Coat cycling")]
    public Button coatPrevButton;
    public Button coatNextButton;
    public Image coatSwatchImage;
    public TMP_Text coatNameText;

    [Header("Name")]
    public TMP_InputField nameField;
    public string defaultName = PlayerProfile.DefaultCatName;
    public int maxNameLength = PlayerProfile.MaxCatNameLength;

    [Header("Confirm")]
    public Button confirmButton;

    PlayerProfile _profile;
    int _selectedCoatIndex;

    void Awake()
    {
        _profile = PlayerProfile.EnsureExists();

        if (nameField != null)
        {
            nameField.characterLimit = maxNameLength;
            if (nameField.placeholder is TMP_Text placeholder)
                placeholder.text = defaultName;

            nameField.text = _profile.hasCustomization && !string.IsNullOrWhiteSpace(_profile.catName)
                ? _profile.catName
                : string.Empty;

            nameField.onValueChanged.RemoveListener(OnNameChanged);
            nameField.onValueChanged.AddListener(OnNameChanged);
        }

        if (coatPrevButton != null)
        {
            coatPrevButton.onClick.RemoveListener(OnCoatPrev);
            coatPrevButton.onClick.AddListener(OnCoatPrev);
        }

        if (coatNextButton != null)
        {
            coatNextButton.onClick.RemoveListener(OnCoatNext);
            coatNextButton.onClick.AddListener(OnCoatNext);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(OnConfirm);
            confirmButton.onClick.AddListener(OnConfirm);
        }
    }

    void Start()
    {
        int startIndex = _profile.hasCustomization ? _profile.chosenCoatIndex : 0;
        if (previewAppearance != null && previewAppearance.coatPresets != null &&
            startIndex >= previewAppearance.coatPresets.Length)
            startIndex = 0;

        SelectCoat(startIndex);

        if (nameField != null)
            OnNameChanged(nameField.text);

        RefreshConfirmInteractable();
    }

    public void OnCoatPrev()
    {
        int count = CoatCount;
        if (count <= 0)
            return;
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayButtonClick();
        SelectCoat((_selectedCoatIndex - 1 + count) % count);
    }

    public void OnCoatNext()
    {
        int count = CoatCount;
        if (count <= 0)
            return;
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayButtonClick();
        SelectCoat((_selectedCoatIndex + 1) % count);
    }

    int CoatCount =>
        previewAppearance != null && previewAppearance.coatPresets != null
            ? previewAppearance.coatPresets.Length
            : 0;

    public void SelectCoat(int index)
    {
        if (previewAppearance == null || previewAppearance.coatPresets == null)
            return;
        if (index < 0 || index >= previewAppearance.coatPresets.Length)
            return;

        _selectedCoatIndex = index;
        CatAppearance.CoatPreset preset = previewAppearance.coatPresets[index];
        previewAppearance.ApplyCoat(preset);
        _profile.SetCoat(preset, index);
        RefreshCoatDisplay(preset);
    }

    void RefreshCoatDisplay(CatAppearance.CoatPreset preset)
    {
        if (coatNameText != null)
            coatNameText.text = preset != null ? preset.name : string.Empty;

        if (coatSwatchImage != null && preset != null)
            coatSwatchImage.color = preset.replace0;
    }

    void OnNameChanged(string value)
    {
        string sanitized = PlayerProfile.SanitizeName(value);
        _profile.SetCatName(sanitized);
        RefreshConfirmInteractable();
    }

    void RefreshConfirmInteractable()
    {
        if (confirmButton == null)
            return;

        bool hasName = !string.IsNullOrWhiteSpace(nameField != null ? nameField.text : _profile.catName);
        confirmButton.interactable = hasName;
    }

    public void OnConfirm()
    {
        if (nameField != null)
            _profile.SetCatName(nameField.text);

        if (string.IsNullOrWhiteSpace(_profile.catName))
            return;

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayButtonClick();

        if (previewAppearance != null && previewAppearance.coatPresets != null &&
            _selectedCoatIndex >= 0 && _selectedCoatIndex < previewAppearance.coatPresets.Length)
        {
            _profile.SetCoat(previewAppearance.coatPresets[_selectedCoatIndex], _selectedCoatIndex);
        }

        GameFlow.LoadMainWorld();
    }
}
