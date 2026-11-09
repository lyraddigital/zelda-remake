
using GdUnit4;
using Godot;
using System.Text.Json;
using ZeldaRemake.Models;
using ZeldaRemake.SceneTests.Features.Rooms.Fixtures;
using ZeldaRemake.SceneTests.Helpers;
using static GdUnit4.Assertions;

namespace ZeldaRemake.SceneTests.Features.Dungeon;

[TestSuite]
[RequireGodotRuntime]
public class DungeonTransitionTests
{
    [TestCase("north", 0, -1)]
    [TestCase("south", 0, 1)]
    [TestCase("east", 1, 0)]
    [TestCase("west", -1, 0)]
    public async Task RoomTransition_WhenDoorIsOpen_MovesBetweenRooms(
        string direction,
        int offsetX,
        int offsetY
    )
    {
        // Arrange
        var dungeonId = $"test_{Guid.NewGuid():N}";
        var path = $"user://{dungeonId}.json";

        var dungeon = CreateDungeon(
            dungeonId,
            direction,
            offsetX,
            offsetY
        );

        try
        {
            WriteDungeon(path, dungeon);

            var (runner, root) = RoomTestFixture.Load();
            var manager = CreateManager(root, path);

            var player = manager.GetNode<Player>("Player");
            var camera = manager.GetNode<Camera2D>("Camera2D");
            var rooms = manager.GetNode<Node2D>("Rooms");

            await runner.SimulateFrames(2);

            AssertBool(rooms.GetChildCount() == 2).IsTrue();

            // Keep player input from affecting the test.
            player.SetPhysicsProcess(false);

            // Position Link just across the starting room
            // boundary to initiate the transition.
            player.Position = direction switch
            {
                "north" => new Vector2(128, 63),
                "south" => new Vector2(128, 241),
                "east" => new Vector2(257, 152),
                "west" => new Vector2(-1, 152),
                _ => throw new ArgumentException(
                    $"Invalid direction: {direction}"
                )
            };

            var destinationOrigin = new Vector2(
                offsetX * 256,
                offsetY * 176 + 64
            );

            var expectedPosition = direction switch
            {
                "north" => destinationOrigin +
                    new Vector2(128, 152),

                "south" => destinationOrigin +
                    new Vector2(128, 24),

                "east" => destinationOrigin +
                    new Vector2(24, 88),

                "west" => destinationOrigin +
                    new Vector2(232, 88),

                _ => throw new ArgumentException(
                    $"Invalid direction: {direction}"
                )
            };

            var expectedCamera = destinationOrigin +
                new Vector2(128, 56);

            // Act
            await root.WaitUntilAsync(
                () =>
                    player.Position.DistanceTo(
                        expectedPosition
                    ) < 0.1f &&
                    camera.Position.DistanceTo(
                        expectedCamera
                    ) < 0.1f,
                "player and camera to reach destination"
            );

            await runner.SimulateFrames(2);

            // Assert
            AssertBool(
                player.Position.DistanceTo(
                    expectedPosition
                ) < 0.1f
            ).IsTrue();

            AssertBool(
                camera.Position.DistanceTo(
                    expectedCamera
                ) < 0.1f
            ).IsTrue();
        }
        finally
        {
            DirAccess.RemoveAbsolute(
                ProjectSettings.GlobalizePath(path)
            );
        }
    }

    private static DungeonData CreateDungeon(
        string dungeonId,
        string direction,
        int offsetX,
        int offsetY
    )
    {
        var opposite = direction switch
        {
            "north" => "south",
            "south" => "north",
            "east" => "west",
            "west" => "east",
            _ => throw new ArgumentException(
                $"Invalid direction: {direction}"
            )
        };

        return new DungeonData
        {
            Id = dungeonId,
            Name = "Transition Test",
            StartingRoom = "start",
            Rooms =
            [
                new RoomData
                {
                    Id = "start",
                    Position = new GridPosition
                    {
                        X = 0,
                        Y = 0
                    },
                    Doors =
                    [
                        new DoorData
                        {
                            Direction = direction,
                            Destination = "destination",
                            Type = "open"
                        }
                    ]
                },
                new RoomData
                {
                    Id = "destination",
                    Position = new GridPosition
                    {
                        X = offsetX,
                        Y = offsetY
                    },
                    Doors =
                    [
                        new DoorData
                        {
                            Direction = opposite,
                            Destination = "start",
                            Type = "open"
                        }
                    ]
                }
            ]
        };
    }

    private static void WriteDungeon(
        string path,
        DungeonData dungeon
    )
    {
        var json = JsonSerializer.Serialize(
            dungeon,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy =
                    JsonNamingPolicy.CamelCase
            }
        );

        using var file = Godot.FileAccess.Open(
            path,
            Godot.FileAccess.ModeFlags.Write
        ) ?? throw new InvalidOperationException(
            $"Unable to create test dungeon: {path}"
        );

        file.StoreString(json);
    }

    private static DungeonManager CreateManager(
        Room root,
        string dungeonPath
    )
    {
        var roomScene = ResourceLoader.Load<PackedScene>(
            "res://Scenes/Room.tscn"
        );

        var playerScene = ResourceLoader.Load<PackedScene>(
            "res://Scenes/Player.tscn"
        );

        var manager = new DungeonManager
        {
            Name = "DungeonManager",
            DungeonPath = dungeonPath,
            RoomScene = roomScene
        };

        manager.AddChild(new Node2D
        {
            Name = "Rooms"
        });

        var player = playerScene.Instantiate<Player>();
        player.Name = "Player";

        manager.AddChild(player);

        manager.AddChild(new Camera2D
        {
            Name = "Camera2D"
        });

        root.AddChild(manager);

        return manager;
    }
}
