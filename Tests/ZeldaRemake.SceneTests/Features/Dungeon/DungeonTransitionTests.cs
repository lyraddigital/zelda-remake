
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
            DeleteTestFile(path);
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

            var returnDirection = OppositeDirection(
                direction
            );

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

            var startRoom = manager.GetNode<Room>(
                "Rooms/start"
            );

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
                root,
                player,
                camera,
                startOrigin,
                destinationOrigin,
                "east"
            );

            await CrossDoorAsync(
                root,
                player,
                camera,
                destinationOrigin,
                startOrigin,
                "west"
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
                    JsonSerializer.SerializeToElement(
                        "bow_cellar"
                    )
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
                    JsonSerializer.SerializeToElement(
                        "north"
                    ),

                ["revealsStaircase"] =
                    JsonSerializer.SerializeToElement(
                        "stairs_01"
                    )
            }
        });

        try
        {
            WriteDungeon(path, dungeon);

            var (runner, root) = RoomTestFixture.Load();
            var manager = CreateManager(root, path);

            var player = manager.GetNode<Player>("Player");
            var camera = manager.GetNode<Camera2D>("Camera2D");

            var room = manager.GetNode<Room>(
                "Rooms/start"
            );

            var objects = room.GetNode<Node2D>(
                "Objects"
            );

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

            // Move Link outside the staircase trigger first.
            player.GlobalPosition =
                staircase.GlobalPosition +
                new Vector2(32, 0);

            await runner.SimulateFrames(3);

            // Enter the staircase.
            player.GlobalPosition =
                staircase.GlobalPosition;

            // Wait until the new cellar player is active.
            await root.WaitUntilAsync(
                () =>
                    manager.GetNodeOrNull<CanvasLayer>(
                        "Cellar"
                    )?.GetNodeOrNull<Player>(
                        "Link"
                    )?.IsPhysicsProcessing() == true,
                "cellar transition to finish",
                5000
            );

            var cellar = manager.GetNode<CanvasLayer>(
                "Cellar"
            );

            var cellarLink = cellar.GetNode<Player>(
                "Link"
            );

            var entranceLadder = cellar.GetNode<Ladder>(
                "LeftLadderTrigger"
            );

            var treasure = cellar.GetNode<TreasurePickup>(
                "Treasure"
            );

            // Assert the cellar has the correct movement mode.
            AssertBool(
                cellarLink.MovementMode ==
                PlayerMovementMode.SideView
            ).IsTrue();

            AssertBool(
                cellarLink.ActiveLadder == entranceLadder
            ).IsTrue();

            // The original dungeon player must be hidden.
            AssertBool(player.Visible).IsFalse();

            // The dungeon camera must not move.
            AssertBool(
                camera.Position.IsEqualApprox(
                    expectedCameraPosition
                )
            ).IsTrue();

            // Verify Link spawned at the top of the
            // entrance ladder, centred horizontally.
            var entranceBounds = entranceLadder.Bounds;

            AssertBool(
                Mathf.IsEqualApprox(
                    cellarLink.GlobalPosition.X,
                    entranceBounds.GetCenter().X
                )
            ).IsTrue();

            AssertBool(
                Mathf.IsEqualApprox(
                    cellarLink.GlobalPosition.Y,
                    entranceBounds.Position.Y + 6.0f
                )
            ).IsTrue();

            // Count valid exits from the top of the
            // entrance ladder.
            var topExitCount = 0;

            entranceLadder.PlayerExited +=
                (_, direction) =>
                {
                    if (direction == LadderExitDirection.Top)
                    {
                        topExitCount++;
                    }
                };

            // Ensure the test project defines the actions.
            var createdDown =
                !InputMap.HasAction("move_down");

            var createdUp =
                !InputMap.HasAction("move_up");

            if (createdDown)
            {
                InputMap.AddAction("move_down");
            }

            if (createdUp)
            {
                InputMap.AddAction("move_up");
            }

            try
            {
                // Link must not immediately exit the cellar.
                await runner.SimulateFrames(3);

                AssertBool(
                    manager.GetNodeOrNull<CanvasLayer>(
                        "Cellar"
                    ) is not null
                ).IsTrue();

                AssertBool(topExitCount == 0).IsTrue();

                // Descend the left ladder.
                runner.SimulateActionPress("move_down");

                await runner.AwaitInputProcessed();

                var bottomY =
                    entranceBounds.End.Y - 6.0f;

                await root.WaitUntilAsync(
                    () =>
                        cellarLink.GlobalPosition.Y >=
                        bottomY - 1.0f,
                    "Link to reach the bottom of the entrance ladder",
                    5000
                );

                runner.SimulateActionRelease("move_down");

                await runner.AwaitInputProcessed();

                // Reaching the bottom must not exit.
                AssertBool(topExitCount == 0).IsTrue();

                AssertBool(
                    manager.GetNodeOrNull<CanvasLayer>(
                        "Cellar"
                    ) is not null
                ).IsTrue();

                // Simulate Link exploring the upper chamber
                // and touching the Bow.
                //
                // The pickup uses an Area2D trigger, so
                // allow physics frames to process BodyEntered.
                cellarLink.GlobalPosition =
                    treasure.GlobalPosition;

                await root.WaitUntilAsync(
                    () => !treasure.Visible,
                    "Bow to be collected",
                    5000
                );

                AssertBool(
                    treasure.Visible
                ).IsFalse();

                // Collection should automatically trigger
                // an update to the dungeon save.
                await root.WaitUntilAsync(
                    () =>
                    {
                        var saved =
                            DungeonSaveService.Load(dungeonId);

                        return saved is not null &&
                            saved.OwnedItems.Contains("bow");
                    },
                    "Bow to be saved",
                    5000
                );

                var savedGame =
                    DungeonSaveService.Load(dungeonId);

                AssertBool(
                    savedGame is not null &&
                    savedGame.OwnedItems.Contains("bow")
                ).IsTrue();

                // Return Link to the foot of the entrance
                // ladder after collecting the Bow.
                //
                // Do not call SpawnAtLadder here because
                // it deliberately resets the descent history.
                cellarLink.GlobalPosition = new Vector2(
                    entranceBounds.GetCenter().X,
                    bottomY
                );

                await root.WaitUntilAsync(
                    () =>
                        cellarLink.ActiveLadder ==
                        entranceLadder,
                    "Link to re-enter the entrance ladder",
                    5000
                );

                // Returning to the ladder from below
                // must not itself cause an exit.
                AssertBool(topExitCount == 0).IsTrue();

                var expectedReturnPosition =
                    staircase.GlobalPosition +
                    new Vector2(0, 20);

                // Climb back up the entrance ladder.
                runner.SimulateActionPress("move_up");

                await runner.AwaitInputProcessed();

                // The ladder emits a Top exit event.
                // DungeonManager then fades back to the room.
                await root.WaitUntilAsync(
                    () =>
                        manager.GetNodeOrNull<CanvasLayer>(
                            "Cellar"
                        ) is null &&
                        player.Visible &&
                        player.IsPhysicsProcessing(),
                    "return transition to finish",
                    7000
                );

                // Assert that the ladder triggered
                // exactly one scene exit.
                AssertBool(topExitCount == 1).IsTrue();

                // Link returns to the same dungeon staircase.
                AssertBool(
                    player.GlobalPosition.DistanceTo(
                        expectedReturnPosition
                    ) < 0.1f
                ).IsTrue();

                // Camera position is preserved.
                AssertBool(
                    camera.Position.IsEqualApprox(
                        expectedCameraPosition
                    )
                ).IsTrue();

                // Temporary room puzzles reset after
                // leaving the cellar.
                AssertBool(staircase.Visible).IsFalse();

                AssertBool(
                    block.Position.IsEqualApprox(
                        initialBlockPosition
                    )
                ).IsTrue();
            }
            finally
            {
                runner.SimulateActionRelease("move_down");
                runner.SimulateActionRelease("move_up");

                await runner.AwaitInputProcessed();

                if (createdDown)
                {
                    InputMap.EraseAction("move_down");
                }

                if (createdUp)
                {
                    InputMap.EraseAction("move_up");
                }
            }
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
                player.Position.DistanceTo(
                    expectedPosition
                ) < 0.1f &&
                camera.Position.DistanceTo(
                    expectedCamera
                ) < 0.1f,
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

    private static string OppositeDirection(
        string direction
    )
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
            "res://Scenes/ItemCellar.tscn"
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
