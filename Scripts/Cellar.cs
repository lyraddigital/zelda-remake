
using Godot;
using System;
using ZeldaRemake.Models;

public partial class Cellar : CanvasLayer
{
    private const float MovementSpeed = 80.0f;
    private const float ClimbSpeed = 64.0f;

    private const float LeftLadderCenterX = 32.0f;
    private const float RightLadderCenterX = 176.0f;

    private const float LeftLadderAccessX = 38.0f;
    private const float RightLadderAccessX = 160.0f;

    private const float FloorY = 188.0f;
    private const float PlayfieldTop = 64.0f;

    private const float RightLadderTopY = 164.0f;
    private const float RightPlatformMaxX = 228.0f;

    private enum LadderSide
    {
        Left,
        Right
    }

    private ColorRect _link = null!;
    private ColorRect? _treasure;
    private DungeonState? _state;
    private string? _itemId;

    private LadderSide _currentLadder = LadderSide.Left;

    private float LeftLinkX =>
        LeftLadderCenterX - _link.Size.X / 2.0f;

    private float RightLinkX =>
        RightLadderCenterX - _link.Size.X / 2.0f;

    public string CellarId { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public event Action? ExitRequested;

    public override void _Ready()
    {
        _link = GetNode<ColorRect>("Link");

        // Link enters at the top of the left staircase.
        _link.Position = new Vector2(
            LeftLinkX,
            PlayfieldTop
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

        // Treasure already exists in ItemCellar.tscn.
        _treasure = GetNode<ColorRect>("Treasure");

        // Preserve the collected item state.
        _treasure.Visible = !state.HasItem(_itemId);
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

        if (_currentLadder == LadderSide.Right &&
            position.Y <= RightLadderTopY + 0.01f &&
            position.X >= RightLinkX - 0.01f)
        {
            // Link is in the raised treasure compartment.
            if (position.X <= RightLinkX + 0.01f &&
                vertical > 0.0f)
            {
                // Descend the short ladder.
                position.Y = Mathf.MoveToward(
                    position.Y,
                    FloorY,
                    ClimbSpeed * frameDelta
                );
            }
            else
            {
                // Walk along the raised platform.
                position.X = Mathf.Clamp(
                    position.X + horizontal * MovementSpeed * frameDelta,
                    RightLinkX,
                    RightPlatformMaxX
                );
            }
        }
        else if (position.Y < FloorY - 0.01f)
        {
            // Link is climbing either staircase.
            var ladderX = _currentLadder == LadderSide.Left
                ? LeftLinkX
                : RightLinkX;

            var topY = _currentLadder == LadderSide.Left
                ? PlayfieldTop - _link.Size.Y
                : RightLadderTopY;

            position.X = ladderX;

            position.Y = Mathf.Clamp(
                position.Y + vertical * ClimbSpeed * frameDelta,
                topY,
                FloorY
            );
        }
        else if (vertical < 0.0f &&
                 TryFindLadder(position.X, out var ladder))
        {
            // Align Link with a ladder before climbing.
            var ladderX = ladder == LadderSide.Left
                ? LeftLinkX
                : RightLinkX;

            position.X = Mathf.MoveToward(
                position.X,
                ladderX,
                MovementSpeed * frameDelta
            );

            if (Mathf.Abs(position.X - ladderX) < 0.01f)
            {
                _currentLadder = ladder;

                var topY = ladder == LadderSide.Left
                    ? PlayfieldTop - _link.Size.Y
                    : RightLadderTopY;

                position.Y = Mathf.MoveToward(
                    position.Y,
                    topY,
                    ClimbSpeed * frameDelta
                );
            }
        }
        else
        {
            // Horizontal movement along the main passage.
            position.X = Mathf.Clamp(
                position.X + horizontal * MovementSpeed * frameDelta,
                LeftLinkX,
                RightLinkX
            );
        }

        _link.Position = position;

        TryCollectTreasure();
        TryExit();
    }


    private static bool TryFindLadder(
        float x,
        out LadderSide ladder
    )
    {
        if (x <= LeftLadderAccessX)
        {
            ladder = LadderSide.Left;
            return true;
        }

        // Link must approach the treasure ladder.
        // Holding Up from anywhere else in the
        // compartment must not pull him towards it.
        if (x >= RightLadderAccessX &&
            x <= RightLadderCenterX + 10.0f)
        {
            ladder = LadderSide.Right;
            return true;
        }

        ladder = default;
        return false;
    }


    public bool TryCollectTreasure()
    {
        if (!IsActive ||
            _treasure is null ||
            !_treasure.Visible ||
            _state is null ||
            _itemId is null ||
            !_link.GetRect().Intersects(_treasure.GetRect()))
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
        var exitY = PlayfieldTop - _link.Size.Y;

        if (!IsActive ||
            _currentLadder != LadderSide.Left ||
            Mathf.Abs(_link.Position.X - LeftLinkX) > 0.01f ||
            _link.Position.Y > exitY + 0.01f)
        {
            return false;
        }

        Deactivate();
        ExitRequested?.Invoke();

        return true;
    }
}
