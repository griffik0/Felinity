using System;
using UnityEngine;

/// <summary>
/// Home / cat-tree upgrade tiers. Tier 0 = starter; higher indices swap the tree sprite.
/// </summary>
public class HomeUpgradeManager : MonoBehaviour
{
    public static HomeUpgradeManager Instance { get; private set; }

    [Header("Tree")]
    public SpriteRenderer catTreeRenderer;
    public Sprite[] tierSprites;

    [SerializeField]
    int currentTierIndex;

    public int CurrentTierIndex => currentTierIndex;

    public event Action OnTierUpgraded;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ApplyTierVisual();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Advances one tier (clamped), updates sprite, fires event + SFX.
    /// </summary>
    public void UpgradeTier()
    {
        int max = tierSprites != null && tierSprites.Length > 0 ? tierSprites.Length - 1 : currentTierIndex + 1;
        int next = Mathf.Min(currentTierIndex + 1, max);
        if (next == currentTierIndex && tierSprites != null && tierSprites.Length > 0)
            return;

        SetTierIndex(next, playSfx: true);
        OnTierUpgraded?.Invoke();
    }

    /// <summary>
    /// Fires <see cref="OnTierUpgraded"/> and plays TreeUpgrade SFX.
    /// Advances the tier if sprites are configured; otherwise just notifies.
    /// </summary>
    public void NotifyTierUpgraded()
    {
        if (tierSprites != null && tierSprites.Length > 0)
        {
            UpgradeTier();
            return;
        }

        OnTierUpgraded?.Invoke();
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayTreeUpgrade();
    }

    /// <summary>
    /// Absolute tier restore for save/load. When <paramref name="playSfx"/> is false, silent.
    /// </summary>
    public void SetTierIndex(int index, bool playSfx = false)
    {
        int max = tierSprites != null && tierSprites.Length > 0
            ? tierSprites.Length - 1
            : Mathf.Max(0, index);
        currentTierIndex = Mathf.Clamp(index, 0, Mathf.Max(0, max));
        ApplyTierVisual();

        if (playSfx && SFXManager.Instance != null)
            SFXManager.Instance.PlayTreeUpgrade();
    }

    void ApplyTierVisual()
    {
        if (catTreeRenderer == null || tierSprites == null || tierSprites.Length == 0)
            return;

        int i = Mathf.Clamp(currentTierIndex, 0, tierSprites.Length - 1);
        if (tierSprites[i] != null)
            catTreeRenderer.sprite = tierSprites[i];
    }
}
