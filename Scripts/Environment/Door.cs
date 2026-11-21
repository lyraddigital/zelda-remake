
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

        const float approachDistance = 24.0f;

        var forward = _data.Direction.ToLowerInvariant() switch
        {
            "north" => Vector2.Up,
            "south" => Vector2.Down,
            "east" => Vector2.Right,
            "west" => Vector2.Left,
            _ => Vector2.Zero
        };

        if (forward == Vector2.Zero ||
            movementDirection.Dot(forward) < 0.95f)
        {
            return false;
        }

        var center = GlobalPosition;

        var distance = (center - playerPosition).Dot(forward);

        // Only align immediately before reaching the doorway.
        if (distance <= 0.0f || distance > approachDistance)
        {
            return false;
        }

        // Horizontal travel needs vertical correction and vice versa.
        var horizontalTravel = forward.X != 0.0f;

        var playerCoordinate = horizontalTravel
            ? playerPosition.Y
            : playerPosition.X;

        var doorCoordinate = horizontalTravel
            ? center.Y
            : center.X;

        var correction = DoorAlignment.GetLateralVelocity(
            playerCoordinate,
            doorCoordinate
        );

        if (Mathf.IsZeroApprox(correction))
        {
            return false;
        }

        alignmentVelocity = horizontalTravel
            ? new Vector2(0, correction)
            : new Vector2(correction, 0);

        return true;
    }
}
