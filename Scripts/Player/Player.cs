
using Godot;
using System.Collections.Generic;

public enum PlayerMovementMode
{
    TopDown,
    SideView
}

public partial class Player : CharacterBody2D
{
    private const float ClimbSpeed = 64.0f;
    private const float LandingTolerance = 1.0f;
    private const float LadderTopClearance = 0.1f;

    [Export]
    public float Speed { get; set; } = 80.0f;

    [Export]
    public PlayerMovementMode MovementMode { get; set; }
        = PlayerMovementMode.TopDown;

    private PushableBlock? _pushTarget;
    private Vector2 _pushDirection;
    private float _pushTimer;

    private readonly HashSet<Door> _nearbyDoors = [];

    private Ladder? _activeLadder;

    // Remember which ladders Link has descended.
    // This prevents exiting an entrance ladder immediately.
    private readonly HashSet<Ladder> _laddersDescended = [];

    public Ladder? ActiveLadder => _activeLadder;

    public override void _Ready()
    {
        MotionMode = MotionModeEnum.Floating;
        SafeMargin = 0.001f;
    }

    public override void _PhysicsProcess(double delta)
    {
        var horizontal = Input.GetAxis(
            "move_left",
            "move_right"
        );

        var vertical = Input.GetAxis(
            "move_up",
            "move_down"
        );

        if (_activeLadder is not null)
        {
            MoveOnLadder(
                _activeLadder,
                horizontal,
                vertical,
                (float)delta
            );

            return;
        }

        if (MovementMode == PlayerMovementMode.SideView)
        {
            Walk(horizontal);
            return;
        }

        MoveTopDown(horizontal, vertical, (float)delta);
    }

    private void MoveTopDown(
        float horizontal,
        float vertical,
        float delta
    )
    {
        var direction = new Vector2(
            horizontal,
            vertical
        ).Normalized();

        var movementVelocity = direction * Speed;

        if (direction != Vector2.Zero)
        {
            movementVelocity += GetDoorAlignmentVelocity(
                direction
            );
        }

        Velocity = movementVelocity;

        MoveAndSlide();

        HandlePush(direction, delta);
    }

    private void Walk(float horizontal)
    {
        Velocity = new Vector2(
            horizontal * Speed,
            0
        );

        MoveAndSlide();
    }

    public void SpawnAtLadder(Ladder ladder)
    {
        _activeLadder = null;
        _laddersDescended.Clear();

        var bounds = ladder.Bounds;

        GlobalPosition = new Vector2(
            bounds.GetCenter().X,
            bounds.Position.Y + GetHalfHeight()
        );

        Velocity = Vector2.Zero;

        EnterLadder(ladder);
    }

    public void EnterLadder(Ladder ladder)
    {
        if (_activeLadder == ladder)
        {
            return;
        }

        if (_activeLadder is not null)
        {
            return;
        }

        _activeLadder = ladder;
        Velocity = Vector2.Zero;
    }

    public void LeaveLadder(Ladder ladder)
    {
        if (_activeLadder != ladder)
        {
            return;
        }

        var bounds = ladder.Bounds;
        var halfHeight = GetHalfHeight();

        var bottomY = bounds.End.Y - halfHeight;

        LadderExitDirection direction;

        if (ladder.ExitsSceneAtTop)
        {
            var exitY = bounds.Position.Y - halfHeight;

            direction =
                _laddersDescended.Contains(ladder) &&
                GlobalPosition.Y <= exitY + LandingTolerance
                    ? LadderExitDirection.Top
                    : GlobalPosition.Y >= bottomY - LandingTolerance
                        ? LadderExitDirection.Bottom
                        : LadderExitDirection.Side;
        }
        else
        {
            var topY = bounds.Position.Y +
                halfHeight - LadderTopClearance;

            direction =
                GlobalPosition.Y <= topY + LandingTolerance
                    ? LadderExitDirection.Top
                    : GlobalPosition.Y >= bottomY - LandingTolerance
                        ? LadderExitDirection.Bottom
                        : LadderExitDirection.Side;
        }

        FinishLadder(ladder, direction);
    }

    private void FinishLadder(
        Ladder ladder,
        LadderExitDirection direction
    )
    {
        if (_activeLadder != ladder)
        {
            return;
        }

        _activeLadder = null;
        Velocity = Vector2.Zero;

        ladder.NotifyPlayerExited(this, direction);
    }

    private void MoveOnLadder(
        Ladder ladder,
        float horizontal,
        float vertical,
        float delta
    )
    {
        var bounds = ladder.Bounds;
        var position = GlobalPosition;

        var halfHeight = GetHalfHeight();

        var bottomY = bounds.End.Y - halfHeight;

        var hasDescended = _laddersDescended.Contains(
            ladder
        );

        var topY = ladder.ExitsSceneAtTop
            ? bounds.Position.Y +
                (hasDescended ? -halfHeight : halfHeight)
            : bounds.Position.Y +
                halfHeight - LadderTopClearance;

        var atBottom = position.Y >=
            bottomY - LandingTolerance;

        if (atBottom)
        {
            _laddersDescended.Add(ladder);
        }

        var wantsHorizontal =
            !Mathf.IsZeroApprox(horizontal);

        // Allow Link to walk away from the bottom.
        if (atBottom && wantsHorizontal)
        {
            Walk(horizontal);
            return;
        }

        // At the top of a platform ladder, automatically
        // finish the climb before walking horizontally.
        if (!ladder.ExitsSceneAtTop &&
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

                Velocity = new Vector2(
                    0,
                    (nextY - position.Y) / delta
                );

                MoveAndSlide();
                return;
            }

            Walk(horizontal);
            return;
        }

        // Centre Link before climbing vertically.
        var centreX = bounds.GetCenter().X;

        if (!Mathf.IsZeroApprox(vertical) &&
            Mathf.Abs(position.X - centreX) > 0.5f)
        {
            var alignedX = Mathf.MoveToward(
                position.X,
                centreX,
                Speed * delta
            );

            Velocity = new Vector2(
                (alignedX - position.X) / delta,
                0
            );

            MoveAndSlide();
            return;
        }

        // Restrict movement to the ladder's vertical axis.
        var nextPositionY = Mathf.Clamp(
            position.Y + vertical * ClimbSpeed * delta,
            topY,
            bottomY
        );

        Velocity = new Vector2(
            0,
            (nextPositionY - position.Y) / delta
        );

        MoveAndSlide();

        // A scene-exit ladder only fires after Link has
        // reached the bottom and climbed all the way back.
        if (ladder.ExitsSceneAtTop &&
            _laddersDescended.Contains(ladder) &&
            GlobalPosition.Y <= topY + 0.01f &&
            vertical < 0)
        {
            FinishLadder(
                ladder,
                LadderExitDirection.Top
            );
        }
    }

    private float GetHalfHeight()
    {
        var shape = GetNode<CollisionShape2D>(
            "CollisionShape2D"
        ).Shape as RectangleShape2D;

        if (shape is null)
        {
            throw new System.InvalidOperationException(
                "Player requires a RectangleShape2D."
            );
        }

        return shape.Size.Y / 2.0f;
    }

    public void RegisterNearbyDoor(Door door)
    {
        _nearbyDoors.Add(door);
    }

    public void UnregisterNearbyDoor(Door door)
    {
        _nearbyDoors.Remove(door);
    }

    private Vector2 GetDoorAlignmentVelocity(
        Vector2 movementDirection
    )
    {
        var closestDistance = float.PositiveInfinity;
        var alignmentVelocity = Vector2.Zero;

        foreach (var door in _nearbyDoors)
        {
            if (!GodotObject.IsInstanceValid(door))
            {
                continue;
            }

            if (!door.TryGetAlignmentVelocity(
                    GlobalPosition,
                    movementDirection,
                    out var candidateVelocity))
            {
                continue;
            }

            var distance = GlobalPosition.DistanceSquaredTo(
                door.GlobalPosition
            );

            if (distance >= closestDistance)
            {
                continue;
            }

            closestDistance = distance;
            alignmentVelocity = candidateVelocity;
        }

        return alignmentVelocity;
    }

    private void HandlePush(
        Vector2 direction,
        float delta
    )
    {
        PushableBlock? target = null;

        if (direction != Vector2.Zero)
        {
            for (var i = 0; i < GetSlideCollisionCount(); i++)
            {
                var collision = GetSlideCollision(i);

                if (collision.GetCollider()
                    is not PushableBlock block)
                {
                    continue;
                }

                if (direction.Dot(
                    collision.GetNormal()
                ) < -0.9f)
                {
                    target = block;
                    break;
                }
            }
        }

        if (target is null)
        {
            ResetPush();
            return;
        }

        if (_pushTarget != target ||
            _pushDirection != direction)
        {
            _pushTarget = target;
            _pushDirection = direction;
            _pushTimer = 0.0f;
        }

        _pushTimer += delta;

        if (_pushTimer < target.PushDelaySeconds)
        {
            return;
        }

        target.TryPush(direction, this);
        ResetPush();
    }

    private void ResetPush()
    {
        _pushTarget = null;
        _pushDirection = Vector2.Zero;
        _pushTimer = 0.0f;
    }
}
