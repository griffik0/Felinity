using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Persistent JSON save/load singleton. Lives next to MusicManager on TitleScreen.
/// Debug: press F5 in play mode to force Save().
/// </summary>
[DefaultExecutionOrder(-50)]
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    public const string SaveFileName = "savefile.json";
    public const Key DebugSaveKey = Key.F5;

    [Header("Item lookup (assign all ItemData assets)")]
    public ItemData[] itemCatalog;

    [Header("Debug")]
    public bool enableDebugSaveKey = true;

    bool _loadPending;
    bool _suppressAutoSave;
    bool _wiredMainWorld;

    public static string SavePath =>
        Path.Combine(Application.persistentDataPath, SaveFileName);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureItemCatalog();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnwireMainWorld();
    }

    void OnDestroy()
    {
        UnwireMainWorld();
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (!enableDebugSaveKey || !Application.isPlaying)
            return;

        Keyboard kb = Keyboard.current;
        if (kb != null && kb[DebugSaveKey].wasPressedThisFrame)
        {
            Save();
            Debug.Log($"[SaveManager] Debug save (F5) → {SavePath}");
        }
    }

    void OnApplicationQuit()
    {
        if (!_suppressAutoSave)
            Save();
    }

    public bool HasSaveFile()
    {
        return File.Exists(SavePath);
    }

    /// <summary>
    /// Fresh play: wipe existing save so Continue won't restore the old run.
    /// </summary>
    public void BeginNewGame()
    {
        _loadPending = false;
        DeleteSaveFile();
        CatCoins.CatCoinBalance = 0;
    }

    public void DeleteSaveFile()
    {
        try
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SaveManager] Could not delete save: {ex.Message}");
        }
    }

    /// <summary>
    /// Title Continue: load MainWorld, then apply save after systems Start.
    /// </summary>
    public void ContinueGame()
    {
        if (!HasSaveFile())
        {
            Debug.LogWarning("[SaveManager] Continue requested but no save file.");
            return;
        }

        _loadPending = true;
        GameFlow.LoadMainWorld();
    }

    public void Save()
    {
        if (_suppressAutoSave)
            return;

        // Only persist meaningful world state from MainWorld (or while already mid-session).
        if (SceneManager.GetActiveScene().name != GameFlow.MainSceneName &&
            InventorySystem.Instance == null)
            return;

        try
        {
            SaveData data = Capture();
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(SavePath, json);
            Debug.Log($"[SaveManager] Saved → {SavePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] Save failed: {ex}");
        }
    }

    public bool Load()
    {
        if (!HasSaveFile())
            return false;

        try
        {
            string json = File.ReadAllText(SavePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            if (data == null)
                return false;

            _suppressAutoSave = true;
            Apply(data);
            _suppressAutoSave = false;
            Debug.Log($"[SaveManager] Loaded ← {SavePath}");
            return true;
        }
        catch (Exception ex)
        {
            _suppressAutoSave = false;
            Debug.LogError($"[SaveManager] Load failed: {ex}");
            return false;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != GameFlow.MainSceneName)
        {
            UnwireMainWorld();
            return;
        }

        StartCoroutine(AfterMainWorldReady());
    }

    IEnumerator AfterMainWorldReady()
    {
        // Wait until Start() has run on scene singletons.
        yield return null;

        WireMainWorld();

        if (_loadPending)
        {
            _loadPending = false;
            Load();
        }
    }

    void WireMainWorld()
    {
        if (_wiredMainWorld)
            return;

        _wiredMainWorld = true;

        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.OnPauseStateChanged -= HandlePauseChanged;
            PauseManager.Instance.OnPauseStateChanged += HandlePauseChanged;
        }

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestCompleted -= HandleQuestCompleted;
            QuestManager.Instance.OnQuestCompleted += HandleQuestCompleted;
        }

        if (HomeUpgradeManager.Instance != null)
        {
            HomeUpgradeManager.Instance.OnTierUpgraded -= HandleTierUpgraded;
            HomeUpgradeManager.Instance.OnTierUpgraded += HandleTierUpgraded;
        }
    }

    void UnwireMainWorld()
    {
        if (!_wiredMainWorld)
            return;

        _wiredMainWorld = false;

        if (PauseManager.Instance != null)
            PauseManager.Instance.OnPauseStateChanged -= HandlePauseChanged;

        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestCompleted -= HandleQuestCompleted;

        if (HomeUpgradeManager.Instance != null)
            HomeUpgradeManager.Instance.OnTierUpgraded -= HandleTierUpgraded;
    }

    void HandlePauseChanged(bool paused)
    {
        if (paused)
            Save();
    }

    void HandleQuestCompleted(QuestData _)
    {
        Save();
    }

    void HandleTierUpgraded()
    {
        Save();
    }

    SaveData Capture()
    {
        var data = new SaveData();

        data.catCoinBalance = CatCoins.CatCoinBalance;

        PlayerProfile profile = PlayerProfile.Instance ?? PlayerProfile.EnsureExists();
        if (profile != null)
        {
            data.catName = profile.catName;
            data.coatPresetIndex = profile.chosenCoatIndex;
            data.coatPresetName = profile.chosenCoat != null ? profile.chosenCoat.name : string.Empty;
            data.hasCustomization = profile.hasCustomization;
        }

        if (HomeUpgradeManager.Instance != null)
            data.currentTreeTierIndex = HomeUpgradeManager.Instance.CurrentTierIndex;

        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            Vector3 p = player.transform.position;
            data.playerX = p.x;
            data.playerY = p.y;
            data.playerZ = p.z;
        }

        GeraldController gerald = UnityEngine.Object.FindAnyObjectByType<GeraldController>();
        if (gerald != null)
        {
            data.geraldState = (int)gerald.state;
            data.geraldBerryCount = InventorySystem.Instance != null && gerald.wildBerry != null
                ? InventorySystem.Instance.GetTotalCount(gerald.wildBerry)
                : 0;
        }

        CaptureInventory(data);
        CaptureQuests(data);
        return data;
    }

    void CaptureInventory(SaveData data)
    {
        data.toolbarSlots = new List<SaveSlotEntry>();
        data.generalSlots = new List<SaveSlotEntry>();

        InventorySystem inv = InventorySystem.Instance;
        if (inv == null)
            return;

        for (int i = 0; i < InventorySystem.ToolbarSize; i++)
            data.toolbarSlots.Add(ToSaveSlot(inv.GetToolbarSlot(i)));

        for (int i = 0; i < InventorySystem.GeneralSize; i++)
            data.generalSlots.Add(ToSaveSlot(inv.GetGeneralSlot(i)));
    }

    static SaveSlotEntry ToSaveSlot(InventorySlotEntry slot)
    {
        var entry = new SaveSlotEntry();
        if (slot == null || slot.IsEmpty || slot.item == null)
        {
            entry.itemName = string.Empty;
            entry.count = 0;
            return entry;
        }

        entry.itemName = string.IsNullOrEmpty(slot.item.itemName) ? slot.item.name : slot.item.itemName;
        entry.count = slot.count;
        return entry;
    }

    void CaptureQuests(SaveData data)
    {
        data.quests = new List<SaveQuestEntry>();
        QuestManager qm = QuestManager.Instance;
        if (qm == null)
            return;

        // Snapshot every known quest (active list + completed).
        var seen = new HashSet<string>();
        foreach (QuestData quest in qm.activeQuests)
        {
            if (quest == null || string.IsNullOrEmpty(quest.questName))
                continue;
            if (!seen.Add(quest.questName))
                continue;

            data.quests.Add(new SaveQuestEntry
            {
                questName = quest.questName,
                progress = qm.GetProgress(quest),
                completed = qm.IsCompleted(quest),
                active = true
            });
        }

        foreach (string name in qm.GetCompletedQuestNames())
        {
            if (string.IsNullOrEmpty(name) || !seen.Add(name))
                continue;

            data.quests.Add(new SaveQuestEntry
            {
                questName = name,
                progress = 0,
                completed = true,
                active = false
            });
        }
    }

    void Apply(SaveData data)
    {
        EnsureItemCatalog();

        // Profile / appearance
        PlayerProfile profile = PlayerProfile.EnsureExists();
        if (profile != null)
        {
            if (!string.IsNullOrWhiteSpace(data.catName))
                profile.SetCatName(data.catName);

            profile.chosenCoatIndex = Mathf.Max(0, data.coatPresetIndex);
            profile.hasCustomization = data.hasCustomization || !string.IsNullOrWhiteSpace(data.catName);

            GameObject playerGo = GameObject.Find("Player");
            CatAppearance appearance = playerGo != null ? playerGo.GetComponent<CatAppearance>() : null;
            if (appearance != null)
            {
                int coatIndex = ResolveCoatIndex(appearance, data.coatPresetName, data.coatPresetIndex);
                profile.chosenCoatIndex = coatIndex;
                if (coatIndex >= 0 && coatIndex < appearance.coatPresets.Length)
                    profile.SetCoat(appearance.coatPresets[coatIndex], coatIndex);
                appearance.ApplyFromProfile(profile);
            }
            else
            {
                profile.NotifyMainWorldLoaded();
            }
        }

        // Coins (absolute set — no SFX)
        CatCoins.CatCoinBalance = Mathf.Max(0, data.catCoinBalance);

        // Inventory
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.ClearAll();
            ApplySlots(data.toolbarSlots, true);
            ApplySlots(data.generalSlots, false);
            InventorySystem.Instance.NotifyChanged();
        }

        // Tree tier
        if (HomeUpgradeManager.Instance != null)
            HomeUpgradeManager.Instance.SetTierIndex(data.currentTreeTierIndex, playSfx: false);

        // Player position
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            Vector3 pos = new Vector3(data.playerX, data.playerY, data.playerZ);
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.position = pos;
            player.transform.position = pos;
        }

        // Gerald
        GeraldController gerald = UnityEngine.Object.FindAnyObjectByType<GeraldController>();
        if (gerald != null)
        {
            gerald.EnsureDefaults();
            GeraldController.GeraldState state =
                (GeraldController.GeraldState)Mathf.Clamp(data.geraldState, 0, 3);
            gerald.state = state;

            if (QuestManager.Instance != null && gerald.berryQuest != null)
            {
                if (state == GeraldController.GeraldState.QuestActive ||
                    state == GeraldController.GeraldState.QuestComplete)
                {
                    QuestManager.Instance.RegisterQuest(gerald.berryQuest);
                }

                if (state == GeraldController.GeraldState.QuestComplete)
                    QuestManager.Instance.MarkCompleteSilent(gerald.berryQuest);
            }
        }

        // Quests (First Scratch etc.)
        ApplyQuests(data);
    }

    void ApplySlots(List<SaveSlotEntry> slots, bool toolbar)
    {
        if (slots == null || InventorySystem.Instance == null)
            return;

        int max = toolbar ? InventorySystem.ToolbarSize : InventorySystem.GeneralSize;
        for (int i = 0; i < max && i < slots.Count; i++)
        {
            SaveSlotEntry saved = slots[i];
            if (saved == null || saved.count <= 0 || string.IsNullOrEmpty(saved.itemName))
                continue;

            ItemData item = FindItemByName(saved.itemName);
            if (item == null)
            {
                Debug.LogWarning($"[SaveManager] Unknown item '{saved.itemName}' — slot {i} skipped.");
                continue;
            }

            InventorySlotEntry slot = toolbar
                ? InventorySystem.Instance.GetToolbarSlot(i)
                : InventorySystem.Instance.GetGeneralSlot(i);
            if (slot != null)
                slot.Set(item, saved.count);
        }
    }

    void ApplyQuests(SaveData data)
    {
        QuestManager qm = QuestManager.Instance;
        if (qm == null || data.quests == null)
            return;

        foreach (SaveQuestEntry entry in data.quests)
        {
            if (entry == null || string.IsNullOrEmpty(entry.questName))
                continue;

            // Gerald's quest handled above via gerald state.
            if (entry.questName == "Gerald's Berries")
            {
                if (entry.completed)
                {
                    GeraldController gerald = UnityEngine.Object.FindAnyObjectByType<GeraldController>();
                    if (gerald != null && gerald.berryQuest != null)
                        qm.MarkCompleteSilent(gerald.berryQuest);
                }
                continue;
            }

            QuestData quest = qm.FindQuestByName(entry.questName);
            if (quest == null)
                continue;

            if (entry.active || entry.completed)
                qm.RegisterQuest(quest);

            qm.SetProgressSilent(quest, entry.progress);
            if (entry.completed)
                qm.MarkCompleteSilent(quest);
        }
    }

    static int ResolveCoatIndex(CatAppearance appearance, string coatName, int fallbackIndex)
    {
        if (appearance == null || appearance.coatPresets == null)
            return Mathf.Max(0, fallbackIndex);

        if (!string.IsNullOrEmpty(coatName))
        {
            for (int i = 0; i < appearance.coatPresets.Length; i++)
            {
                if (appearance.coatPresets[i] != null &&
                    string.Equals(appearance.coatPresets[i].name, coatName, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
        }

        return Mathf.Clamp(fallbackIndex, 0, appearance.coatPresets.Length - 1);
    }

    ItemData FindItemByName(string name)
    {
        if (string.IsNullOrEmpty(name) || itemCatalog == null)
            return null;

        for (int i = 0; i < itemCatalog.Length; i++)
        {
            ItemData item = itemCatalog[i];
            if (item == null)
                continue;

            if (string.Equals(item.itemName, name, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(item.name, name, StringComparison.OrdinalIgnoreCase))
                return item;
        }

        return null;
    }

    void EnsureItemCatalog()
    {
        if (itemCatalog != null && itemCatalog.Length > 0)
            return;

#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets("t:ItemData");
        var list = new List<ItemData>();
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item != null)
                list.Add(item);
        }

        itemCatalog = list.ToArray();
#endif
    }
}
