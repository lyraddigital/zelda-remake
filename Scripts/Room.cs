
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
    private const int RoomWidthInTiles = 16;
    private const int RoomHeightInTiles = 11;
    private const int PerimeterThickness = 2;

    private const int TileSourceId = 0;
    private static readonly Vector2I GrassTile = new(0, 0);
    private static readonly Vector2I WallTile = new(1, 0);
    private static readonly Vector2I StoneTile = new(2, 0);

    private TileMapLayer _floors = null!;
    private TileMapLayer _walls = null!;
    private readonly List<PushableBlock> _resettableBlocks = [];
    private readonly List<Staircase> _temporaryStaircases = [];

    public override void _Ready()
    {
        _floors = GetNode<TileMapLayer>("Floors");
        _walls = GetNode<TileMapLayer>("Walls");
    }

    public void LoadLayout(RoomData room, DungeonState state)
    {
        ValidateLayout(room);

        _floors.Clear();
        _walls.Clear();

        var defaultFloor = GetFloorTile(room.DefaultFloor);

        // Generate the standard dungeon room.
        for (var y = 0; y < RoomHeightInTiles; y++)
        {
            for (var x = 0; x < RoomWidthInTiles; x++)
            {
                var position = new Vector2I(x, y);

                _floors.SetCell(
                    position,
                    TileSourceId,
                    defaultFloor
                );

                if (!IsPerimeter(x, y))
                {
                    continue;
                }

                if (DoorwayGeometry.IsOpening(room.Doors, x, y))
                {
                    continue;
                }

                _walls.SetCell(
                    position,
                    TileSourceId,
                    WallTile
                );
            }
        }

        // Add the room-specific internal walls.
        foreach (var wall in room.InteriorWalls)
        {
            for (var y = wall.Y; y < wall.Y + wall.Height; y++)
            {
                for (var x = wall.X; x < wall.X + wall.Width; x++)
                {
                    _walls.SetCell(
                        new Vector2I(x, y),
                        TileSourceId,
                        WallTile
                    );
                }
            }
        }

        // Apply any custom floor tiles.
        foreach (var floor in room.FloorOverrides)
        {
            _floors.SetCell(
                new Vector2I(floor.X, floor.Y),
                TileSourceId,
                GetFloorTile(floor.Floor)
            );
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

        foreach (var staircase in _temporaryStaircases)
        {
            staircase.Conceal();
        }
    }

    private static void ValidateLayout(RoomData room)
    {
        // Validate the default floor before rendering.
        _ = GetFloorTile(room.DefaultFloor);

        foreach (var wall in room.InteriorWalls)
        {
            var maximumX = RoomWidthInTiles - PerimeterThickness;
            var maximumY = RoomHeightInTiles - PerimeterThickness;

            if (wall.Width <= 0 ||
                wall.Height <= 0 ||
                wall.X < PerimeterThickness ||
                wall.Y < PerimeterThickness ||
                wall.X >= maximumX ||
                wall.Y >= maximumY ||
                wall.Width > maximumX - wall.X ||
                wall.Height > maximumY - wall.Y)
            {
                throw new ArgumentException(
                    $"Room '{room.Id}' contains an invalid " +
                    $"interior wall at ({wall.X}, {wall.Y}) " +
                    $"with size {wall.Width}x{wall.Height}."
                );
            }
        }

        foreach (var floor in room.FloorOverrides)
        {
            if (floor.X < 0 ||
                floor.X >= RoomWidthInTiles ||
                floor.Y < 0 ||
                floor.Y >= RoomHeightInTiles)
            {
                throw new ArgumentException(
                    $"Room '{room.Id}' contains an invalid " +
                    $"floor position ({floor.X}, {floor.Y})."
                );
            }

            _ = GetFloorTile(floor.Floor);
        }
    }

    private static bool IsPerimeter(int x, int y)
    {
        return x < PerimeterThickness ||
               x >= RoomWidthInTiles - PerimeterThickness ||
               y < PerimeterThickness ||
               y >= RoomHeightInTiles - PerimeterThickness;
    }

    private static Vector2I GetFloorTile(string floor)
    {
        return floor?.ToLowerInvariant() switch
        {
            "grass" => GrassTile,
            "stone" => StoneTile,

            _ => throw new ArgumentException(
                $"Unsupported floor type: '{floor}'."
            )
        };
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
        _temporaryStaircases.Clear();

        var staircases = new Dictionary<string, Staircase>(
            StringComparer.OrdinalIgnoreCase
        );

        var staircaseBindings = new List<(
            PushableBlock Block,
            string BlockId,
            string StaircaseId
        )>();

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

                        staircases.Add(data.Id, staircase);

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

                        // Hidden staircase puzzle: visibility is room-local.
                        if (revealsStaircase is not null)
                        {
                            if (string.Equals(
                                direction,
                                "any",
                                StringComparison.OrdinalIgnoreCase
                            ))
                            {
                                throw new InvalidOperationException(
                                    "Staircase blocks require a fixed push direction."
                                );
                            }

                            staircaseBindings.Add((
                                block,
                                data.Id,
                                revealsStaircase
                            ));
                        }

                        // Every block returns to its starting position.
                        _resettableBlocks.Add(block);

                        break;
                    }
            }
        }

        // Bind after every object exists, regardless of JSON order.
        foreach (var (block, blockId, staircaseId) in staircaseBindings)
        {
            if (!staircases.TryGetValue(staircaseId, out var staircase))
            {
                throw new InvalidOperationException(
                    $"Block '{blockId}' must reference a staircase."
                );
            }

            staircase.Conceal();

            if (!_temporaryStaircases.Contains(staircase))
            {
                _temporaryStaircases.Add(staircase);
            }

            block.Pushed += staircase.Reveal;
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