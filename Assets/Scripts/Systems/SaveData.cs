using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Serializable snapshot of persistent game state (JSON via JsonUtility).
/// </summary>
[Serializable]
public class SaveData
{
    public int version = 1;

    public int catCoinBalance;

    public string catName;
    public string coatPresetName;
    public int coatPresetIndex;
    public bool hasCustomization;

    public int currentTreeTierIndex;

    public float playerX;
    public float playerY;
    public float playerZ;

    public int geraldState;
    public int geraldBerryCount;

    public List<SaveSlotEntry> toolbarSlots = new List<SaveSlotEntry>();
    public List<SaveSlotEntry> generalSlots = new List<SaveSlotEntry>();

    public List<SaveQuestEntry> quests = new List<SaveQuestEntry>();

    public List<SaveForageEntry> foragePatches = new List<SaveForageEntry>();
}

[Serializable]
public class SaveSlotEntry
{
    public string itemName;
    public int count;
}

[Serializable]
public class SaveQuestEntry
{
    public string questName;
    public int progress;
    public bool completed;
    public bool active;
}

[Serializable]
public class SaveForageEntry
{
    public string saveId;
    public float respawnRemaining;
    public bool available;
}
