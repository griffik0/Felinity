using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent player choices from Character Creation (DontDestroyOnLoad).
/// </summary>
public class PlayerProfile : MonoBehaviour
{
    public const string DefaultCatName = "Whiskers";
    public const int MaxCatNameLength = 16;

    public static PlayerProfile Instance { get; private set; }

    public CatAppearance.CoatPreset chosenCoat;
    public int chosenCoatIndex;
    public string catName = DefaultCatName;
    public bool hasCustomization;

    bool _appliedForCurrentMainWorld;

    public static PlayerProfile EnsureExists()
    {
        if (Instance != null)
            return Instance;

        var go = new GameObject("PlayerProfile");
        return go.AddComponent<PlayerProfile>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (string.IsNullOrWhiteSpace(catName))
            catName = DefaultCatName;

        if (chosenCoat == null)
            chosenCoat = CloneCoat(GetDefaultCoat());
    }

    void Start()
    {
        // Cover Play-from-MainWorld (no prior sceneLoaded for the already-active scene).
        if (SceneManager.GetActiveScene().name == GameFlow.MainSceneName)
            QueueApplyToMainWorld();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != GameFlow.MainSceneName)
            return;

        _appliedForCurrentMainWorld = false;
        ApplyToMainWorldPlayerNow();
    }

    /// <summary>
    /// Called after LoadMainWorld in case sceneLoaded did not apply yet.
    /// </summary>
    public void NotifyMainWorldLoaded()
    {
        ApplyToMainWorldPlayerNow();
    }

    void QueueApplyToMainWorld()
    {
        _appliedForCurrentMainWorld = false;
        ApplyToMainWorldPlayerNow();
    }

    void ApplyToMainWorldPlayerNow()
    {
        if (_appliedForCurrentMainWorld)
            return;
        _appliedForCurrentMainWorld = true;

        if (!hasCustomization)
        {
            if (chosenCoat == null)
                chosenCoat = GetDefaultCoat();
            if (string.IsNullOrWhiteSpace(catName))
                catName = DefaultCatName;
        }

        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            CatAppearance appearance = player.GetComponent<CatAppearance>();
            if (appearance != null)
                appearance.ApplyFromProfile(this);
        }

        Debug.Log($"Welcome, {catName}!");
    }

    public void SetCoat(CatAppearance.CoatPreset preset, int index)
    {
        chosenCoat = CloneCoat(preset ?? GetDefaultCoat());
        chosenCoatIndex = Mathf.Max(0, index);
        hasCustomization = true;
    }

    public void SetCatName(string name)
    {
        catName = SanitizeName(name);
        hasCustomization = true;
    }

    public static string SanitizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        name = name.Trim();
        if (name.Length > MaxCatNameLength)
            name = name.Substring(0, MaxCatNameLength);
        return name;
    }

    public static CatAppearance.CoatPreset CloneCoat(CatAppearance.CoatPreset source)
    {
        if (source == null)
            return GetDefaultCoat();

        return new CatAppearance.CoatPreset
        {
            name = source.name,
            replace0 = source.replace0,
            replace1 = source.replace1,
            replace2 = source.replace2,
            replace3 = source.replace3
        };
    }

    public static CatAppearance.CoatPreset GetDefaultCoat()
    {
        return new CatAppearance.CoatPreset
        {
            name = "Orange Tabby",
            replace0 = new Color(0.949f, 0.596f, 0.278f, 1f),
            replace1 = new Color(0.373f, 0.188f, 0.133f, 1f),
            replace2 = new Color(0.506f, 0.227f, 0.169f, 1f),
            replace3 = new Color(0f, 0f, 0f, 0f)
        };
    }
}
