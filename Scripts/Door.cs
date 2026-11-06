
using Godot;
using System;
using ZeldaRemake.Models;

public partial class Door : Node2D
{
    private ColorRect _opening = null!;
    private CollisionShape2D _collision = null!;
    private Area2D _unlockArea = null!;
    private DungeonState _state = null!;
    private DoorData _data = null!;

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
            _state.ShutterOpened -= OnShutterOpened;
        }
    }

    public void Configure(
        DoorData data,
        DungeonState state,
        string roomId
    )
    {
        _data = data;
        _state = state;
        _roomId = roomId;

        _state.ShutterOpened += OnShutterOpened;

        UpdateAppearance();
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is not Player)
        {
            return;
        }

        if (_state.IsDoorOpen(_roomId, _data))
        {
            return;
        }

        if (!_state.TryUnlockDoor(_roomId, _data))
        {
            return;
        }

        UpdateAppearance();

        GD.Print(
            $"Door unlocked. Keys remaining: {_state.Keys}"
        );
    }

    private void OnShutterOpened(string roomId, string direction)
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
        var isOpen = _state.IsDoorOpen(_roomId, _data);

        _opening.Color = isOpen
            ? new Color("#202020")
            : new Color("#A66B32");

        _collision.SetDeferred(
            CollisionShape2D.PropertyName.Disabled,
            isOpen
        );
    }
}
