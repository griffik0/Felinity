using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stationary NPC cat — dialogue + Wild Berry gather quest.
/// </summary>
public class GeraldController : MonoBehaviour, IInteractable
{
    public enum GeraldState
    {
        NotMet,
        QuestOffered,
        QuestActive,
        QuestComplete
    }

    [Header("Quest")]
    public ItemData wildBerry;
    public int berriesRequired = 3;
    public int coinReward = 25;
    public QuestData berryQuest;

    [Header("State")]
    public GeraldState state = GeraldState.NotMet;

    [Header("Idle Sprites (8 directions)")]
    public Sprite idleSouth;
    public Sprite idleSouthEast;
    public Sprite idleEast;
    public Sprite idleNorthEast;
    public Sprite idleNorth;
    public Sprite idleNorthWest;
    public Sprite idleWest;
    public Sprite idleSouthWest;

    [Tooltip("Stationary facing. South faces the usual approach from below.")]
    public PlayerFacing.Facing idleFacing = PlayerFacing.Facing.South;

    [Header("Dialogue — NotMet / intro")]
    public List<DialogueLine> introLines = new List<DialogueLine>();

    [Header("Dialogue — QuestActive (not enough berries)")]
    public List<DialogueLine> notEnoughLines = new List<DialogueLine>();

    [Header("Dialogue — QuestActive (turn in)")]
    public List<DialogueLine> turnInLines = new List<DialogueLine>();

    [Header("Dialogue — QuestComplete (flavor)")]
    public List<DialogueLine> completeLines = new List<DialogueLine>();

    bool _busy;
    SpriteRenderer _renderer;

    void Reset()
    {
        FillDefaultDialogue();
    }

    void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        EnsureDefaults();
        ApplyIdleSprite();
    }

    public void EnsureDefaults()
    {
        if (introLines == null || introLines.Count == 0)
            FillDefaultDialogue();

        EnsureBerryQuestAsset();
    }

    /// <summary>
    /// Applies the configured idle facing sprite. Gerald stays stationary for now.
    /// </summary>
    public void ApplyIdleSprite()
    {
        if (_renderer == null)
            _renderer = GetComponent<SpriteRenderer>();
        if (_renderer == null)
            return;

        Sprite sprite = GetIdleSprite(idleFacing);
        if (sprite == null)
            sprite = idleSouth;
        if (sprite == null)
            return;

        _renderer.sprite = sprite;
        _renderer.color = Color.white;
    }

    Sprite GetIdleSprite(PlayerFacing.Facing dir)
    {
        switch (dir)
        {
            case PlayerFacing.Facing.South: return idleSouth;
            case PlayerFacing.Facing.SouthEast: return idleSouthEast;
            case PlayerFacing.Facing.East: return idleEast;
            case PlayerFacing.Facing.NorthEast: return idleNorthEast;
            case PlayerFacing.Facing.North: return idleNorth;
            case PlayerFacing.Facing.NorthWest: return idleNorthWest;
            case PlayerFacing.Facing.West: return idleWest;
            case PlayerFacing.Facing.SouthWest: return idleSouthWest;
            default: return idleSouth;
        }
    }

    public string GetInteractionPrompt() => "Talk";

    public bool CanInteract(GameObject interactor)
    {
        if (_busy)
            return false;
        if (DialogueUI.Instance != null &&
            (DialogueUI.Instance.IsOpen || DialogueUI.Instance.BlocksWorldInput))
            return false;
        return true;
    }

    public void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor))
            return;

        if (DialogueUI.Instance == null)
        {
            Debug.LogError("GeraldController: DialogueUI.Instance is missing.");
            _busy = false;
            return;
        }

        switch (state)
        {
            case GeraldState.NotMet:
            case GeraldState.QuestOffered:
                BeginIntro();
                break;
            case GeraldState.QuestActive:
                BeginQuestActiveTalk();
                break;
            case GeraldState.QuestComplete:
                BeginCompleteTalk();
                break;
        }
    }

    void BeginIntro()
    {
        state = GeraldState.QuestOffered;
        _busy = true;
        DialogueUI.Instance.Play(introLines, OnIntroFinished);
    }

    void OnIntroFinished()
    {
        EnsureBerryQuestAsset();
        if (QuestManager.Instance != null && berryQuest != null)
            QuestManager.Instance.RegisterQuest(berryQuest);

        state = GeraldState.QuestActive;
        _busy = false;
        Debug.Log("Gerald: berry quest registered.");
    }

    void BeginQuestActiveTalk()
    {
        int have = InventorySystem.Instance != null && wildBerry != null
            ? InventorySystem.Instance.GetCount(wildBerry)
            : 0;

        _busy = true;

        if (have < berriesRequired)
        {
            var lines = BuildNotEnoughLines(have);
            DialogueUI.Instance.Play(lines, () => _busy = false);
            return;
        }

        DialogueUI.Instance.Play(turnInLines, OnTurnInFinished);
    }

    void OnTurnInFinished()
    {
        if (InventorySystem.Instance != null && wildBerry != null)
            InventorySystem.Instance.RemoveItem(wildBerry, berriesRequired);

        // Reward coins via CatCoins; quest asset reward is 0 to avoid double-pay on MarkComplete.
        CatCoins.Add(coinReward);

        if (QuestManager.Instance != null && berryQuest != null)
            QuestManager.Instance.MarkComplete(berryQuest);

        state = GeraldState.QuestComplete;
        _busy = false;
        Debug.Log($"Gerald: accepted {berriesRequired} Wild Berry, +{coinReward} CatCoins.");
    }

    void BeginCompleteTalk()
    {
        _busy = true;
        DialogueUI.Instance.Play(completeLines, () => _busy = false);
    }

    List<DialogueLine> BuildNotEnoughLines(int have)
    {
        // Prefer a live count line; fall back to inspector templates.
        var lines = new List<DialogueLine>();
        if (notEnoughLines != null && notEnoughLines.Count > 0)
        {
            for (int i = 0; i < notEnoughLines.Count; i++)
            {
                DialogueLine src = notEnoughLines[i];
                if (src == null)
                    continue;
                lines.Add(new DialogueLine
                {
                    speakerName = src.speakerName,
                    text = FormatBerryCount(src.text, have)
                });
            }
        }

        if (lines.Count == 0)
        {
            lines.Add(new DialogueLine
            {
                speakerName = "Gerald",
                text =
                    $"Hmm. You've only got {have} of {berriesRequired} Wild Berries, kit. " +
                    "The bushes don't pick themselves — come back when your paws are stained red."
            });
        }

        return lines;
    }

    string FormatBerryCount(string template, int have)
    {
        if (string.IsNullOrEmpty(template))
            return template;

        return template
            .Replace("{have}", have.ToString())
            .Replace("{need}", berriesRequired.ToString())
            .Replace("{left}", Mathf.Max(0, berriesRequired - have).ToString());
    }

    void EnsureBerryQuestAsset()
    {
        if (berryQuest == null)
            berryQuest = ScriptableObject.CreateInstance<QuestData>();

        berryQuest.questName = "Gerald's Berries";
        berryQuest.description = "Gather 3 Wild Berries and bring them to Gerald.";
        berryQuest.objectiveType = ObjectiveType.ItemCollected;
        berryQuest.targetAmount = berriesRequired;
        berryQuest.requiredItem = wildBerry;
        berryQuest.requiresTurnIn = true;
        // Coins granted manually on turn-in so MarkComplete does not double-reward.
        berryQuest.catCoinReward = 0;
    }

    void FillDefaultDialogue()
    {
        introLines = new List<DialogueLine>
        {
            new DialogueLine
            {
                speakerName = "Gerald",
                text = "Easy now, kit. These old whiskers have seen more sunrises than you've had naps."
            },
            new DialogueLine
            {
                speakerName = "Gerald",
                text = "Name's Gerald. I keep an eye on this stretch of yard — and on cats who rush about without looking."
            },
            new DialogueLine
            {
                speakerName = "Gerald",
                text = "Tell you what. Bring me three Wild Berries from the forage patches. " +
                       "Sweet ones. Not the half-chewed kind you leave under the porch."
            },
            new DialogueLine
            {
                speakerName = "Gerald",
                text = "Do that, and I'll see you sorted with a few CatCoins for your trouble. Fair pawshake?"
            }
        };

        notEnoughLines = new List<DialogueLine>
        {
            new DialogueLine
            {
                speakerName = "Gerald",
                text = "Still short, kit — {have} of {need} Wild Berries. The bushes are out there. Sniff carefully."
            }
        };

        turnInLines = new List<DialogueLine>
        {
            new DialogueLine
            {
                speakerName = "Gerald",
                text = "Ah. There they are — sticky, bright, and honest. You've done right by an old tom."
            },
            new DialogueLine
            {
                speakerName = "Gerald",
                text = "Take these CatCoins. Don't spend them all on catnip in one afternoon, hear me?"
            }
        };

        completeLines = new List<DialogueLine>
        {
            new DialogueLine
            {
                speakerName = "Gerald",
                text = "Still here, eh? Good. The yard's quieter with a friend who keeps their word."
            },
            new DialogueLine
            {
                speakerName = "Gerald",
                text = "Go stretch those legs. I'll be on this spot if you need an old ear to bend."
            }
        };
    }
}
