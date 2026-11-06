
using Godot;

public partial class Player : CharacterBody2D
{
    [Export]
    public float Speed { get; set; } = 80.0f;

    private PushableBlock? _pushTarget;
    private Vector2 _pushDirection;
    private float _pushTimer;

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

        Velocity = direction * Speed;

        MoveAndSlide();

        HandlePush(direction, (float)delta);
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
