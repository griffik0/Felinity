using UnityEngine;

/// <summary>
/// Persistent one-shot SFX singleton. Null clips are silently skipped.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    [Header("Library")]
    public SFXLibrary library;

    AudioSource _source;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _source = GetComponent<AudioSource>();
        if (_source == null)
            _source = gameObject.AddComponent<AudioSource>();

        _source.playOnAwake = false;
        _source.loop = false;
        _source.spatialBlend = 0f;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Plays a one-shot clip. Does nothing if the clip (or this manager) is missing.
    /// </summary>
    public void Play(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || _source == null)
            return;

        _source.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    public void PlayScratch()
    {
        if (library == null) return;
        Play(library.scratch, library.scratchVolume);
    }

    public void PlayForage()
    {
        if (library == null) return;
        Play(PickRandom(library.forage), library.forageVolume);
    }

    public void PlayItemPickupChime()
    {
        if (library == null) return;
        Play(library.itemPickupChime, library.itemPickupChimeVolume);
    }

    public void PlayCatCoinGain()
    {
        if (library == null) return;
        Play(library.catCoinGain, library.catCoinGainVolume);
    }

    public void PlayQuestComplete()
    {
        if (library == null) return;
        Play(library.questComplete, library.questCompleteVolume);
    }

    public void PlayDialogueAdvance()
    {
        if (library == null) return;
        Play(library.dialogueAdvance, library.dialogueAdvanceVolume);
    }

    public void PlayButtonClick()
    {
        if (library == null) return;
        Play(library.buttonClick, library.buttonClickVolume);
    }

    public void PlayToolbarSwitch()
    {
        if (library == null) return;
        Play(library.toolbarSwitch, library.toolbarSwitchVolume);
    }

    public void PlayInventoryOpen()
    {
        if (library == null) return;
        Play(library.inventoryOpen, library.inventoryOpenVolume);
    }

    public void PlayInventoryClose()
    {
        if (library == null) return;
        Play(library.inventoryClose, library.inventoryCloseVolume);
    }

    public void PlayPauseOpen()
    {
        if (library == null) return;
        Play(library.pauseOpen, library.pauseOpenVolume);
    }

    public void PlayPauseClose()
    {
        if (library == null) return;
        Play(library.pauseClose, library.pauseCloseVolume);
    }

    public void PlayFootstepA()
    {
        if (library == null) return;
        Play(library.footstepA, library.footstepAVolume);
    }

    public void PlayFootstepB()
    {
        if (library == null) return;
        Play(library.footstepB, library.footstepBVolume);
    }

    public void PlayTreeUpgrade()
    {
        if (library == null) return;
        Play(library.treeUpgrade, library.treeUpgradeVolume);
    }

    public void PlayFootstep(bool useB)
    {
        if (useB)
            PlayFootstepB();
        else
            PlayFootstepA();
    }

    static AudioClip PickRandom(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
            return null;

        int valid = 0;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null)
                valid++;
        }

        if (valid == 0)
            return null;

        int pick = Random.Range(0, valid);
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] == null)
                continue;
            if (pick == 0)
                return clips[i];
            pick--;
        }

        return null;
    }
}
