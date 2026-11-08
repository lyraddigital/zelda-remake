
using Godot;
using System;
using System.Collections.Generic;
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
    private readonly HashSet<Player> _nearbyPlayers = [];

    public override void _Ready()
    {
        _opening = GetNode<ColorRect>("Opening");

        _collision = GetNode<CollisionShape2D>(
            "Blocker/CollisionShape2D"
        );

        _unlockArea = GetNode<Area2D>("UnlockArea");
        _unlockArea.BodyEntered += OnBodyEntered;
        _unlockArea.BodyExited += OnBodyExited;
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
            _unlockArea.BodyExited -= OnBodyExited;
        }

        foreach (var player in _nearbyPlayers)
        {
            if (GodotObject.IsInstanceValid(player))
            {
                player.UnregisterNearbyDoor(this);
            }
        }

        _nearbyPlayers.Clear();
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
        if (body is not Player player)
        {
            return;
        }

        _nearbyPlayers.Add(player);
        player.RegisterNearbyDoor(this);

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

    private void OnBodyExited(Node2D body)
    {
        if (body is not Player player)
        {
            return;
        }

        _nearbyPlayers.Remove(player);
        player.UnregisterNearbyDoor(this);
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


    public bool TryGetAlignmentVelocity(
        Vector2 playerPosition,
        Vector2 movementDirection,
        out Vector2 alignmentVelocity
    )
    {
        alignmentVelocity = Vector2.Zero;

        if (_state is null ||
            _behavior is null ||
            _data is null ||
            !_behavior.IsOpen(_state, _roomId, _data))
        {
            return false;
        }

        var center = GlobalPosition;

        switch (_data.Direction.ToLowerInvariant())
        {
            case "north":
                if (movementDirection.Y >= 0 ||
                    playerPosition.Y < center.Y)
                {
                    return false;
                }

                alignmentVelocity = new Vector2(
                    DoorAlignment.GetLateralVelocity(
                        playerPosition.X,
                        center.X
                    ),
                    0
                );

                return true;

            case "south":
                if (movementDirection.Y <= 0 ||
                    playerPosition.Y > center.Y)
                {
                    return false;
                }

                alignmentVelocity = new Vector2(
                    DoorAlignment.GetLateralVelocity(
                        playerPosition.X,
                        center.X
                    ),
                    0
                );

                return true;

            case "east":
                if (movementDirection.X <= 0 ||
                    playerPosition.X > center.X)
                {
                    return false;
                }

                alignmentVelocity = new Vector2(
                    0,
                    DoorAlignment.GetLateralVelocity(
                        playerPosition.Y,
                        center.Y
                    )
                );

                return true;

            case "west":
                if (movementDirection.X >= 0 ||
                    playerPosition.X < center.X)
                {
                    return false;
                }

                alignmentVelocity = new Vector2(
                    0,
                    DoorAlignment.GetLateralVelocity(
                        playerPosition.Y,
                        center.Y
                    )
                );

                return true;

            default:
                return false;
        }
    }
}
