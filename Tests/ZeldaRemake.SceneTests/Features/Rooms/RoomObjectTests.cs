
using GdUnit4;
using Godot;
using System.Text.Json;
using ZeldaRemake.Models;
using ZeldaRemake.SceneTests.Features.Rooms.Fixtures;
using ZeldaRemake.SceneTests.Helpers;
using static GdUnit4.Assertions;

namespace ZeldaRemake.SceneTests.Features.Rooms;

[TestSuite]
[RequireGodotRuntime]
public class RoomObjectTests
{
    [TestCase]
    public void LoadLayout_WhenKeyIsConfigured_CreatesKeyAtTilePosition()
    {
        // Arrange
        var (_, room) = RoomTestFixture.Load();

        var roomData = RoomDataFactory.Create();

        roomData.Objects.Add(new RoomObjectData
        {
            Id = "test_key",
            Type = "key",
            X = 5,
            Y = 4
        });

        // Act
        room.LoadLayout(roomData, new DungeonState());

        // Assert
        var objects = room.GetNode<Node2D>("Objects");

        AssertBool(objects.GetChildCount() == 1).IsTrue();

        var key = objects.GetChild(0) as KeyPickup;

        AssertBool(key is not null).IsTrue();

        AssertBool(
            key!.Position.IsEqualApprox(new Vector2(88, 72))
        ).IsTrue();
    }

    [TestCase]
    public void LoadLayout_WhenKeyWasCollected_DoesNotCreateKey()
    {
        // Arrange
        var (_, room) = RoomTestFixture.Load();

        var roomData = RoomDataFactory.Create();

        roomData.Objects.Add(new RoomObjectData
        {
            Id = "test_key",
            Type = "key",
            X = 5,
            Y = 4
        });

        var state = new DungeonState();

        state.TryCollectKey(roomData.Id, "test_key");

        // Act
        room.LoadLayout(roomData, state);

        // Assert
        var objects = room.GetNode<Node2D>("Objects");

        AssertBool(objects.GetChildCount() == 0).IsTrue();
    }

    [TestCase]
    public void LoadLayout_WhenStaircaseHasNoRevealBlock_ShowsStaircase()
    {
        // Arrange
        var (_, room) = RoomTestFixture.Load();

        var roomData = RoomDataFactory.Create();

        roomData.Objects.Add(new RoomObjectData
        {
            Id = "test_staircase",
            Type = "staircase",
            X = 6,
            Y = 4
        });

        // Act
        room.LoadLayout(roomData, new DungeonState());

        // Assert
        var staircase = room.GetNode<Node2D>("Objects")
            .GetChild<Staircase>(0);

        AssertBool(staircase.Visible).IsTrue();

        room.ResetTemporaryState();

        AssertBool(staircase.Visible).IsTrue();
    }

    [TestCase]
    public void LoadLayout_WhenStaircaseHasRevealBlock_HidesStaircase()
    {
        // Arrange
        var (_, room) = RoomTestFixture.Load();

        var roomData = RoomDataFactory.Create();

        roomData.Objects.Add(
            CreateBlock("revealsStaircase", "test_staircase")
        );

        roomData.Objects.Add(new RoomObjectData
        {
            Id = "test_staircase",
            Type = "staircase",
            X = 10,
            Y = 4
        });

        // Act
        room.LoadLayout(roomData, new DungeonState());

        // Assert
        var staircase = room.GetNode<Node2D>("Objects")
            .GetChild<Staircase>(1);

        AssertBool(staircase.Visible).IsFalse();
    }

    [TestCase]
    public async Task TryPush_WhenBlockOpensShutter_OpensConfiguredShutter()
    {
        // Arrange
        var (runner, room) = RoomTestFixture.Load();

        var roomData = RoomDataFactory.Create();

        var shutterData = new DoorData
        {
            Direction = "north",
            Destination = "test_destination",
            Type = "shutter"
        };

        roomData.Doors.Add(shutterData);

        roomData.Objects.Add(
            CreateBlock("opensDoor", "north")
        );

        var state = new DungeonState();

        room.LoadLayout(roomData, state);

        var block = room.GetNode<Node2D>("Objects")
            .GetChild<PushableBlock>(0);

        var shutter = room.GetNode<Node2D>("Doors")
            .GetChild<Door>(0);

        var blocker = shutter.GetNode<CollisionShape2D>(
            "Blocker/CollisionShape2D"
        );

        block.PushDurationSeconds = 0.05f;

        using var player = new Player();

        await runner.SimulateFrames(2);

        AssertBool(state.IsDoorOpen(roomData.Id, shutterData)).IsFalse();
        AssertBool(blocker.Disabled).IsFalse();

        // Act
        var result = block.TryPush(Vector2.Up, player);

        await room.WaitUntilAsync(
            () => state.IsDoorOpen(roomData.Id, shutterData),
            "configured shutter to open"
        );

        await room.WaitUntilAsync(
            () => blocker.Disabled,
            "shutter collision to be disabled"
        );

        // Assert
        AssertBool(result).IsTrue();

        AssertBool(
            state.IsDoorOpen(roomData.Id, shutterData)
        ).IsTrue();

        AssertBool(blocker.Disabled).IsTrue();
    }


    [TestCase]
    [ThrowsException(typeof(InvalidOperationException))]
    public void LoadLayout_WhenBlockReferencesMissingShutter_ThrowsInvalidOperationException()
    {
        // Arrange
        var (_, room) = RoomTestFixture.Load();

        var roomData = RoomDataFactory.Create();

        roomData.Objects.Add(
            CreateBlock("opensDoor", "north")
        );

        // Act
        room.LoadLayout(roomData, new DungeonState());

        // Assert
        // The ThrowsException attribute verifies the exception.
    }

    [TestCase]
    public async Task TryPush_WhenBlockRevealsStaircase_ShowsUntilRoomReset()
    {
        // Arrange
        var (runner, room) = RoomTestFixture.Load();

        var roomData = RoomDataFactory.Create();

        roomData.Objects.Add(new RoomObjectData
        {
            Id = "test_staircase",
            Type = "staircase",
            X = 10,
            Y = 4
        });

        roomData.Objects.Add(
            CreateBlock("revealsStaircase", "test_staircase")
        );

        var state = new DungeonState();

        room.LoadLayout(roomData, state);

        var objects = room.GetNode<Node2D>("Objects");
        var staircase = objects.GetChild<Staircase>(0);
        var block = objects.GetChild<PushableBlock>(1);

        var initialPosition = block.Position;

        block.PushDurationSeconds = 0.05f;

        using var player = new Player();

        await runner.SimulateFrames(2);

        AssertBool(staircase.Visible).IsFalse();

        // Act
        var firstPushResult = block.TryPush(Vector2.Up, player);

        await room.WaitUntilAsync(
            () => staircase.Visible,
            "hidden staircase to appear"
        );

        // Assert
        AssertBool(firstPushResult).IsTrue();
        AssertBool(staircase.Visible).IsTrue();

        AssertBool(
            state.IsPuzzleCompleted(roomData.Id, "test_staircase")
        ).IsFalse();

        // Simulate leaving the room.
        room.ResetTemporaryState();

        await block.WaitForPositionAsync(initialPosition);

        AssertBool(staircase.Visible).IsFalse();

        // Allow the physics state to synchronise after the reset.
        await runner.SimulateFrames(2);

        // The player can reveal the staircase again.
        var secondPushResult = block.TryPush(Vector2.Up, player);

        await room.WaitUntilAsync(
            () => staircase.Visible,
            "hidden staircase to reappear"
        );

        AssertBool(secondPushResult).IsTrue();
        AssertBool(staircase.Visible).IsTrue();
    }

    [TestCase]
    public async Task ResetTemporaryState_WhenBlockMoved_ResetsAndAllowsAnotherPush()
    {
        // Arrange
        var (runner, room) = RoomTestFixture.Load();

        var roomData = RoomDataFactory.Create();

        roomData.Objects.Add(CreateBlock());

        room.LoadLayout(roomData, new DungeonState());

        var block = room.GetNode<Node2D>("Objects")
            .GetChild<PushableBlock>(0);

        var initialPosition = block.Position;
        var movedPosition = initialPosition + Vector2.Up * 16.0f;

        block.PushDurationSeconds = 0.05f;

        using var player = new Player();

        await runner.SimulateFrames(2);

        var firstPushResult = block.TryPush(Vector2.Up, player);

        AssertBool(firstPushResult).IsTrue();

        await block.WaitForPositionAsync(movedPosition);

        // Act
        room.ResetTemporaryState();

        await block.WaitForPositionAsync(initialPosition);

        // Assert
        AssertBool(
            block.Position.IsEqualApprox(initialPosition)
        ).IsTrue();

        var secondPushResult = block.TryPush(Vector2.Up, player);

        AssertBool(secondPushResult).IsTrue();

        await block.WaitForPositionAsync(movedPosition);

        AssertBool(
            block.Position.IsEqualApprox(movedPosition)
        ).IsTrue();
    }


    [TestCase]
    [ThrowsException(typeof(InvalidOperationException))]
    public void LoadLayout_WhenBlockTargetsNonShutterDoor_ThrowsInvalidOperationException()
    {
        // Arrange
        var (_, room) = RoomTestFixture.Load();

        var roomData = RoomDataFactory.Create();

        roomData.Doors.Add(new DoorData
        {
            Direction = "north",
            Destination = "another_room",
            Type = "open"
        });

        roomData.Objects.Add(
            CreateBlock("opensDoor", "north")
        );

        // Act
        room.LoadLayout(roomData, new DungeonState());

        // Assert
        // The ThrowsException attribute verifies the exception.
    }

    [TestCase]
    [ThrowsException(typeof(InvalidOperationException))]
    public void LoadLayout_WhenBlockReferencesMissingStaircase_ThrowsInvalidOperationException()
    {
        // Arrange
        var (_, room) = RoomTestFixture.Load();

        var roomData = RoomDataFactory.Create();

        roomData.Objects.Add(
            CreateBlock("revealsStaircase", "missing_staircase")
        );

        // Act
        room.LoadLayout(roomData, new DungeonState());

        // Assert
        // The ThrowsException attribute verifies the exception.
    }

    [TestCase]
    [ThrowsException(typeof(InvalidOperationException))]
    public void LoadLayout_WhenStaircaseBlockAllowsAnyDirection_ThrowsInvalidOperationException()
    {
        // Arrange
        var (_, room) = RoomTestFixture.Load();

        var roomData = RoomDataFactory.Create();

        roomData.Objects.Add(new RoomObjectData
        {
            Id = "test_staircase",
            Type = "staircase",
            X = 10,
            Y = 4
        });

        var block = CreateBlock(
            "revealsStaircase",
            "test_staircase"
        );

        block.Properties["pushDirection"] =
            JsonSerializer.SerializeToElement("any");

        roomData.Objects.Add(block);

        // Act
        room.LoadLayout(roomData, new DungeonState());

        // Assert
        // The ThrowsException attribute verifies the exception.
    }

    [TestCase]
    [ThrowsException(typeof(InvalidOperationException))]
    public void LoadLayout_WhenBlockHasConflictingPuzzleActions_ThrowsInvalidOperationException()
    {
        // Arrange
        var (_, room) = RoomTestFixture.Load();

        var roomData = RoomDataFactory.Create();

        var block = CreateBlock("opensDoor", "north");

        block.Properties["revealsStaircase"] =
            JsonSerializer.SerializeToElement("test_staircase");

        roomData.Objects.Add(block);

        // Act
        room.LoadLayout(roomData, new DungeonState());

        // Assert
        // The ThrowsException attribute verifies the exception.
    }

    [TestCase]
    [ThrowsException(typeof(ArgumentException))]
    public void LoadLayout_WhenBlockHasInvalidPushDirection_ThrowsArgumentException()
    {
        // Arrange
        var (_, room) = RoomTestFixture.Load();

        var roomData = RoomDataFactory.Create();

        var block = CreateBlock();

        block.Properties["pushDirection"] =
            JsonSerializer.SerializeToElement("diagonal");

        roomData.Objects.Add(block);

        // Act
        room.LoadLayout(roomData, new DungeonState());

        // Assert
        // The ThrowsException attribute verifies the exception.
    }

    [TestCase]
    [ThrowsException(typeof(InvalidOperationException))]
    public void LoadLayout_WhenBlockPropertyIsNotAString_ThrowsInvalidOperationException()
    {
        // Arrange
        var (_, room) = RoomTestFixture.Load();

        var roomData = RoomDataFactory.Create();

        var block = CreateBlock();

        block.Properties["opensDoor"] =
            JsonSerializer.SerializeToElement(123);

        roomData.Objects.Add(block);

        // Act
        room.LoadLayout(roomData, new DungeonState());

        // Assert
        // The ThrowsException attribute verifies the exception.
    }

    [TestCase]
    public void LoadLayout_WhenSavedPuzzleWasCompleted_StillHidesTemporaryStaircase()
    {
        // Arrange
        var (_, room) = RoomTestFixture.Load();

        var roomData = RoomDataFactory.Create();

        roomData.Objects.Add(new RoomObjectData
        {
            Id = "test_staircase",
            Type = "staircase",
            X = 10,
            Y = 4
        });

        roomData.Objects.Add(
            CreateBlock("revealsStaircase", "test_staircase")
        );

        var state = new DungeonState();

        state.TryCompletePuzzle(roomData.Id, "test_staircase");

        // Act
        room.LoadLayout(roomData, state);

        // Assert
        var staircase = room.GetNode<Node2D>("Objects")
            .GetChild<Staircase>(0);

        AssertBool(staircase.Visible).IsFalse();
    }

    private static RoomObjectData CreateBlock(
        string? targetProperty = null,
        string? targetValue = null
    )
    {
        var properties = new Dictionary<string, JsonElement>
        {
            ["pushDirection"] = JsonSerializer.SerializeToElement("north")
        };

        if (targetProperty is not null && targetValue is not null)
        {
            properties[targetProperty] = JsonSerializer.SerializeToElement(
                targetValue
            );
        }

        return new RoomObjectData
        {
            Id = "test_block",
            Type = "pushable_block",
            X = 6,
            Y = 5,
            Properties = properties
        };
    }
}
