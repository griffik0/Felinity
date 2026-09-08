using UnityEngine;

public enum ObjectiveType
{
    ScratchCount,
    ForageCount,
    CatCoinTotal,
    ItemCollected
}

[CreateAssetMenu(fileName = "New Quest", menuName = "Quests/New Quest")]
public class QuestData : ScriptableObject
{
    public string questName;
    [TextArea(2, 4)]
    public string description;
    public ObjectiveType objectiveType;
    public int targetAmount = 1;
    public int catCoinReward;

    [Tooltip("Used when objectiveType is ItemCollected.")]
    public ItemData requiredItem;
}
