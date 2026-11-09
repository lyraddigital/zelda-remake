
using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using ZeldaRemake.Geometry;
using ZeldaRemake.Models;

public partial class DungeonManager : Node2D
{
    private const float TransitionDuration = 0.6f;

    [Export]
    public PackedScene RoomScene { get; set; } = null!;

    [Export]
    public string DungeonPath { get; set; } = "res://Data/Dungeons/dungeon01.json";

    private Node2D _rooms = null!;
    private Player _player = null!;
    private Camera2D _camera = null!;

    private readonly Dictionary<string, RoomData> _roomLookup = new();

    private DungeonState _state = new();
    private string _dungeonId = string.Empty;

    private RoomData _currentRoom = null!;
    private bool _isTransitioning;
    private RoomData? _pendingEntryRoom;
    private DoorData? _pendingEntryShutter;

    public override void _Ready()
    {
        SetPhysicsProcess(false);

        _rooms = GetNode<Node2D>("Rooms");
        _player = GetNode<Player>("Player");
        _camera = GetNode<Camera2D>("Camera2D");

        if (RoomScene is null)
        {
            GD.PushError("Room Scene has not been assigned.");
            return;
        }

        try
        {
            LoadDungeon(DungeonPath);
            SetPhysicsProcess(true);
        }
        catch (Exception exception)
        {
            GD.PushError($"Failed to load dungeon: {exception}");
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!@event.IsActionPressed("save_game"))
        {
            return;
        }

        try
        {
            SaveGame();
        }
        catch (Exception exception)
        {
            GD.PushError($"Failed to save game: {exception}");
        }

        GetViewport().SetInputAsHandled();
    }


    private void LoadDungeon(string path)
    {
        if (!Godot.FileAccess.FileExists(path))
        {
            throw new InvalidOperationException(
                $"Dungeon configuration not found: {path}"
            );
        }

        var json = Godot.FileAccess.GetFileAsString(path);

        var dungeon = JsonSerializer.Deserialize<DungeonData>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }
        ) ?? throw new InvalidOperationException(
            "Dungeon configuration is invalid."
        );

        _roomLookup.Clear();

        foreach (var roomData in dungeon.Rooms)
        {
            if (!_roomLookup.TryAdd(roomData.Id, roomData))
            {
                throw new InvalidOperationException(
                    $"Duplicate room ID: {roomData.Id}"
                );
            }
        }

        if (!_roomLookup.TryGetValue(
            dungeon.StartingRoom,
            out var startingRoom))
        {
            throw new InvalidOperationException(
                $"Starting room not found: {dungeon.StartingRoom}"
            );
        }

        _dungeonId = dungeon.Id;

        // Load a saved game, if one exists.
        var save = DungeonSaveService.Load(_dungeonId);

        _state = new DungeonState();

        if (save is not null)
        {
            _state.Restore(save);
        }

        _state.Changed += AutoSave;

        // Instantiate the dungeon rooms after restoring state.
        foreach (var roomData in dungeon.Rooms)
        {
            CreateRoom(roomData);
        }

        // Always start at the dungeon's configured entrance.
        _currentRoom = startingRoom;

        var origin = GetRoomOrigin(startingRoom);

        _player.Position = origin + new Vector2(128, 88);

        _camera.Position = GetCameraPosition(startingRoom);
        _camera.MakeCurrent();

        GD.Print(
            $"Loaded dungeon '{dungeon.Name}' with {dungeon.Rooms.Count} rooms."
        );
    }

    private void AutoSave()
    {
        if (_isTransitioning || string.IsNullOrEmpty(_dungeonId))
        {
            return;
        }

        try
        {
            SaveGame();
        }
        catch (Exception exception)
        {
            GD.PushError($"Auto-save failed: {exception}");
        }
    }

    private void SaveGame()
    {
        if (_isTransitioning || string.IsNullOrEmpty(_dungeonId))
        {
            return;
        }

        var save = new DungeonSaveData
        {
            DungeonId = _dungeonId
        };

        _state.CopyTo(save);

        DungeonSaveService.Save(save);
    }

    private void CreateRoom(RoomData roomData)
    {
        var room = RoomScene.Instantiate<Room>();

        room.Name = roomData.Id;
        room.Position = GetRoomOrigin(roomData);

        _rooms.AddChild(room);

        room.LoadLayout(roomData, _state);

        GD.Print($"Created room: {roomData.Id}");
    }

    private static Vector2 GetRoomOrigin(RoomData room)
    {
        var origin = DungeonRoomGeometry.GetRoomOrigin(
            room.Position
        );

        return new Vector2(origin.X, origin.Y);
    }

    private static Vector2 GetCameraPosition(RoomData room)
    {
        var position = DungeonRoomGeometry.GetCameraPosition(
            room.Position
        );

        return new Vector2(position.X, position.Y);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_isTransitioning)
        {
            return;
        }

        TryCloseEntryShutter();

        var origin = GetRoomOrigin(_currentRoom);

        if (_player.Position.X >= origin.X + DungeonRoomGeometry.RoomWidth)
        {
            TryTransition("east");
        }
        else if (_player.Position.X <= origin.X)
        {
            TryTransition("west");
        }
        else if (_player.Position.Y >= origin.Y + DungeonRoomGeometry.RoomHeight)
        {
            TryTransition("south");
        }
        else if (_player.Position.Y <= origin.Y)
        {
            TryTransition("north");
        }
    }

    private void TryCloseEntryShutter()
    {
        var room = _pendingEntryRoom;
        var door = _pendingEntryShutter;

        if (room is null || door is null)
        {
            return;
        }

        var origin = GetRoomOrigin(room);
        var playerPosition = _player.Position - origin;

        // Wait until the entire 12x12 player collider
        // is clear of the doorway's blocker.
        var hasClearedDoor = door.Direction.ToLowerInvariant() switch
        {
            "north" => playerPosition.Y > 38.0f,
            "south" => playerPosition.Y < DungeonRoomGeometry.RoomHeight - 38.0f,
            "west" => playerPosition.X > 38.0f,
            "east" => playerPosition.X < DungeonRoomGeometry.RoomWidth - 38.0f,
            _ => throw new InvalidOperationException(
                $"Invalid door direction: {door.Direction}"
            )
        };

        if (!hasClearedDoor)
        {
            return;
        }

        _state.ResetShutters(room.Id, [door]);

        _pendingEntryRoom = null;
        _pendingEntryShutter = null;
    }

    private void TryTransition(string direction)
    {
        foreach (var door in _currentRoom.Doors)
        {
            if (!string.Equals(
                    door.Direction,
                    direction,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!_state.IsDoorOpen(_currentRoom.Id, door))
            {
                return;
            }

            if (!_roomLookup.TryGetValue(
                    door.Destination,
                    out var destination))
            {
                GD.PushError(
                    $"Destination room not found: {door.Destination}"
                );

                return;
            }

            if (!DungeonRoomGeometry.IsAdjacent(
                _currentRoom.Position,
                destination.Position,
                direction
            ))
            {
                GD.PushError(
                    $"Room '{destination.Id}' is not adjacent " +
                    $"to '{_currentRoom.Id}' in direction {direction}."
                );

                return;
            }

            StartTransition(destination, direction);
            return;
        }
    }


    private void StartTransition(
        RoomData destination,
        string direction
    )
    {
        var departingRoom = _currentRoom;

        // Any previously pending entry belongs to the
        // room we're now leaving.
        _pendingEntryRoom = null;
        _pendingEntryShutter = null;

        // Find the doorway on the opposite side
        // of the destination room.
        var entryDirection = DungeonRoomGeometry.OppositeDirection(
            direction
        );

        var entryDoor = destination.Doors.Find(door =>
            string.Equals(
                door.Direction,
                entryDirection,
                StringComparison.OrdinalIgnoreCase
            )
        );

        // A closed incoming shutter needs to allow passage.
        DoorData? entryShutter = null;

        if (entryDoor is not null &&
            string.Equals(
                entryDoor.Type,
                "shutter",
                StringComparison.OrdinalIgnoreCase))
        {
            entryShutter = entryDoor;

            // Normally the shutter is already open when
            // returning through it. This is a safeguard.
            if (!_state.IsDoorOpen(destination.Id, entryDoor))
            {
                _state.TryOpenShutter(destination.Id, entryDoor);
            }
        }

        _isTransitioning = true;

        // Pause player-controlled movement during scrolling.
        _player.SetPhysicsProcess(false);
        _player.Velocity = Vector2.Zero;

        var entryPosition = DungeonRoomGeometry.GetEntryPosition(
            destination.Position,
            direction,
            _player.Position.X,
            _player.Position.Y
        );

        var playerTarget = new Vector2(
            entryPosition.X,
            entryPosition.Y
        );

        var cameraTarget = GetCameraPosition(destination);

        var tween = CreateTween();

        tween.SetParallel(true);
        tween.SetTrans(Tween.TransitionType.Linear);

        tween.TweenProperty(
            _camera,
            "position",
            cameraTarget,
            TransitionDuration
        );

        tween.TweenProperty(
            _player,
            "position",
            playerTarget,
            TransitionDuration
        );

        tween.Finished += () =>
        {
            // The player has finished leaving the old room.
            var previousRoom = _rooms.GetNode<Room>(
                departingRoom.Id
            );

            previousRoom.ResetTemporaryState();

            _currentRoom = destination;

            if (entryShutter is not null)
            {
                _pendingEntryRoom = destination;
                _pendingEntryShutter = entryShutter;
            }

            _player.Velocity = Vector2.Zero;
            _player.SetPhysicsProcess(true);

            _isTransitioning = false;

            GD.Print($"Entered room: {destination.Id}");
        };
    }
}
