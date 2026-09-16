using UnityEngine;

/// <summary>
/// Central clip table for one-shot SFX. Assign clips in the Inspector; leave empty to skip.
/// Each clip has a matching volume slider (0–1) for balancing loudness.
/// </summary>
[CreateAssetMenu(fileName = "SFXLibrary", menuName = "Felinity/SFX Library", order = 10)]
public class SFXLibrary : ScriptableObject
{
    [Header("World")]
    public AudioClip scratch;
    [Range(0f, 1f)] public float scratchVolume = 1f;

    [Tooltip("One clip is chosen at random each forage. Empty entries are skipped.")]
    public AudioClip[] forage;
    [Range(0f, 1f)] public float forageVolume = 1f;

    public AudioClip itemPickupChime;
    [Range(0f, 1f)] public float itemPickupChimeVolume = 0.35f;

    public AudioClip footstepA;
    [Range(0f, 1f)] public float footstepAVolume = 1f;

    public AudioClip footstepB;
    [Range(0f, 1f)] public float footstepBVolume = 1f;

    public AudioClip treeUpgrade;
    [Range(0f, 1f)] public float treeUpgradeVolume = 1f;

    [Header("Economy / Quests")]
    public AudioClip catCoinGain;
    [Range(0f, 1f)] public float catCoinGainVolume = 1f;

    public AudioClip questComplete;
    [Range(0f, 1f)] public float questCompleteVolume = 1f;

    [Header("Dialogue / UI")]
    public AudioClip dialogueAdvance;
    [Range(0f, 1f)] public float dialogueAdvanceVolume = 1f;

    public AudioClip buttonClick;
    [Range(0f, 1f)] public float buttonClickVolume = 1f;

    public AudioClip toolbarSwitch;
    [Range(0f, 1f)] public float toolbarSwitchVolume = 1f;

    public AudioClip inventoryOpen;
    [Range(0f, 1f)] public float inventoryOpenVolume = 1f;

    public AudioClip inventoryClose;
    [Range(0f, 1f)] public float inventoryCloseVolume = 1f;

    public AudioClip pauseOpen;
    [Range(0f, 1f)] public float pauseOpenVolume = 1f;

    public AudioClip pauseClose;
    [Range(0f, 1f)] public float pauseCloseVolume = 1f;
}
