
using GdUnit4;
using Godot;
using ZeldaRemake.SceneTests.Features.Rooms.Fixtures;
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
    public void LoadLayout_WhenStaircasePuzzleIsIncomplete_HidesStaircase()
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
        var objects = room.GetNode<Node2D>("Objects");
        var staircase = objects.GetChild(0) as Staircase;

        AssertBool(staircase is not null).IsTrue();
        AssertBool(staircase!.Visible).IsFalse();
    }

    [TestCase]
    public void LoadLayout_WhenStaircasePuzzleCompletes_RevealsStaircase()
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

        var state = new DungeonState();

        room.LoadLayout(roomData, state);

        var staircase = room.GetNode<Node2D>("Objects")
            .GetChild(0) as Staircase;

        AssertBool(staircase is not null).IsTrue();
        AssertBool(staircase!.Visible).IsFalse();

        // Act
        state.TryCompletePuzzle(
            roomData.Id,
            "test_staircase"
        );

        // Assert
        AssertBool(staircase.Visible).IsTrue();
    }
}
