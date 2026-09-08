using UnityEngine;

public enum ItemCategory
{
    Herb,
    Fish,
    Material,
    Cosmetic
}

[CreateAssetMenu(fileName = "New Item", menuName = "Items/New Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    [TextArea(1, 3)]
    public string description;
    public ItemCategory category = ItemCategory.Herb;
}
