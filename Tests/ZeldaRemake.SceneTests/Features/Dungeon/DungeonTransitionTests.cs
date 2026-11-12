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

    [TestCase("north", 0, -1)]
    [TestCase("south", 0, 1)]
    [TestCase("east", 1, 0)]
    [TestCase("west", -1, 0)]
    public async Task RoomTransition_WhenReturning_ReentersOriginalRoom(
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

        var startOrigin = new Vector2(0, 64);
        var destinationOrigin = new Vector2(
            offsetX * 256,
            offsetY * 176 + 64
        );

        try
        {
            WriteDungeon(path, dungeon);

            var (runner, root) = RoomTestFixture.Load();
            var manager = CreateManager(root, path);

            var player = manager.GetNode<Player>("Player");
            var camera = manager.GetNode<Camera2D>("Camera2D");

            await runner.SimulateFrames(2);

            // Travel to the neighbouring room.
            await CrossDoorAsync(
                root,
                player,
                camera,
                startOrigin,
                destinationOrigin,
                direction
            );

            var returnDirection = OppositeDirection(direction);

            // Act - return to the starting room.
            await CrossDoorAsync(
                root,
                player,
                camera,
                destinationOrigin,
                startOrigin,
                returnDirection
            );

            // Assert
            AssertBool(
                player.Position.DistanceTo(
                    ExpectedEntryPosition(
                        startOrigin,
                        returnDirection
                    )
                ) < 0.1f
            ).IsTrue();

            AssertBool(
                camera.Position.DistanceTo(
                    startOrigin + new Vector2(128, 56)
                ) < 0.1f
            ).IsTrue();
        }
        finally
        {
            DeleteTestFile(path);
        }
    }

    [TestCase]
    public async Task RoomTransition_WhenReturningThroughUnlockedDoor_RemainsOpen()
    {
        // Arrange
        var dungeonId = $"test_{Guid.NewGuid():N}";
        var path = $"user://{dungeonId}.json";
        var savePath = $"user://Saves/{dungeonId}.json";

        var dungeon = CreateDungeon(
            dungeonId,
            "east",
            1,
            0
        );

        dungeon.Rooms[0].Doors[0].Type = "locked";

        try
        {
            WriteDungeon(path, dungeon);

            DungeonSaveService.Save(new DungeonSaveData
            {
                DungeonId = dungeonId,
                UnlockedDoors = ["start:east"]
            });

            var (runner, root) = RoomTestFixture.Load();
            var manager = CreateManager(root, path);

            var player = manager.GetNode<Player>("Player");
            var camera = manager.GetNode<Camera2D>("Camera2D");

            var startRoom = manager.GetNode<Room>("Rooms/start");

            var door = startRoom.GetNode<Node2D>("Doors")
                .GetChild<Door>(0);

            var blocker = door.GetNode<CollisionShape2D>(
                "Blocker/CollisionShape2D"
            );

            await runner.SimulateFrames(2);

            AssertBool(blocker.Disabled).IsTrue();

            var startOrigin = new Vector2(0, 64);
            var destinationOrigin = new Vector2(256, 64);

            // Travel east, then return west.
            await CrossDoorAsync(
                root, player, camera,
                startOrigin, destinationOrigin, "east"
            );

            await CrossDoorAsync(
                root, player, camera,
                destinationOrigin, startOrigin, "west"
            );

            // Assert
            AssertBool(blocker.Disabled).IsTrue();

            var saved = DungeonSaveService.Load(dungeonId);

            AssertBool(
                saved is not null &&
                saved.UnlockedDoors.Contains("start:east")
            ).IsTrue();
        }
        finally
        {
            DeleteTestFile(path);
            DeleteTestFile(savePath);
        }
    }

    [TestCase]
    public async Task RoomTransition_WhenEnteringThroughShutter_ClosesAfterClearingDoor()
    {
        // Arrange
        var dungeonId = $"test_{Guid.NewGuid():N}";
        var path = $"user://{dungeonId}.json";

        var dungeon = CreateDungeon(
            dungeonId,
            "east",
            1,
            0
        );

        // The starting room has an ordinary east door.
        // The destination has an incoming west shutter.
        dungeon.Rooms[1].Doors[0].Type = "shutter";

        try
        {
            WriteDungeon(path, dungeon);

            var (runner, root) = RoomTestFixture.Load();
            var manager = CreateManager(root, path);

            var player = manager.GetNode<Player>("Player");
            var camera = manager.GetNode<Camera2D>("Camera2D");

            var destinationRoom = manager.GetNode<Room>(
                "Rooms/destination"
            );

            var shutter = destinationRoom
                .GetNode<Node2D>("Doors")
                .GetChild<Door>(0);

            var blocker = shutter.GetNode<CollisionShape2D>(
                "Blocker/CollisionShape2D"
            );

            await runner.SimulateFrames(2);

            AssertBool(blocker.Disabled).IsFalse();

            var startOrigin = new Vector2(0, 64);
            var destinationOrigin = new Vector2(256, 64);

            // Act - enter through the incoming shutter.
            await CrossDoorAsync(
                root,
                player,
                camera,
                startOrigin,
                destinationOrigin,
                "east"
            );

            // Link has arrived but has not fully cleared
            // the incoming doorway yet.
            AssertBool(blocker.Disabled).IsTrue();

            // Move Link past the shutter's closing threshold.
            player.Position = destinationOrigin +
                new Vector2(40, 88);

            await root.WaitUntilAsync(
                () => !blocker.Disabled,
                "incoming shutter to close"
            );

            // Assert
            AssertBool(blocker.Disabled).IsFalse();
        }
        finally
        {
            DeleteTestFile(path);
        }
    }

    [TestCase]
    public async Task StaircaseTransition_WhenEnteringAndExiting_ReturnsToDungeon()
    {
        // Arrange
        var dungeonId = $"test_{Guid.NewGuid():N}";
        var path = $"user://{dungeonId}.json";
        var savePath = $"user://Saves/{dungeonId}.json";

        var dungeon = CreateDungeon(
            dungeonId,
            "east",
            1,
            0
        );

        dungeon.Cellars.Add(new CellarData
        {
            Id = "bow_cellar",
            Type = "item",
            ItemId = "bow"
        });

        var startingRoom = dungeon.Rooms[0];

        startingRoom.Objects.Add(new RoomObjectData
        {
            Id = "stairs_01",
            Type = "staircase",
            X = 10,
            Y = 5,
            Properties = new()
            {
                ["destinationRoom"] =
                    JsonSerializer.SerializeToElement("bow_cellar")
            }
        });

        startingRoom.Objects.Add(new RoomObjectData
        {
            Id = "block_01",
            Type = "pushable_block",
            X = 6,
            Y = 5,
            Properties = new()
            {
                ["pushDirection"] =
                    JsonSerializer.SerializeToElement("north"),

                ["revealsStaircase"] =
                    JsonSerializer.SerializeToElement("stairs_01")
            }
        });

        try
        {
            WriteDungeon(path, dungeon);

            var (runner, root) = RoomTestFixture.Load();
            var manager = CreateManager(root, path);

            var player = manager.GetNode<Player>("Player");
            var camera = manager.GetNode<Camera2D>("Camera2D");

            var room = manager.GetNode<Room>("Rooms/start");

            var objects = room.GetNode<Node2D>("Objects");
            var staircase = objects.GetChild<Staircase>(0);
            var block = objects.GetChild<PushableBlock>(1);

            var initialBlockPosition = block.Position;
            var expectedCameraPosition = camera.Position;

            player.SetPhysicsProcess(false);

            await runner.SimulateFrames(3);

            AssertBool(staircase.Visible).IsFalse();

            // Reveal the staircase using its existing puzzle.
            block.PushDurationSeconds = 0.05f;

            using var testPlayer = new Player();

            var pushed = block.TryPush(
                Vector2.Up,
                testPlayer
            );

            AssertBool(pushed).IsTrue();

            await root.WaitUntilAsync(
                () => staircase.Visible,
                "staircase to be revealed"
            );

            // Move Link into the staircase detection area.
            player.GlobalPosition =
                staircase.GlobalPosition + new Vector2(32, 0);

            await runner.SimulateFrames(3);

            // Act - enter the staircase.
            player.GlobalPosition = staircase.GlobalPosition;

            await root.WaitUntilAsync(
                () => manager.GetNodeOrNull<Cellar>("Cellar")?.IsActive == true,
                "cellar transition to finish",
                5000
            );

            var cellar = manager.GetNode<Cellar>("Cellar");

            // Assert - cellar opened and dungeon Link is hidden.
            AssertBool(cellar.CellarId == "bow_cellar").IsTrue();
            AssertBool(player.Visible).IsFalse();

            AssertBool(
                camera.Position.IsEqualApprox(expectedCameraPosition)
            ).IsTrue();

            // Collect the Bow before returning upstairs.
            var treasure = cellar.GetNode<ColorRect>("Treasure");
            var cellarLink = cellar.GetNode<ColorRect>("Link");

            cellarLink.Position = new Vector2(218, 112);

            AssertBool(cellar.TryCollectTreasure()).IsTrue();
            AssertBool(treasure.Visible).IsFalse();

            // Collection should automatically trigger a save.
            var savedGame = DungeonSaveService.Load(dungeonId);

            AssertBool(
                savedGame is not null &&
                savedGame.OwnedItems.Contains("bow")
            ).IsTrue();

            // Return to the original dungeon room.
            var expectedReturnPosition =
                staircase.GlobalPosition + new Vector2(0, 20);

            cellarLink.Position = new Vector2(26, 52);
            AssertBool(cellar.TryExit()).IsTrue();

            await root.WaitUntilAsync(
                () =>
                    manager.GetNodeOrNull<Cellar>("Cellar") is null &&
                    player.Visible &&
                    player.IsPhysicsProcessing(),
                "return transition to finish",
                5000
            );

            // Link returns to the same room and staircase.
            AssertBool(
                player.GlobalPosition.DistanceTo(expectedReturnPosition) < 0.1f
            ).IsTrue();

            AssertBool(
                camera.Position.IsEqualApprox(expectedCameraPosition)
            ).IsTrue();

            // The temporary puzzle resets when Link leaves the cellar.
            AssertBool(staircase.Visible).IsFalse();

            AssertBool(
                block.Position.IsEqualApprox(initialBlockPosition)
            ).IsTrue();
        }
        finally
        {
            DeleteTestFile(path);
            DeleteTestFile(savePath);
        }
    }

    private static async Task CrossDoorAsync(
        Room root,
        Player player,
        Camera2D camera,
        Vector2 fromOrigin,
        Vector2 destinationOrigin,
        string direction
    )
    {
        player.SetPhysicsProcess(false);
        player.Velocity = Vector2.Zero;

        // Simulate Link crossing the current room boundary.
        player.Position = direction switch
        {
            "north" => fromOrigin + new Vector2(128, -1),
            "south" => fromOrigin + new Vector2(128, 177),
            "east" => fromOrigin + new Vector2(257, 88),
            "west" => fromOrigin + new Vector2(-1, 88),
            _ => throw new ArgumentException(
                $"Invalid direction: {direction}"
            )
        };

        var expectedPosition = ExpectedEntryPosition(
            destinationOrigin,
            direction
        );

        var expectedCamera = destinationOrigin +
            new Vector2(128, 56);

        // Player physics resumes only after the transition
        // tween's Finished callback has completed.
        await root.WaitUntilAsync(
            () =>
                player.IsPhysicsProcessing() &&
                player.Position.DistanceTo(expectedPosition) < 0.1f &&
                camera.Position.DistanceTo(expectedCamera) < 0.1f,
            $"room transition {direction} to finish",
            5000
        );

        player.SetPhysicsProcess(false);
    }

    private static Vector2 ExpectedEntryPosition(
        Vector2 roomOrigin,
        string direction
    )
    {
        return direction switch
        {
            "north" => roomOrigin + new Vector2(128, 152),
            "south" => roomOrigin + new Vector2(128, 24),
            "east" => roomOrigin + new Vector2(24, 88),
            "west" => roomOrigin + new Vector2(232, 88),
            _ => throw new ArgumentException(
                $"Invalid direction: {direction}"
            )
        };
    }

    private static string OppositeDirection(string direction)
    {
        return direction switch
        {
            "north" => "south",
            "south" => "north",
            "east" => "west",
            "west" => "east",
            _ => throw new ArgumentException(
                $"Invalid direction: {direction}"
            )
        };
    }

    private static void DeleteTestFile(string path)
    {
        if (!Godot.FileAccess.FileExists(path))
        {
            return;
        }

        DirAccess.RemoveAbsolute(
            ProjectSettings.GlobalizePath(path)
        );
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

        var cellarScene = ResourceLoader.Load<PackedScene>(
            "res://Scenes/Cellar.tscn"
        );

        var manager = new DungeonManager
        {
            Name = "DungeonManager",
            DungeonPath = dungeonPath,
            RoomScene = roomScene,
            CellarScene = cellarScene
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