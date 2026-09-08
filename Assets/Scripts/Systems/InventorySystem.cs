using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple item inventory singleton.
/// </summary>
public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }

    readonly Dictionary<ItemData, int> _items = new Dictionary<ItemData, int>();

    public event Action OnInventoryChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public IReadOnlyDictionary<ItemData, int> Items => _items;

    public int GetCount(ItemData item)
    {
        if (item == null)
            return 0;

        return _items.TryGetValue(item, out int count) ? count : 0;
    }

    public void AddItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return;

        if (_items.ContainsKey(item))
            _items[item] += amount;
        else
            _items[item] = amount;

        OnInventoryChanged?.Invoke();
    }

    public bool RemoveItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return false;

        if (!_items.TryGetValue(item, out int count) || count < amount)
            return false;

        count -= amount;
        if (count <= 0)
            _items.Remove(item);
        else
            _items[item] = count;

        OnInventoryChanged?.Invoke();
        return true;
    }
}
