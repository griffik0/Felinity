using UnityEngine;

/// <summary>
/// Slowly cycles idle facing directions so the character-creation preview feels alive.
/// </summary>
[RequireComponent(typeof(PlayerFacing))]
public class CharacterPreviewIdle : MonoBehaviour
{
    public float secondsPerFacing = 0.85f;

    PlayerFacing _facing;
    float _timer;
    int _index;

    static readonly PlayerFacing.Facing[] Order =
    {
        PlayerFacing.Facing.South,
        PlayerFacing.Facing.SouthEast,
        PlayerFacing.Facing.East,
        PlayerFacing.Facing.NorthEast,
        PlayerFacing.Facing.North,
        PlayerFacing.Facing.NorthWest,
        PlayerFacing.Facing.West,
        PlayerFacing.Facing.SouthWest
    };

    void Awake()
    {
        _facing = GetComponent<PlayerFacing>();
    }

    void Update()
    {
        if (_facing == null)
            return;

        _timer += Time.unscaledDeltaTime;
        if (_timer < secondsPerFacing)
            return;

        _timer = 0f;
        _index = (_index + 1) % Order.Length;
        _facing.facing = Order[_index];
        _facing.ApplySprite();
    }
}
