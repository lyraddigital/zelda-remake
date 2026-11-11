
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
        if (!IsActive)
        {
            return;
        }

        var position = _link.Position;
        var frameDelta = (float)delta;

        var horizontal = Input.GetAxis(
            "move_left",
            "move_right"
        );

        var vertical = Input.GetAxis(
            "move_up",
            "move_down"
        );

        var exitY = PlayfieldTop - _link.Size.Y;

        if (position.Y < FloorY - 0.01f)
        {
            // Link is already climbing the ladder.
            // Only vertical movement is permitted.
            position.X = LadderX;

            position.Y = Mathf.Clamp(
                position.Y + vertical * ClimbSpeed * frameDelta,
                exitY,
                FloorY
            );
        }
        else if (vertical < 0.0f &&
                 position.X <= LadderAccessX)
        {
            // Align Link with the ladder without teleporting.
            position.X = Mathf.MoveToward(
                position.X,
                LadderX,
                MovementSpeed * frameDelta
            );

            // Start climbing once aligned.
            if (Mathf.Abs(position.X - LadderX) < 0.01f)
            {
                position.Y = Mathf.MoveToward(
                    position.Y,
                    exitY,
                    ClimbSpeed * frameDelta
                );
            }
        }
        else
        {
            // Normal side-view horizontal movement.
            position.X = Mathf.Clamp(
                position.X + horizontal * MovementSpeed * frameDelta,
                LadderX,
                MaximumX
            );
        }

        _link.Position = position;

        // Return only after Link has completely
        // disappeared above the playfield.
        TryExit();
    }

    public bool TryExit()
    {
        var exitY = PlayfieldTop - _link.Size.Y;

        if (!IsActive ||
            Mathf.Abs(_link.Position.X - LadderX) > 0.01f ||
            _link.Position.Y > exitY + 0.01f)
        {
            return false;
        }

        Deactivate();
        ExitRequested?.Invoke();

        return true;
    }
}
