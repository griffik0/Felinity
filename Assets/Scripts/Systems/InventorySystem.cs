using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One inventory cell: an item reference + stack count (or empty).
/// </summary>
[Serializable]
public class InventorySlotEntry
{
    public ItemData item;
    public int count;

    public bool IsEmpty => item == null || count <= 0;

    public void Clear()
    {
        item = null;
        count = 0;
    }

    public void Set(ItemData data, int amount)
    {
        item = data;
        count = Mathf.Max(0, amount);
        if (count <= 0 || item == null)
            Clear();
    }
}

/// <summary>
/// Split inventory: 7 toolbar slots + 24 general bag slots.
/// </summary>
public class InventorySystem : MonoBehaviour
{
    public const int ToolbarSize = 7;
    public const int GeneralSize = 24;

    public static InventorySystem Instance { get; private set; }

    readonly InventorySlotEntry[] _toolbar = new InventorySlotEntry[ToolbarSize];
    readonly InventorySlotEntry[] _general = new InventorySlotEntry[GeneralSize];

    public event Action OnInventoryChanged;

    public IReadOnlyList<InventorySlotEntry> ToolbarSlots => _toolbar;
    public IReadOnlyList<InventorySlotEntry> GeneralSlots => _general;

    /// <summary>
    /// Aggregated unique item totals (toolbar + general). Useful for legacy UI.
    /// </summary>
    public IReadOnlyDictionary<ItemData, int> Items
    {
        get
        {
            var map = new Dictionary<ItemData, int>();
            Accumulate(_toolbar, map);
            Accumulate(_general, map);
            return map;
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureSlotsInitialized(_toolbar);
        EnsureSlotsInitialized(_general);
    }

    static void EnsureSlotsInitialized(InventorySlotEntry[] slots)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                slots[i] = new InventorySlotEntry();
        }
    }

    public int GetTotalCount(ItemData item)
    {
        if (item == null)
            return 0;

        int total = 0;
        total += CountIn(_toolbar, item);
        total += CountIn(_general, item);
        return total;
    }

    /// <summary>Alias for <see cref="GetTotalCount"/> — checks toolbar + general.</summary>
    public int GetCount(ItemData item) => GetTotalCount(item);

    public InventorySlotEntry GetToolbarSlot(int index)
    {
        if (index < 0 || index >= _toolbar.Length)
            return null;
        return _toolbar[index];
    }

    public InventorySlotEntry GetGeneralSlot(int index)
    {
        if (index < 0 || index >= _general.Length)
            return null;
        return _general[index];
    }

    public void AddItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return;

        EnsureSlotsInitialized(_toolbar);
        EnsureSlotsInitialized(_general);

        int remaining = amount;

        // a) Stack onto existing stacks first (toolbar, then general).
        remaining = AddToExistingStacks(_toolbar, item, remaining);
        if (remaining > 0)
            remaining = AddToExistingStacks(_general, item, remaining);

        // b) Empty toolbar slots for brand-new items / overflow stacks.
        if (remaining > 0)
            remaining = PlaceIntoEmptySlots(_toolbar, item, remaining);

        // c) Then general bag.
        if (remaining > 0)
            remaining = PlaceIntoEmptySlots(_general, item, remaining);

        // d) Completely full.
        if (remaining > 0)
        {
            Debug.LogWarning(
                $"[InventorySystem] Inventory full — could not add {remaining}x " +
                $"{(string.IsNullOrEmpty(item.itemName) ? item.name : item.itemName)}.");
        }

        if (remaining < amount)
            OnInventoryChanged?.Invoke();
    }

    public bool RemoveItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return false;

        if (GetTotalCount(item) < amount)
            return false;

        int remaining = amount;
        remaining = RemoveFromSlots(_toolbar, item, remaining);
        if (remaining > 0)
            remaining = RemoveFromSlots(_general, item, remaining);

        OnInventoryChanged?.Invoke();
        return remaining == 0;
    }

    static int CountIn(InventorySlotEntry[] slots, ItemData item)
    {
        int total = 0;
        for (int i = 0; i < slots.Length; i++)
        {
            InventorySlotEntry slot = slots[i];
            if (slot != null && !slot.IsEmpty && slot.item == item)
                total += slot.count;
        }

        return total;
    }

    static void Accumulate(InventorySlotEntry[] slots, Dictionary<ItemData, int> map)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            InventorySlotEntry slot = slots[i];
            if (slot == null || slot.IsEmpty)
                continue;

            if (map.ContainsKey(slot.item))
                map[slot.item] += slot.count;
            else
                map[slot.item] = slot.count;
        }
    }

    static int AddToExistingStacks(InventorySlotEntry[] slots, ItemData item, int amount)
    {
        if (amount <= 0)
            return 0;

        for (int i = 0; i < slots.Length; i++)
        {
            InventorySlotEntry slot = slots[i];
            if (slot == null || slot.IsEmpty || slot.item != item)
                continue;

            slot.count += amount;
            return 0;
        }

        return amount;
    }

    static int PlaceIntoEmptySlots(InventorySlotEntry[] slots, ItemData item, int amount)
    {
        if (amount <= 0)
            return 0;

        for (int i = 0; i < slots.Length; i++)
        {
            InventorySlotEntry slot = slots[i];
            if (slot == null)
                slots[i] = slot = new InventorySlotEntry();

            if (!slot.IsEmpty)
                continue;

            slot.Set(item, amount);
            return 0;
        }

        return amount;
    }

    static int RemoveFromSlots(InventorySlotEntry[] slots, ItemData item, int amount)
    {
        if (amount <= 0)
            return 0;

        for (int i = 0; i < slots.Length && amount > 0; i++)
        {
            InventorySlotEntry slot = slots[i];
            if (slot == null || slot.IsEmpty || slot.item != item)
                continue;

            int take = Mathf.Min(slot.count, amount);
            slot.count -= take;
            amount -= take;
            if (slot.count <= 0)
                slot.Clear();
        }

        return amount;
    }
}
