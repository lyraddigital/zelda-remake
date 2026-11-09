
using Godot;
using System;
using ZeldaRemake.Doors;
using ZeldaRemake.Dungeon;
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
    private DungeonDefinition _definition = null!;

    private DungeonState _state = new();
    private DungeonSaveCoordinator? _saveCoordinator;

    private RoomData _currentRoom = null!;
    private bool _isTransitioning;
    private ShutterEntryController _shutterEntryController = null!;

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

    public override void _ExitTree()
    {
        _saveCoordinator?.Dispose();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!@event.IsActionPressed("save_game"))
        {
            return;
        }

        try
        {
            _saveCoordinator?.Save();
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

        _definition = DungeonDefinition.Parse(json);

        var dungeon = _definition.Data;
        var startingRoom = _definition.StartingRoom;

        _saveCoordinator?.Dispose();

        _state = new DungeonState();

        _saveCoordinator = new DungeonSaveCoordinator(
            dungeon.Id,
            _state,
            DungeonSaveService.Load,
            DungeonSaveService.Save,
            () => _isTransitioning,
            exception => GD.PushError(
                $"Auto-save failed: {exception}"
            )
        );

        // Restore the state before creating the dungeon rooms.
        _saveCoordinator.Restore();

        _shutterEntryController = new ShutterEntryController(
            _state
        );

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

        _shutterEntryController.TryCloseAfterEntry(
            _player.Position.X,
            _player.Position.Y
        );

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

            var destination = _definition.FindRoom(door.Destination);

            if (destination is null)
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
        var entryShutter = _shutterEntryController.PrepareEntry(
            destination,
            direction
        );

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

            _shutterEntryController.BeginEntry(
                destination,
                entryShutter
            );

            _player.Velocity = Vector2.Zero;
            _player.SetPhysicsProcess(true);

            _isTransitioning = false;

            GD.Print($"Entered room: {destination.Id}");
        };
    }
}
