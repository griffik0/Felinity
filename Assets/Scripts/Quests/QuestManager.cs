using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks active quests and progress. Scratch/Forage/Inventory systems report into this.
/// </summary>
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    public List<QuestData> activeQuests = new List<QuestData>();

    readonly Dictionary<QuestData, int> _progress = new Dictionary<QuestData, int>();
    readonly HashSet<QuestData> _completed = new HashSet<QuestData>();

    public event Action<QuestData, int, int> OnQuestProgress;
    public event Action<QuestData> OnQuestCompleted;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InitializeProgress();
    }

    void OnEnable()
    {
        CatCoins.OnBalanceChanged += ReportCatCoins;
        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnInventoryChanged += HandleInventoryChanged;
    }

    void OnDisable()
    {
        CatCoins.OnBalanceChanged -= ReportCatCoins;
        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnInventoryChanged -= HandleInventoryChanged;
    }

    void Start()
    {
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnInventoryChanged -= HandleInventoryChanged;
            InventorySystem.Instance.OnInventoryChanged += HandleInventoryChanged;
            HandleInventoryChanged();
        }

        foreach (QuestData quest in activeQuests)
        {
            if (quest == null || _completed.Contains(quest))
                continue;

            int current = GetProgress(quest);
            OnQuestProgress?.Invoke(quest, current, quest.targetAmount);
        }
    }

    void InitializeProgress()
    {
        _progress.Clear();
        foreach (QuestData quest in activeQuests)
        {
            if (quest == null || _progress.ContainsKey(quest))
                continue;

            _progress[quest] = 0;
        }
    }

    public int GetProgress(QuestData quest)
    {
        if (quest == null)
            return 0;

        return _progress.TryGetValue(quest, out int value) ? value : 0;
    }

    public bool IsCompleted(QuestData quest)
    {
        return quest != null && _completed.Contains(quest);
    }

    public void ReportScratch(int amount = 1)
    {
        ReportObjective(ObjectiveType.ScratchCount, amount);
    }

    public void ReportForage(int amount = 1)
    {
        ReportObjective(ObjectiveType.ForageCount, amount);
    }

    public void ReportItemCollected(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return;

        foreach (QuestData quest in activeQuests)
        {
            if (quest == null || quest.objectiveType != ObjectiveType.ItemCollected)
                continue;

            if (quest.requiredItem != item)
                continue;

            if (_completed.Contains(quest))
                continue;

            SetProgress(quest, GetProgress(quest) + amount);
        }
    }

    public void ReportCatCoins(int totalBalance)
    {
        foreach (QuestData quest in activeQuests)
        {
            if (quest == null || quest.objectiveType != ObjectiveType.CatCoinTotal)
                continue;

            if (_completed.Contains(quest))
                continue;

            SetProgress(quest, totalBalance);
        }
    }

    void HandleInventoryChanged()
    {
        if (InventorySystem.Instance == null)
            return;

        foreach (QuestData quest in activeQuests)
        {
            if (quest == null || quest.objectiveType != ObjectiveType.ItemCollected)
                continue;

            if (quest.requiredItem == null || _completed.Contains(quest))
                continue;

            SetProgress(quest, InventorySystem.Instance.GetTotalCount(quest.requiredItem));
        }
    }

    void ReportObjective(ObjectiveType type, int amount)
    {
        if (amount <= 0)
            return;

        foreach (QuestData quest in activeQuests)
        {
            if (quest == null || quest.objectiveType != type)
                continue;

            if (_completed.Contains(quest))
                continue;

            int current = GetProgress(quest) + amount;
            SetProgress(quest, current);
        }
    }

    void SetProgress(QuestData quest, int current)
    {
        current = Mathf.Max(0, current);
        _progress[quest] = current;
        OnQuestProgress?.Invoke(quest, current, quest.targetAmount);

        if (current >= quest.targetAmount && !quest.requiresTurnIn)
            CompleteQuest(quest);
    }

    /// <summary>
    /// Adds a quest at runtime (e.g. offered by an NPC) and refreshes UI progress.
    /// </summary>
    public void RegisterQuest(QuestData quest)
    {
        if (quest == null)
            return;

        if (!activeQuests.Contains(quest))
            activeQuests.Add(quest);

        if (!_progress.ContainsKey(quest))
            _progress[quest] = 0;

        // Sync ItemCollected progress from current inventory.
        if (quest.objectiveType == ObjectiveType.ItemCollected &&
            quest.requiredItem != null &&
            InventorySystem.Instance != null &&
            !_completed.Contains(quest))
        {
            _progress[quest] = InventorySystem.Instance.GetTotalCount(quest.requiredItem);
        }

        int current = GetProgress(quest);
        OnQuestProgress?.Invoke(quest, current, quest.targetAmount);

        if (current >= quest.targetAmount && !quest.requiresTurnIn)
            CompleteQuest(quest);
    }

    /// <summary>
    /// Marks a turn-in (or any) quest complete and grants its CatCoin reward once.
    /// </summary>
    public void MarkComplete(QuestData quest)
    {
        if (quest == null)
            return;

        if (!_progress.ContainsKey(quest))
            _progress[quest] = quest.targetAmount;
        else
            _progress[quest] = Mathf.Max(_progress[quest], quest.targetAmount);

        OnQuestProgress?.Invoke(quest, GetProgress(quest), quest.targetAmount);
        CompleteQuest(quest);
    }

    void CompleteQuest(QuestData quest)
    {
        if (!_completed.Add(quest))
            return;

        if (quest.catCoinReward > 0)
            CatCoins.Add(quest.catCoinReward);

        Debug.Log($"Quest complete: {quest.questName}! +{quest.catCoinReward} CatCoins");
        OnQuestCompleted?.Invoke(quest);

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayQuestComplete();
    }
}
