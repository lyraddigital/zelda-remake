
using Godot;
using System;

public partial class Cellar : CanvasLayer
{
    private const float MovementSpeed = 80.0f;
    private const float ClimbSpeed = 64.0f;

    private const float LadderX = 24.0f;
    private const float LadderAccessX = 38.0f;

    private const float FloorY = 188.0f;
    private const float PlayfieldTop = 64.0f;
    private const float MaximumX = 232.0f;

    private ColorRect _link = null!;

    public string CellarId { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public event Action? ExitRequested;

    public override void _Ready()
    {
        _link = GetNode<ColorRect>("Link");
        SetPhysicsProcess(false);
    }

    public void Configure(string cellarId)
    {
        CellarId = cellarId;
    }

    public void Activate()
    {
        IsActive = true;
        SetPhysicsProcess(true);
    }

    public void Deactivate()
    {
        IsActive = false;
        SetPhysicsProcess(false);
    }

    public override void _PhysicsProcess(double delta)
    {
        var position = _link.Position;
        var distance = (float)delta;

        var nearLadder = position.X <= LadderAccessX;

        if (nearLadder && Input.IsActionPressed("move_up"))
        {
            // Climb vertically until Link is fully above
            // the top of the gameplay area.
            position.X = LadderX;

            position.Y = Mathf.MoveToward(
                position.Y,
                PlayfieldTop - _link.Size.Y,
                ClimbSpeed * distance
            );
        }
        else if (nearLadder &&
                 Input.IsActionPressed("move_down") &&
                 position.Y < FloorY)
        {
            // Allow Link to descend the same staircase.
            position.X = LadderX;

            position.Y = Mathf.MoveToward(
                position.Y,
                FloorY,
                ClimbSpeed * distance
            );
        }
        else if (position.Y >= FloorY)
        {
            // Horizontal movement is only allowed
            // at the bottom of the cellar.
            var horizontal = Input.GetAxis(
                "move_left",
                "move_right"
            );

            position.X = Mathf.Clamp(
                position.X + horizontal * MovementSpeed * distance,
                LadderX,
                MaximumX
            );
        }

        _link.Position = position;

        // Exit automatically when the climb is complete.
        TryExit();
    }

    public bool TryExit()
    {
        var exitY = PlayfieldTop - _link.Size.Y;

        if (!IsActive ||
            Mathf.Abs(_link.Position.X - LadderX) > 1.0f ||
            _link.Position.Y > exitY + 0.01f)
        {
            return false;
        }

        Deactivate();
        ExitRequested?.Invoke();

        return true;
    }
}
