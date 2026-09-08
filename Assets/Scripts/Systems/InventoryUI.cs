using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Bottom-left inventory list updated from InventorySystem.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    public TMP_Text inventoryText;
    public InventorySystem inventory;

    void Awake()
    {
        if (inventoryText == null)
            inventoryText = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        Bind();
        Refresh();
    }

    void OnDisable()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= Refresh;
    }

    void Start()
    {
        Bind();
        Refresh();
    }

    void Bind()
    {
        if (inventory == null)
            inventory = InventorySystem.Instance;

        if (inventory == null)
            return;

        inventory.OnInventoryChanged -= Refresh;
        inventory.OnInventoryChanged += Refresh;
    }

    public void Refresh()
    {
        if (inventoryText == null)
            return;

        if (inventory == null)
            inventory = InventorySystem.Instance;

        if (inventory == null)
        {
            inventoryText.text = "Inventory:\n(empty)";
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("Inventory:");

        bool any = false;
        foreach (var pair in inventory.Items)
        {
            if (pair.Key == null || pair.Value <= 0)
                continue;

            any = true;
            string name = string.IsNullOrEmpty(pair.Key.itemName) ? pair.Key.name : pair.Key.itemName;
            sb.AppendLine($"{name} x{pair.Value}");
        }

        if (!any)
            sb.AppendLine("(empty)");

        inventoryText.text = sb.ToString().TrimEnd();
    }
}
