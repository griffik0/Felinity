using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Runtime coat recolor via MaterialPropertyBlock + SpritePaletteSwap shader.
/// Debug: press 1-4 to cycle Orange Tabby / Black / Grey / Cream.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class CatAppearance : MonoBehaviour
{
    [Serializable]
    public class CoatPreset
    {
        public string name;
        public Color replace0 = Color.white;
        public Color replace1 = Color.white;
        public Color replace2 = Color.white;
        public Color replace3 = new Color(0f, 0f, 0f, 0f);
    }

    static readonly int Key0Id = Shader.PropertyToID("_KeyColor0");
    static readonly int Key1Id = Shader.PropertyToID("_KeyColor1");
    static readonly int Key2Id = Shader.PropertyToID("_KeyColor2");
    static readonly int Key3Id = Shader.PropertyToID("_KeyColor3");
    static readonly int Replace0Id = Shader.PropertyToID("_ReplaceColor0");
    static readonly int Replace1Id = Shader.PropertyToID("_ReplaceColor1");
    static readonly int Replace2Id = Shader.PropertyToID("_ReplaceColor2");
    static readonly int Replace3Id = Shader.PropertyToID("_ReplaceColor3");
    static readonly int ToleranceId = Shader.PropertyToID("_Tolerance");

    [Header("Source palette keys (sampled from cat art)")]
    public Color keyColor0 = new Color(0.949f, 0.596f, 0.278f, 1f); // #F29847 main orange
    public Color keyColor1 = new Color(0.373f, 0.188f, 0.133f, 1f); // #5F3022 dark brown
    public Color keyColor2 = new Color(0.506f, 0.227f, 0.169f, 1f); // #813A2B mid brown
    public Color keyColor3 = new Color(0f, 0f, 0f, 0f);              // unused

    [Range(0.001f, 0.2f)]
    public float tolerance = 0.05f;

    [Header("Coat presets")]
    public CoatPreset[] coatPresets =
    {
        new CoatPreset
        {
            name = "Orange Tabby",
            replace0 = new Color(0.949f, 0.596f, 0.278f, 1f), // #F29847
            replace1 = new Color(0.373f, 0.188f, 0.133f, 1f), // #5F3022
            replace2 = new Color(0.506f, 0.227f, 0.169f, 1f), // #813A2B
            replace3 = new Color(0f, 0f, 0f, 0f)
        },
        new CoatPreset
        {
            name = "Black",
            replace0 = new Color(0.18f, 0.18f, 0.20f, 1f),
            replace1 = new Color(0.05f, 0.05f, 0.06f, 1f),
            replace2 = new Color(0.10f, 0.10f, 0.11f, 1f),
            replace3 = new Color(0f, 0f, 0f, 0f)
        },
        new CoatPreset
        {
            name = "Grey",
            replace0 = new Color(0.55f, 0.58f, 0.62f, 1f),
            replace1 = new Color(0.28f, 0.30f, 0.34f, 1f),
            replace2 = new Color(0.40f, 0.42f, 0.46f, 1f),
            replace3 = new Color(0f, 0f, 0f, 0f)
        },
        new CoatPreset
        {
            name = "Cream",
            replace0 = new Color(0.93f, 0.84f, 0.68f, 1f),
            replace1 = new Color(0.72f, 0.58f, 0.42f, 1f),
            replace2 = new Color(0.82f, 0.70f, 0.52f, 1f),
            replace3 = new Color(0f, 0f, 0f, 0f)
        }
    };

    public int defaultPresetIndex;

    [Header("Debug")]
    public bool enableDebugHotkeys = true;

    [Tooltip("When false (character-creation preview), Start won't apply PlayerProfile.")]
    public bool applyPlayerProfileOnStart = true;

    SpriteRenderer _renderer;
    MaterialPropertyBlock _block;
    int _activePresetIndex = -1;

    public string ActiveCoatName =>
        _activePresetIndex >= 0 && coatPresets != null && _activePresetIndex < coatPresets.Length
            ? coatPresets[_activePresetIndex].name
            : "(none)";

    void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _block = new MaterialPropertyBlock();
    }

    void Start()
    {
        if (!applyPlayerProfileOnStart)
            return;

        ApplyFromProfile(PlayerProfile.EnsureExists());
    }

    /// <summary>
    /// Applies Character Creation choices (or Orange Tabby default).
    /// </summary>
    public void ApplyFromProfile(PlayerProfile profile)
    {
        if (profile != null && profile.hasCustomization && coatPresets != null && coatPresets.Length > 0)
        {
            ApplyCoat(Mathf.Clamp(profile.chosenCoatIndex, 0, coatPresets.Length - 1));
            return;
        }

        if (profile != null && profile.chosenCoat != null)
        {
            ApplyCoat(PlayerProfile.CloneCoat(profile.chosenCoat));
            return;
        }

        if (coatPresets != null && coatPresets.Length > 0)
            ApplyCoat(Mathf.Clamp(defaultPresetIndex, 0, coatPresets.Length - 1));
    }

    void Update()
    {
        if (!enableDebugHotkeys)
            return;

        Keyboard kb = Keyboard.current;
        if (kb == null || coatPresets == null)
            return;

        if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame)
            ApplyCoat(0);
        else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame)
            ApplyCoat(1);
        else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame)
            ApplyCoat(2);
        else if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame)
            ApplyCoat(3);
    }

    public void ApplyCoat(int presetIndex)
    {
        if (coatPresets == null || presetIndex < 0 || presetIndex >= coatPresets.Length)
            return;

        ApplyCoat(coatPresets[presetIndex]);
        _activePresetIndex = presetIndex;
    }

    public void ApplyCoat(CoatPreset preset)
    {
        if (preset == null || _renderer == null)
            return;

        if (_block == null)
            _block = new MaterialPropertyBlock();

        _renderer.GetPropertyBlock(_block);

        _block.SetColor(Key0Id, keyColor0);
        _block.SetColor(Key1Id, keyColor1);
        _block.SetColor(Key2Id, keyColor2);
        _block.SetColor(Key3Id, keyColor3);

        _block.SetColor(Replace0Id, preset.replace0);
        _block.SetColor(Replace1Id, preset.replace1);
        _block.SetColor(Replace2Id, preset.replace2);
        _block.SetColor(Replace3Id, preset.replace3);

        _block.SetFloat(ToleranceId, tolerance);

        _renderer.SetPropertyBlock(_block);

        if (coatPresets != null)
        {
            for (int i = 0; i < coatPresets.Length; i++)
            {
                if (coatPresets[i] == null)
                    continue;
                if (coatPresets[i] == preset ||
                    (!string.IsNullOrEmpty(preset.name) && coatPresets[i].name == preset.name))
                {
                    _activePresetIndex = i;
                    break;
                }
            }
        }

        Debug.Log($"CatAppearance: applied coat '{preset.name}'");
    }
}
