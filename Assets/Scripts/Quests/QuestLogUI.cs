using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Plain-text quest log under the CatCoins counter.
/// </summary>
public class QuestLogUI : MonoBehaviour
{
    public TMP_Text logText;
    public QuestManager questManager;

    void Awake()
    {
        if (logText == null)
            logText = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        if (questManager == null)
            questManager = QuestManager.Instance;

        if (questManager != null)
        {
            questManager.OnQuestProgress += HandleProgress;
            questManager.OnQuestCompleted += HandleCompleted;
        }

        Refresh();
    }

    void OnDisable()
    {
        if (questManager != null)
        {
            questManager.OnQuestProgress -= HandleProgress;
            questManager.OnQuestCompleted -= HandleCompleted;
        }
    }

    void Start()
    {
        if (questManager == null)
            questManager = QuestManager.Instance;

        if (questManager != null)
        {
            questManager.OnQuestProgress -= HandleProgress;
            questManager.OnQuestCompleted -= HandleCompleted;
            questManager.OnQuestProgress += HandleProgress;
            questManager.OnQuestCompleted += HandleCompleted;
        }

        Refresh();
    }

    void HandleProgress(QuestData quest, int current, int target)
    {
        Refresh();
    }

    void HandleCompleted(QuestData quest)
    {
        Refresh();
    }

    public void Refresh()
    {
        if (logText == null)
            return;

        if (questManager == null)
            questManager = QuestManager.Instance;

        if (questManager == null || questManager.activeQuests == null)
        {
            logText.text = "Quests:\n(none)";
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("Quests:");

        bool any = false;
        foreach (QuestData quest in questManager.activeQuests)
        {
            if (quest == null)
                continue;

            any = true;
            int current = questManager.GetProgress(quest);
            int target = quest.targetAmount;
            string suffix = questManager.IsCompleted(quest) ? " (Done)" : string.Empty;
            sb.AppendLine($"{quest.questName}: {Mathf.Min(current, target)}/{target}{suffix}");
        }

        if (!any)
            sb.AppendLine("(none)");

        logText.text = sb.ToString().TrimEnd();
    }
}
