
using Godot;
using System.Collections.Generic;

public partial class Player : CharacterBody2D
{
    [Export]
    public float Speed { get; set; } = 80.0f;

    private PushableBlock? _pushTarget;
    private Vector2 _pushDirection;
    private float _pushTimer;
    private readonly HashSet<Door> _nearbyDoors = [];

    public override void _Ready()
    {
        MotionMode = MotionModeEnum.Floating;
        SafeMargin = 0.001f;
    }

    public override void _PhysicsProcess(double delta)
    {
        var horizontal = Input.GetAxis("move_left", "move_right");
        var vertical = Input.GetAxis("move_up", "move_down");

        // Four-direction movement, with horizontal priority.
        var direction = horizontal != 0
            ? new Vector2(horizontal, 0)
            : new Vector2(0, vertical);

        var movementVelocity = direction * Speed;

        if (direction != Vector2.Zero)
        {
            movementVelocity += GetDoorAlignmentVelocity(direction);
        }

        Velocity = movementVelocity.LimitLength(Speed);

        MoveAndSlide();

        HandlePush(direction, (float)delta);
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

    private void HandlePush(Vector2 direction, float delta)
    {
        PushableBlock? target = null;

        if (direction != Vector2.Zero)
        {
            for (var i = 0; i < GetSlideCollisionCount(); i++)
            {
                var collision = GetSlideCollision(i);

                if (collision.GetCollider() is not PushableBlock block)
                {
                    continue;
                }

                // Only push when moving into the block's face.
                if (direction.Dot(collision.GetNormal()) < -0.9f)
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

        if (_pushTarget != target || _pushDirection != direction)
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
