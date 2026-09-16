using System;
using UnityEngine;

/// <summary>
/// Thin upgrade hub so TreeUpgrade SFX has a real trigger point.
/// Call <see cref="NotifyTierUpgraded"/> when a home/tree tier is unlocked.
/// </summary>
public class HomeUpgradeManager : MonoBehaviour
{
    public static HomeUpgradeManager Instance { get; private set; }

    public event Action OnTierUpgraded;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Fires <see cref="OnTierUpgraded"/> and plays the TreeUpgrade SFX (if assigned).
    /// </summary>
    public void NotifyTierUpgraded()
    {
        OnTierUpgraded?.Invoke();
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayTreeUpgrade();
    }
}
