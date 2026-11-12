
using Godot;
using System;
using ZeldaRemake.Models;

public partial class Cellar : CanvasLayer
{
    private const float MovementSpeed = 80.0f;
    private const float ClimbSpeed = 64.0f;

    private const float LeftLadderCenterX = 32.0f;
    private const float RightLadderCenterX = 224.0f;

    private const float LeftLadderAccessX = 38.0f;
    private const float RightLadderAccessX = 206.0f;

    private const float FloorY = 188.0f;
    private const float PlayfieldTop = 64.0f;
    private const float RightLadderTopY = 112.0f;

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

        // Set the starting position before the cellar renders.
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

        CreateRightLadder();

        if (!state.HasItem(_itemId))
        {
            CreateTreasure();
        }
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

        if (position.Y < FloorY - 0.01f)
        {
            // Link is already on one of the ladders.
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
            // Align Link with the selected ladder.
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
            // Side-view movement on the cellar floor.
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

        if (x >= RightLadderAccessX)
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

    private void CreateRightLadder()
    {
        var ladder = new ColorRect
        {
            Name = "TreasureLadder",
            Position = new Vector2(216, 112),
            Size = new Vector2(16, 88),
            Color = new Color("#B58B42"),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };

        AddChild(ladder);

        // Keep the player rendered in front of the ladder.
        MoveChild(ladder, _link.GetIndex());
    }

    private void CreateTreasure()
    {
        _treasure = new ColorRect
        {
            Name = "Treasure",
            Position = new Vector2(218, 104),
            Size = new Vector2(12, 12),
            Color = new Color("#F2D35E"),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };

        AddChild(_treasure);

        // The player renders over the treasure on contact.
        MoveChild(_treasure, _link.GetIndex());
    }
}
