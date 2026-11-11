
using Godot;
using System;

public partial class Cellar : CanvasLayer
{
    private const float MovementSpeed = 80.0f;
    private const float ClimbSpeed = 64.0f;

    // The ladder extends from X=24 to X=40.
    // Link must be centred within its 16px width.
    private const float LadderCenterX = 32.0f;
    private const float LadderAccessX = 38.0f;

    private const float FloorY = 188.0f;
    private const float PlayfieldTop = 64.0f;
    private const float MaximumX = 232.0f;

    private ColorRect _link = null!;

    private float LadderLinkX =>
        LadderCenterX - _link.Size.X / 2.0f;

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
        // Link enters the cellar at the top of the ladder.
        _link.Position = new Vector2(
            LadderLinkX,
            PlayfieldTop
        );

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
            // Link is on the ladder.
            // He can climb up or down but not sideways.
            position.X = LadderLinkX;

            position.Y = Mathf.Clamp(
                position.Y + vertical * ClimbSpeed * frameDelta,
                exitY,
                FloorY
            );
        }
        else if (vertical < 0.0f &&
                 position.X <= LadderAccessX)
        {
            // Link approaches the ladder from the floor.
            position.X = Mathf.MoveToward(
                position.X,
                LadderLinkX,
                MovementSpeed * frameDelta
            );

            if (Mathf.Abs(position.X - LadderLinkX) < 0.01f)
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
            // Normal horizontal movement on the cellar floor.
            position.X = Mathf.Clamp(
                position.X + horizontal * MovementSpeed * frameDelta,
                LadderLinkX,
                MaximumX
            );
        }

        _link.Position = position;

        // Exit only after Link has climbed fully above
        // the gameplay area.
        TryExit();
    }

    public bool TryExit()
    {
        var exitY = PlayfieldTop - _link.Size.Y;

        if (!IsActive ||
            Mathf.Abs(_link.Position.X - LadderLinkX) > 0.01f ||
            _link.Position.Y > exitY + 0.01f)
        {
            return false;
        }

        Deactivate();
        ExitRequested?.Invoke();

        return true;
    }
}
