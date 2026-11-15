
using Godot;
using System;
using ZeldaRemake.Models;

public partial class Cellar : CanvasLayer
{
    private const float MovementSpeed = 80.0f;
    private const float ClimbSpeed = 64.0f;
    private const float LandingTolerance = 1.0f;

    private CharacterBody2D _link = null!;
    private ColorRect _treasure = null!;

    private Area2D _leftLadder = null!;
    private Area2D _rightLadder = null!;

    private Rect2 _leftLadderBounds;
    private float _linkHalfHeight;

    private DungeonState? _state;
    private string? _itemId;
    private bool _hasReachedLeftLadderBottom;

    public string CellarId { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public event Action? ExitRequested;

    public override void _Ready()
    {
        _link = GetNode<CharacterBody2D>("Link");

        _leftLadder = GetNodeOrNull<Area2D>(
            "LeftLadderTrigger"
        ) ?? throw new InvalidOperationException(
            "Missing LeftLadderTrigger in ItemCellar.tscn."
        );

        _rightLadder = GetNodeOrNull<Area2D>(
            "RightLadderTrigger"
        ) ?? throw new InvalidOperationException(
            "Missing RightLadderTrigger in ItemCellar.tscn."
        );

        var linkShape = _link.GetNode<CollisionShape2D>(
            "CollisionShape2D"
        ).Shape as RectangleShape2D
            ?? throw new InvalidOperationException(
                "Cellar Link requires a RectangleShape2D."
            );

        _linkHalfHeight = linkShape.Size.Y / 2.0f;

        _leftLadderBounds = GetBounds(_leftLadder);

        _link.MotionMode = CharacterBody2D.MotionModeEnum.Floating;
        _link.SafeMargin = 0.001f;

        // Spawn at the top of the entrance ladder.
        _link.GlobalPosition = new Vector2(
            _leftLadderBounds.GetCenter().X,
            _leftLadderBounds.Position.Y + _linkHalfHeight
        );

        SetPhysicsProcess(false);
    }

    public void Configure(CellarData data, DungeonState state)
    {
        if (!string.Equals(
                data.Type,
                "item",
                StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(data.ItemId))
        {
            throw new InvalidOperationException(
                $"Unsupported item cellar: '{data.Id}'."
            );
        }

        CellarId = data.Id;
        _itemId = data.ItemId;
        _state = state;

        _treasure = GetNode<ColorRect>("Treasure");
        _treasure.Visible = !state.HasItem(_itemId);
    }

    public void Activate()
    {
        _hasReachedLeftLadderBottom = false;
        IsActive = true;
        SetPhysicsProcess(true);
    }

    public void Deactivate()
    {
        IsActive = false;
        _link.Velocity = Vector2.Zero;
        SetPhysicsProcess(false);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsActive)
        {
            return;
        }

        var horizontal = Input.GetAxis(
            "move_left",
            "move_right"
        );

        var vertical = Input.GetAxis(
            "move_up",
            "move_down"
        );

        // A trigger decides whether Link is climbing.
        Area2D? ladder = null;

        if (_leftLadder.OverlapsBody(_link))
        {
            ladder = _leftLadder;
        }
        else if (_rightLadder.OverlapsBody(_link))
        {
            ladder = _rightLadder;
        }

        if (ladder is not null)
        {
            MoveOnLadder(
                ladder,
                horizontal,
                vertical,
                (float)delta
            );
        }
        else
        {
            Walk(horizontal);
        }

        if (_leftLadder.OverlapsBody(_link))
        {
            var bottomY = _leftLadderBounds.End.Y -
                _linkHalfHeight;

            if (_link.GlobalPosition.Y >=
                bottomY - LandingTolerance)
            {
                _hasReachedLeftLadderBottom = true;
            }
        }

        TryCollectTreasure();
        TryExit();
    }


    private void MoveOnLadder(
        Area2D ladder,
        float horizontal,
        float vertical,
        float delta
    )
    {
        var bounds = GetBounds(ladder);
        var position = _link.GlobalPosition;

        var isEntrance = ladder == _leftLadder;

        var bottomY = bounds.End.Y - _linkHalfHeight;

        var topY = isEntrance
            ? bounds.Position.Y +
                (_hasReachedLeftLadderBottom
                    ? -_linkHalfHeight
                    : _linkHalfHeight)
            : bounds.Position.Y + _linkHalfHeight - 0.1f;

        var atBottom = position.Y >=
            bottomY - LandingTolerance;

        var wantsHorizontal =
            !Mathf.IsZeroApprox(horizontal);

        // Walk away from the bottom of either ladder.
        if (atBottom && wantsHorizontal)
        {
            Walk(horizontal);
            return;
        }

        // Near the top of the short ladder, finish climbing
        // before allowing horizontal movement.
        if (!isEntrance &&
            wantsHorizontal &&
            position.Y <= topY + 8.0f)
        {
            if (position.Y > topY + 0.5f)
            {
                var nextY = Mathf.MoveToward(
                    position.Y,
                    topY,
                    ClimbSpeed * delta
                );

                _link.Velocity = new Vector2(
                    0,
                    (nextY - position.Y) / delta
                );

                _link.MoveAndSlide();
                return;
            }

            Walk(horizontal);
            return;
        }

        // Centre Link on the ladder before climbing.
        var centreX = bounds.GetCenter().X;

        if (!Mathf.IsZeroApprox(vertical) &&
            Mathf.Abs(position.X - centreX) > 0.5f)
        {
            var alignedX = Mathf.MoveToward(
                position.X,
                centreX,
                MovementSpeed * delta
            );

            _link.Velocity = new Vector2(
                (alignedX - position.X) / delta,
                0
            );

            _link.MoveAndSlide();
            return;
        }

        // Climb vertically while inside the ladder.
        var nextPositionY = Mathf.Clamp(
            position.Y + vertical * ClimbSpeed * delta,
            topY,
            bottomY
        );

        _link.Velocity = new Vector2(
            0,
            (nextPositionY - position.Y) / delta
        );

        _link.MoveAndSlide();
    }


    private void Walk(float horizontal)
    {
        _link.Velocity = new Vector2(
            horizontal * MovementSpeed,
            0
        );

        _link.MoveAndSlide();
    }

    private static Rect2 GetBounds(Area2D ladder)
    {
        var collision = ladder.GetNode<CollisionShape2D>(
            "CollisionShape2D"
        );

        var rectangle = collision.Shape as RectangleShape2D
            ?? throw new InvalidOperationException(
                $"{ladder.Name} requires a RectangleShape2D."
            );

        return new Rect2(
            collision.GlobalPosition - rectangle.Size / 2.0f,
            rectangle.Size
        );
    }

    public bool TryCollectTreasure()
    {
        if (!IsActive ||
            !_treasure.Visible ||
            _state is null ||
            _itemId is null)
        {
            return false;
        }

        var linkShape = _link.GetNode<CollisionShape2D>(
            "CollisionShape2D"
        ).Shape as RectangleShape2D;

        if (linkShape is null)
        {
            return false;
        }

        var linkBounds = new Rect2(
            _link.GlobalPosition - linkShape.Size / 2.0f,
            linkShape.Size
        );

        if (!linkBounds.Intersects(_treasure.GetGlobalRect()))
        {
            return false;
        }

        if (!_state.TryCollectItem(CellarId, _itemId))
        {
            return false;
        }

        _treasure.Hide();

        GD.Print($"Collected item: {_itemId}");

        return true;
    }

    public bool TryExit()
    {
        if (!IsActive || !_hasReachedLeftLadderBottom)
        {
            return false;
        }

        var centreX = _leftLadderBounds.GetCenter().X;

        var exitY = _leftLadderBounds.Position.Y -
            _linkHalfHeight;

        if (Mathf.Abs(
                _link.GlobalPosition.X - centreX
            ) > LandingTolerance ||
            _link.GlobalPosition.Y > exitY + 0.01f)
        {
            return false;
        }

        Deactivate();
        ExitRequested?.Invoke();

        return true;
    }
}
