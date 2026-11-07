
using Godot;
using System;

public partial class PushableBlock : AnimatableBody2D
{
    private const float TileSize = 16.0f;

    [Export]
    public float PushDelaySeconds { get; set; } = 0.27f;

    [Export]
    public float PushDurationSeconds { get; set; } = 0.53f;

    private CollisionShape2D _collision = null!;

    private Vector2 _allowedDirection = Vector2.Zero;
    private bool _hasMoved;
    private bool _pushDisabled;
    private bool _isMoving;
    private Vector2 _initialPosition;
    private Tween? _movementTween;
    public event Action? Pushed;

    public override void _Ready()
    {
        _collision = GetNode<CollisionShape2D>(
            "CollisionShape2D"
        );

        _initialPosition = Position;
    }

    public void Configure(string pushDirection)
    {
        _allowedDirection = pushDirection.ToLowerInvariant() switch
        {
            "north" => Vector2.Up,
            "south" => Vector2.Down,
            "east" => Vector2.Right,
            "west" => Vector2.Left,
            "any" => Vector2.Zero,
            _ => throw new ArgumentException(
                $"Invalid push direction: {pushDirection}"
            )
        };
    }

    public void Reset()
    {
        _movementTween?.Kill();
        _movementTween = null;

        Position = _initialPosition;

        _hasMoved = false;
        _isMoving = false;
    }

    public void DisablePushing()
    {
        _pushDisabled = true;
    }

    public void RestoreCompletedPosition()
    {
        if (_allowedDirection == Vector2.Zero)
        {
            throw new InvalidOperationException(
                "A permanent staircase block requires a fixed push direction."
            );
        }

        _movementTween?.Kill();
        _movementTween = null;

        Position = _initialPosition + _allowedDirection * TileSize;

        _hasMoved = true;
        _isMoving = false;
    }

    public bool TryPush(Vector2 direction, Player player)
    {
        if (_hasMoved || _isMoving || _pushDisabled)
        {
            return false;
        }

        if (_allowedDirection != Vector2.Zero &&
            direction != _allowedDirection)
        {
            return false;
        }

        if (direction != Vector2.Up &&
            direction != Vector2.Down &&
            direction != Vector2.Left &&
            direction != Vector2.Right)
        {
            return false;
        }

        var movement = direction * TileSize;
        var destination = GlobalPosition + movement;

        // Check the destination against physical obstacles.
        var query = new PhysicsShapeQueryParameters2D
        {
            Shape = new RectangleShape2D
            {
                Size = new Vector2(15, 15)
            },
            Transform = new Transform2D(0.0f, destination),
            CollisionMask = CollisionMask,
            CollideWithBodies = true,
            CollideWithAreas = false,
            Margin = 0.0f,
            Exclude = new Godot.Collections.Array<Rid>
            {
                GetRid(),
                player.GetRid()
            }
        };

        var collisions = GetWorld2D()
            .DirectSpaceState
            .IntersectShape(query, 1);

        if (collisions.Count > 0)
        {
            return false;
        }

        // Reserve this block's one permitted push.
        _hasMoved = true;
        _isMoving = true;

        var tween = CreateTween();

        _movementTween = tween;

        tween.SetProcessMode(
            Tween.TweenProcessMode.Physics
        );

        tween.SetTrans(Tween.TransitionType.Linear);

        tween.TweenProperty(
            this,
            "position",
            Position + movement,
            PushDurationSeconds
        );

        tween.Finished += () =>
        {
            _isMoving = false;
            Pushed?.Invoke();

            _movementTween = null;

            GD.Print("Pushable block moved.");
        };

        return true;
    }
}
