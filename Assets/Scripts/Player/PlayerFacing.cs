using UnityEngine;

/// <summary>
/// 8-direction cat visuals: idle sprites when still, walk frame loops while moving.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(PlayerController))]
public class PlayerFacing : MonoBehaviour
{
    public enum Facing
    {
        South,
        SouthEast,
        East,
        NorthEast,
        North,
        NorthWest,
        West,
        SouthWest
    }

    [Header("Idle Sprites (8 directions)")]
    public Sprite idleSouth;
    public Sprite idleSouthEast;
    public Sprite idleEast;
    public Sprite idleNorthEast;
    public Sprite idleNorth;
    public Sprite idleNorthWest;
    public Sprite idleWest;
    public Sprite idleSouthWest;

    [Header("Walk Frames (8 directions, ordered frame 0..N)")]
    public Sprite[] walkSouth;
    public Sprite[] walkSouthEast;
    public Sprite[] walkEast;
    public Sprite[] walkNorthEast;
    public Sprite[] walkNorth;
    public Sprite[] walkNorthWest;
    public Sprite[] walkWest;
    public Sprite[] walkSouthWest;

    [Header("Walk Timing")]
    [Tooltip("Seconds per walk frame (filenames use delay-0.2s; 0.1 = 2x that speed).")]
    public float frameDuration = 0.1f;

    [Header("State")]
    public Facing facing = Facing.South;

    PlayerController _player;
    SpriteRenderer _renderer;
    Vector2 _lastMove = Vector2.down;
    float _animTime;
    int _frameIndex;
    bool _wasMoving;

    public Facing CurrentFacing => facing;

    void Awake()
    {
        _player = GetComponent<PlayerController>();
        _renderer = GetComponent<SpriteRenderer>();
        ApplyVisual(false);
    }

    void LateUpdate()
    {
        Vector2 move = _player != null ? _player.MoveInput : Vector2.zero;
        bool moving = move.sqrMagnitude > 0.0001f;

        if (moving)
        {
            _lastMove = move;
            Facing nextFacing = DirectionToFacing(_lastMove);
            if (nextFacing != facing)
            {
                facing = nextFacing;
                _frameIndex = 0;
                _animTime = 0f;
            }

            if (!_wasMoving)
            {
                _frameIndex = 0;
                _animTime = 0f;
            }

            AdvanceWalkAnimation();
            ApplyVisual(true);
        }
        else
        {
            _frameIndex = 0;
            _animTime = 0f;
            ApplyVisual(false);
        }

        _wasMoving = moving;
    }

    void AdvanceWalkAnimation()
    {
        Sprite[] frames = GetWalkFrames(facing);
        if (frames == null || frames.Length == 0)
            return;

        float duration = Mathf.Max(0.01f, frameDuration);
        _animTime += Time.deltaTime;
        while (_animTime >= duration)
        {
            _animTime -= duration;
            _frameIndex = (_frameIndex + 1) % frames.Length;
        }
    }

    public void ApplySprite()
    {
        bool moving = _player != null && _player.IsMoving;
        ApplyVisual(moving);
    }

    void ApplyVisual(bool moving)
    {
        if (_renderer == null)
            _renderer = GetComponent<SpriteRenderer>();

        Sprite sprite = null;
        if (moving)
        {
            Sprite[] frames = GetWalkFrames(facing);
            if (frames != null && frames.Length > 0)
                sprite = frames[Mathf.Clamp(_frameIndex, 0, frames.Length - 1)];
        }

        if (sprite == null)
            sprite = GetIdleSprite(facing);

        if (sprite != null)
            _renderer.sprite = sprite;
    }

    Sprite GetIdleSprite(Facing dir)
    {
        switch (dir)
        {
            case Facing.South: return idleSouth;
            case Facing.SouthEast: return idleSouthEast;
            case Facing.East: return idleEast;
            case Facing.NorthEast: return idleNorthEast;
            case Facing.North: return idleNorth;
            case Facing.NorthWest: return idleNorthWest;
            case Facing.West: return idleWest;
            case Facing.SouthWest: return idleSouthWest;
            default: return idleSouth;
        }
    }

    Sprite[] GetWalkFrames(Facing dir)
    {
        switch (dir)
        {
            case Facing.South: return walkSouth;
            case Facing.SouthEast: return walkSouthEast;
            case Facing.East: return walkEast;
            case Facing.NorthEast: return walkNorthEast;
            case Facing.North: return walkNorth;
            case Facing.NorthWest: return walkNorthWest;
            case Facing.West: return walkWest;
            case Facing.SouthWest: return walkSouthWest;
            default: return walkSouth;
        }
    }

    static Facing DirectionToFacing(Vector2 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (angle < 0f)
            angle += 360f;

        int octant = Mathf.RoundToInt(angle / 45f) % 8;
        switch (octant)
        {
            case 0: return Facing.East;
            case 1: return Facing.NorthEast;
            case 2: return Facing.North;
            case 3: return Facing.NorthWest;
            case 4: return Facing.West;
            case 5: return Facing.SouthWest;
            case 6: return Facing.South;
            case 7: return Facing.SouthEast;
            default: return Facing.South;
        }
    }
}
