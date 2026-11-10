
using GdUnit4;
using Godot;
using ZeldaRemake.Models;
using ZeldaRemake.SceneTests.Features.Rooms.Fixtures;
using ZeldaRemake.SceneTests.Helpers;
using static GdUnit4.Assertions;

namespace ZeldaRemake.SceneTests.Features.Doors;

[TestSuite]
[RequireGodotRuntime]
public class DoorSceneTests
{
    [TestCase("north", 128, 16)]
    [TestCase("south", 128, 160)]
    [TestCase("east", 240, 88)]
    [TestCase("west", 16, 88)]
    public void LoadLayout_WhenDoorHasDirection_UsesCorrectGeometry(
        string direction,
        int expectedX,
        int expectedY
    )
    {
        // Arrange
        var (_, _, door, _, _) = LoadDoor(direction, "open");

        // Act
        var blocker = door.GetNode<CollisionShape2D>(
            "Blocker/CollisionShape2D"
        );

        var shape = (RectangleShape2D)blocker.Shape;

        var expectedHeight = direction is "north" or "south"
            ? 32.0f
            : 16.0f;

        // Assert
        AssertBool(
            door.Position.IsEqualApprox(
                new Vector2(expectedX, expectedY)
            )
        ).IsTrue();

        AssertBool(
            Mathf.IsEqualApprox(shape.Size.Y, expectedHeight)
        ).IsTrue();
    }

    [TestCase("north", "open", true)]
    [TestCase("south", "open", true)]
    [TestCase("east", "open", true)]
    [TestCase("west", "open", true)]
    [TestCase("north", "locked", false)]
    [TestCase("south", "locked", false)]
    [TestCase("east", "locked", false)]
    [TestCase("west", "locked", false)]
    [TestCase("north", "shutter", false)]
    [TestCase("south", "shutter", false)]
    [TestCase("east", "shutter", false)]
    [TestCase("west", "shutter", false)]
    public async Task LoadLayout_WhenDoorConfigured_SetsCorrectCollision(
        string direction,
        string type,
        bool expectedOpen
    )
    {
        // Arrange
        var (runner, _, door, data, state) = LoadDoor(
            direction,
            type
        );

        // Act
        await runner.SimulateFrames(2);

        var blocker = door.GetNode<CollisionShape2D>(
            "Blocker/CollisionShape2D"
        );

        // Assert
        AssertBool(
            state.IsDoorOpen("test_room", data) == expectedOpen
        ).IsTrue();

        AssertBool(
            blocker.Disabled == expectedOpen
        ).IsTrue();
    }

    [TestCase]
    public async Task UnlockArea_WhenPlayerHasKey_UnlocksDoor()
    {
        // Arrange
        var state = new DungeonState();
        state.TryCollectKey("test_room", "key_01");

        var (runner, room, door, data, _) = LoadDoor(
            "north",
            "locked",
            state
        );

        // Place the player inside the unlock detection area.
        var player = CreatePlayer(
            room,
            new Vector2(128, 44)
        );

        player.SetPhysicsProcess(false);

        // Act
        await runner.SimulateFrames(4);

        var blocker = door.GetNode<CollisionShape2D>(
            "Blocker/CollisionShape2D"
        );

        // Assert
        AssertBool(
            state.IsDoorOpen("test_room", data)
        ).IsTrue();

        AssertBool(state.Keys == 0).IsTrue();
        AssertBool(blocker.Disabled).IsTrue();
    }

    [TestCase]
    public async Task ShutterDoor_WhenReset_ClosesCollision()
    {
        // Arrange
        var (runner, _, door, data, state) = LoadDoor(
            "south",
            "shutter"
        );

        var blocker = door.GetNode<CollisionShape2D>(
            "Blocker/CollisionShape2D"
        );

        // Act - open the shutter.
        state.TryOpenShutter("test_room", data);

        await runner.SimulateFrames(2);

        AssertBool(blocker.Disabled).IsTrue();

        // Act - simulate the shutter reset.
        state.ResetShutters("test_room", [data]);

        await runner.SimulateFrames(2);

        // Assert
        AssertBool(
            state.IsDoorOpen("test_room", data)
        ).IsFalse();

        AssertBool(blocker.Disabled).IsFalse();
    }

    [TestCase("locked")]
    [TestCase("shutter")]
    public void TryGetAlignmentVelocity_WhenDoorClosed_ReturnsFalse(
        string type
    )
    {
        // Arrange
        var (_, _, door, _, _) = LoadDoor("east", type);

        // Act
        var result = door.TryGetAlignmentVelocity(
            new Vector2(214, 92),
            Vector2.Right,
            out var velocity
        );

        // Assert
        AssertBool(result).IsFalse();

        AssertBool(
            velocity.IsEqualApprox(Vector2.Zero)
        ).IsTrue();
    }


    [TestCase(216, true)]
    [TestCase(215, false)]
    public void TryGetAlignmentVelocity_WhenApproachingEastDoor_Uses24PixelRange(
        int playerX,
        bool expectedAlignment
    )
    {
        // Arrange
        var (_, _, door, _, _) = LoadDoor("east", "open");

        // Act
        var result = door.TryGetAlignmentVelocity(
            new Vector2(playerX, 92),
            Vector2.Right,
            out _
        );

        // Assert
        AssertBool(result == expectedAlignment).IsTrue();
    }

    [TestCase("north", 132, 39, "move_up")]
    [TestCase("south", 132, 137, "move_down")]
    [TestCase("east", 217, 92, "move_right")]
    [TestCase("west", 39, 92, "move_left")]
    public async Task PlayerMovement_WhenApproachingOpenDoor_AlignsPlayer(
        string direction,
        int startX,
        int startY,
        string action
    )
    {
        // Arrange
        var (runner, room, door, _, _) = LoadDoor(
            direction,
            "open"
        );

        var player = CreatePlayer(
            room,
            new Vector2(startX, startY)
        );

        var createdActions = EnsureMovementActions();

        var unlockArea = door.GetNode<Area2D>("UnlockArea");

        var isVerticalDoor = direction is "north" or "south";

        float GetAlignmentError()
        {
            return isVerticalDoor
                ? Mathf.Abs(
                    player.GlobalPosition.X - door.GlobalPosition.X
                )
                : Mathf.Abs(
                    player.GlobalPosition.Y - door.GlobalPosition.Y
                );
        }

        try
        {
            // Wait for Godot to detect the player in the doorway area.
            await room.WaitUntilAsync(
                () => unlockArea.GetOverlappingBodies().Contains(player),
                "player to enter the doorway detection area"
            );

            await runner.SimulateFrames(1);

            var initialError = GetAlignmentError();

            var movementDirection = direction switch
            {
                "north" => Vector2.Up,
                "south" => Vector2.Down,
                "east" => Vector2.Right,
                "west" => Vector2.Left,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(direction)
                )
            };

            // Confirm the player is eligible for alignment.
            AssertBool(
                door.TryGetAlignmentVelocity(
                    player.GlobalPosition,
                    movementDirection,
                    out var alignmentVelocity
                )
            ).IsTrue();

            AssertBool(
                alignmentVelocity.Length() > 0.0f
            ).IsTrue();

            // Act
            runner.SimulateActionPress(action);

            await runner.AwaitInputProcessed();

            // Wait for an actual correction instead of assuming
            // it occurs within a fixed number of frames.
            await room.WaitUntilAsync(
                () => GetAlignmentError() <= initialError - 1.0f,
                "player to align with the doorway"
            );

            // Assert
            AssertBool(
                GetAlignmentError() < initialError
            ).IsTrue();
        }
        finally
        {
            runner.SimulateActionRelease(action);

            await runner.AwaitInputProcessed();

            foreach (var createdAction in createdActions)
            {
                InputMap.EraseAction(createdAction);
            }
        }
    }

    [TestCase("move_up", "move_right", 1, -1)]
    [TestCase("move_up", "move_left", -1, -1)]
    [TestCase("move_down", "move_right", 1, 1)]
    [TestCase("move_down", "move_left", -1, 1)]
    public async Task PlayerMovement_WhenMovingDiagonally_MaintainsNormalSpeed(
        string firstAction,
        string secondAction,
        int expectedX,
        int expectedY
    )
    {
        // Arrange
        var (runner, room) = RoomTestFixture.Load();

        room.LoadLayout(
            RoomDataFactory.Create(),
            new DungeonState()
        );

        var player = CreatePlayer(
            room,
            new Vector2(128, 88)
        );

        var createdActions = EnsureMovementActions();

        await runner.SimulateFrames(2);

        var initialPosition = player.Position;

        try
        {
            // Act
            runner.SimulateActionPress(firstAction);
            runner.SimulateActionPress(secondAction);

            await runner.AwaitInputProcessed();
            await runner.SimulateFrames(8);

            var displacement = player.Position - initialPosition;

            // Assert
            AssertBool(
                displacement.X * expectedX > 0
            ).IsTrue();

            AssertBool(
                displacement.Y * expectedY > 0
            ).IsTrue();

            AssertBool(
                Mathf.Abs(player.Velocity.Length() - player.Speed) < 0.5f
            ).IsTrue();
        }
        finally
        {
            runner.SimulateActionRelease(firstAction);
            runner.SimulateActionRelease(secondAction);

            await runner.AwaitInputProcessed();

            foreach (var action in createdActions)
            {
                InputMap.EraseAction(action);
            }
        }
    }

    [TestCase("open", false)]
    [TestCase("locked", true)]
    [TestCase("shutter", true)]
    public async Task PlayerMovement_WhenApproachingDoor_RespectsPhysicalCollision(
        string doorType,
        bool shouldCollide
    )
    {
        // Arrange
        var (runner, room, _, _, _) = LoadDoor(
            "north",
            doorType
        );

        var player = CreatePlayer(
            room,
            new Vector2(128, 58)
        );

        player.SetPhysicsProcess(false);

        await runner.SimulateFrames(3);

        // Act - test a 64px movement through the doorway.
        // Link's actual position is not changed.
        var hasCollision = player.TestMove(
            player.GlobalTransform,
            Vector2.Up * 64.0f
        );

        // Assert
        AssertBool(
            hasCollision == shouldCollide
        ).IsTrue();
    }

    private static (
        ISceneRunner Runner,
        Room Room,
        Door Door,
        DoorData Data,
        DungeonState State
    ) LoadDoor(
        string direction,
        string type,
        DungeonState? state = null
    )
    {
        var (runner, room) = RoomTestFixture.Load();

        var data = new DoorData
        {
            Direction = direction,
            Type = type,
            Destination = "test_destination"
        };

        var roomData = RoomDataFactory.Create();
        roomData.Doors.Add(data);

        state ??= new DungeonState();

        room.LoadLayout(roomData, state);

        var door = room.GetNode<Node2D>("Doors")
            .GetChild<Door>(0);

        return (runner, room, door, data, state);
    }

    private static Player CreatePlayer(
        Room room,
        Vector2 position
    )
    {
        var scene = ResourceLoader.Load<PackedScene>(
            "res://Scenes/Player.tscn"
        );

        var player = scene.Instantiate<Player>();

        player.Position = position;
        room.AddChild(player);

        return player;
    }

    private static List<string> EnsureMovementActions()
    {
        var createdActions = new List<string>();

        foreach (var action in new[]
        {
            "move_up",
            "move_down",
            "move_left",
            "move_right"
        })
        {
            if (InputMap.HasAction(action))
            {
                continue;
            }

            InputMap.AddAction(action);
            createdActions.Add(action);
        }

        return createdActions;
    }
}
