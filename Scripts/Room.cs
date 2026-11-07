
using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using ZeldaRemake.Models;

public partial class Room : Node2D
{
    [Export]
    public PackedScene SideDoorScene { get; set; } = null!;

    [Export]
    public PackedScene VerticalDoorScene { get; set; } = null!;

    [Export]
    public PackedScene KeyScene { get; set; } = null!;

    [Export]
    public PackedScene PushableBlockScene { get; set; } = null!;

    [Export]
    public PackedScene StaircaseScene { get; set; } = null!;

    private const int TileSourceId = 0;
    private static readonly Vector2I GrassTile = new(0, 0);
    private static readonly Vector2I WallTile = new(1, 0);
    private static readonly Vector2I StoneTile = new(2, 0);

    private TileMapLayer _floors = null!;
    private TileMapLayer _walls = null!;
    private readonly List<PushableBlock> _resettableBlocks = [];

    public override void _Ready()
    {
        _floors = GetNode<TileMapLayer>("Floors");
        _walls = GetNode<TileMapLayer>("Walls");
    }

    public void LoadLayout(RoomData room, DungeonState state)
    {
        if (room.Walls.Length != 11)
        {
            throw new ArgumentException(
                $"Room '{room.Id}' must contain 11 rows."
            );
        }

        foreach (var row in room.Walls)
        {
            if (row.Length != 16)
            {
                throw new ArgumentException(
                    $"Room '{room.Id}' contains a row that is not 16 tiles wide."
                );
            }
        }

        _floors.Clear();
        _walls.Clear();

        var defaultFloor = string.Equals(
            room.DefaultFloor,
            "stone",
            StringComparison.OrdinalIgnoreCase)
            ? StoneTile
            : GrassTile;

        for (var y = 0; y < room.Walls.Length; y++)
        {
            for (var x = 0; x < room.Walls[y].Length; x++)
            {
                var position = new Vector2I(x, y);
                var tile = room.Walls[y][x];

                var floorTile = tile == 's'
                    ? StoneTile
                    : defaultFloor;

                _floors.SetCell(
                    position,
                    TileSourceId,
                    floorTile
                );

                if (tile == '#')
                {
                    var isDoorOpening = DoorwayGeometry.IsOpening(
                        room.Doors,
                        x,
                        y
                    );

                    if (!isDoorOpening)
                    {
                        _walls.SetCell(
                            position,
                            TileSourceId,
                            WallTile
                        );
                    }
                }
            }
        }

        LoadDoors(room, state);
        LoadObjects(room, state);
    }

    public void ResetTemporaryState()
    {
        foreach (var block in _resettableBlocks)
        {
            block.Reset();
        }
    }

    private void LoadDoors(RoomData room, DungeonState state)
    {
        var container = GetNode<Node2D>("Doors");

        foreach (var child in container.GetChildren())
        {
            container.RemoveChild(child);
            child.QueueFree();
        }

        foreach (var data in room.Doors)
        {
            PackedScene scene;
            Vector2 position;

            switch (data.Direction.ToLowerInvariant())
            {
                case "north":
                    scene = VerticalDoorScene;
                    position = new Vector2(128, 16);
                    break;

                case "south":
                    scene = VerticalDoorScene;
                    position = new Vector2(128, 160);
                    break;

                case "west":
                    scene = SideDoorScene;
                    position = new Vector2(16, 88);
                    break;

                case "east":
                    scene = SideDoorScene;
                    position = new Vector2(240, 88);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported door direction: {data.Direction}"
                    );
            }

            if (scene is null)
            {
                throw new InvalidOperationException(
                    $"Door scene is missing for {data.Direction}."
                );
            }

            var door = scene.Instantiate<Door>();

            container.AddChild(door);

            door.Position = position;
            door.Configure(data, state, room.Id);
        }
    }

    private void LoadObjects(RoomData room, DungeonState state)
    {
        var container = GetNode<Node2D>("Objects");

        _resettableBlocks.Clear();

        foreach (var child in container.GetChildren())
        {
            container.RemoveChild(child);
            child.QueueFree();
        }

        foreach (var data in room.Objects)
        {
            var position = new Vector2(
                data.X * 16 + 8,
                data.Y * 16 + 8
            );

            switch (data.Type.ToLowerInvariant())
            {
                case "key":
                    {
                        if (state.IsObjectCollected(room.Id, data.Id))
                        {
                            break;
                        }

                        if (KeyScene is null)
                        {
                            throw new InvalidOperationException(
                                "KeyScene has not been assigned."
                            );
                        }

                        var key = KeyScene.Instantiate<KeyPickup>();

                        key.Position = position;
                        container.AddChild(key);

                        key.Configure(state, room.Id, data.Id);

                        break;
                    }

                case "staircase":
                    {
                        if (StaircaseScene is null)
                        {
                            throw new InvalidOperationException(
                                "StaircaseScene has not been assigned."
                            );
                        }

                        var staircase = StaircaseScene.Instantiate<Staircase>();

                        staircase.Position = position;
                        container.AddChild(staircase);

                        staircase.Configure(state, room.Id, data.Id);

                        break;
                    }

                case "pushable_block":
                    {
                        if (PushableBlockScene is null)
                        {
                            throw new InvalidOperationException(
                                "PushableBlockScene has not been assigned."
                            );
                        }

                        var direction = GetOptionalString(
                            data,
                            "pushDirection"
                        ) ?? "any";

                        var opensDoor = GetOptionalString(
                            data,
                            "opensDoor"
                        );

                        var revealsStaircase = GetOptionalString(
                            data,
                            "revealsStaircase"
                        );

                        if (opensDoor is not null &&
                            revealsStaircase is not null)
                        {
                            throw new InvalidOperationException(
                                $"Block '{data.Id}' cannot open a door " +
                                "and reveal a staircase simultaneously."
                            );
                        }

                        var block = PushableBlockScene.Instantiate<PushableBlock>();

                        block.Position = position;
                        container.AddChild(block);

                        block.Configure(direction);

                        // Shutter-door puzzle.
                        if (opensDoor is not null)
                        {
                            var targetDoor = room.Doors.Find(door =>
                                string.Equals(
                                    door.Direction,
                                    opensDoor,
                                    StringComparison.OrdinalIgnoreCase
                                )
                            );

                            if (targetDoor is null ||
                                !string.Equals(
                                    targetDoor.Type,
                                    "shutter",
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                throw new InvalidOperationException(
                                    $"Block '{data.Id}' targets an invalid shutter door."
                                );
                            }

                            block.Pushed += () =>
                            {
                                state.TryOpenShutter(room.Id, targetDoor);
                            };
                        }

                        // Permanent staircase puzzle.
                        if (revealsStaircase is not null)
                        {
                            var staircaseData = room.Objects.Find(obj =>
                                string.Equals(
                                    obj.Id,
                                    revealsStaircase,
                                    StringComparison.OrdinalIgnoreCase
                                ) &&
                                string.Equals(
                                    obj.Type,
                                    "staircase",
                                    StringComparison.OrdinalIgnoreCase
                                )
                            );

                            if (staircaseData is null ||
                                staircaseData.X != data.X ||
                                staircaseData.Y != data.Y)
                            {
                                throw new InvalidOperationException(
                                    $"Block '{data.Id}' must reference a " +
                                    "staircase at its starting position."
                                );
                            }

                            if (string.Equals(
                                direction,
                                "any",
                                StringComparison.OrdinalIgnoreCase))
                            {
                                throw new InvalidOperationException(
                                    "Staircase blocks require a fixed push direction."
                                );
                            }

                            block.Pushed += () =>
                            {
                                state.TryCompletePuzzle(
                                    room.Id,
                                    revealsStaircase
                                );
                            };

                            if (state.IsPuzzleCompleted(
                                room.Id,
                                revealsStaircase))
                            {
                                block.RestoreCompletedPosition();
                            }
                        }
                        else
                        {
                            // Ordinary and shutter blocks reset on room exit.
                            _resettableBlocks.Add(block);
                        }

                        break;
                    }
            }
        }
    }

    private static string? GetOptionalString(
        RoomObjectData data,
        string propertyName)
    {
        if (!data.Properties.TryGetValue(
            propertyName,
            out var value))
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException(
                $"Object '{data.Id}' requires a string " +
                $"for '{propertyName}'."
            );
        }

        var result = value.GetString();

        if (string.IsNullOrWhiteSpace(result))
        {
            throw new InvalidOperationException(
                $"Object '{data.Id}' has an empty '{propertyName}'."
            );
        }

        return result;
    }
}