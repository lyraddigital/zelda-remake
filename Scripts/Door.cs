
using Godot;
using System;
using ZeldaRemake.Doors;
using ZeldaRemake.Models;

public partial class Door : Node2D
{
    private ColorRect _opening = null!;
    private CollisionShape2D _collision = null!;
    private Area2D _unlockArea = null!;

    private DungeonState _state = null!;
    private DoorData _data = null!;
    private IDoorBehavior _behavior = null!;

    private string _roomId = string.Empty;

    public override void _Ready()
    {
        _opening = GetNode<ColorRect>("Opening");

        _collision = GetNode<CollisionShape2D>(
            "Blocker/CollisionShape2D"
        );

        _unlockArea = GetNode<Area2D>("UnlockArea");
        _unlockArea.BodyEntered += OnBodyEntered;
    }

    public override void _ExitTree()
    {
        if (_state is not null)
        {
            _state.ShutterOpened -= OnShutterChanged;
            _state.ShutterClosed -= OnShutterChanged;
        }

        if (_unlockArea is not null)
        {
            _unlockArea.BodyEntered -= OnBodyEntered;
        }
    }

    public void Configure(
        DoorData data,
        DungeonState state,
        string roomId
    )
    {
        if (_state is not null)
        {
            _state.ShutterOpened -= OnShutterChanged;
            _state.ShutterClosed -= OnShutterChanged;
        }

        _data = data;
        _state = state;
        _roomId = roomId;

        _behavior = data.Type.ToLowerInvariant() switch
        {
            "open" => new StandardDoorBehavior(),
            "locked" => new LockedDoorBehavior(),
            "shutter" => new ShutterDoorBehavior(),

            _ => throw new InvalidOperationException(
                $"Unsupported door type: {data.Type}"
            )
        };

        if (_behavior is ShutterDoorBehavior)
        {
            _state.ShutterOpened += OnShutterChanged;
            _state.ShutterClosed += OnShutterChanged;
        }

        UpdateAppearance();
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is not Player)
        {
            return;
        }

        if (_behavior.IsOpen(_state, _roomId, _data))
        {
            return;
        }

        if (!_behavior.TryOpen(_state, _roomId, _data))
        {
            return;
        }

        UpdateAppearance();

        GD.Print(
            $"Door unlocked. Keys remaining: {_state.Keys}"
        );
    }

    private void OnShutterChanged(
        string roomId,
        string direction
    )
    {
        if (!string.Equals(
                roomId,
                _roomId,
                StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(
                direction,
                _data.Direction,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        UpdateAppearance();
    }

    private void UpdateAppearance()
    {
        var isOpen = _behavior.IsOpen(
            _state,
            _roomId,
            _data
        );

        _opening.Color = isOpen
            ? new Color("#202020")
            : new Color("#A66B32");

        _collision.SetDeferred(
            CollisionShape2D.PropertyName.Disabled,
            isOpen
        );
    }
}
