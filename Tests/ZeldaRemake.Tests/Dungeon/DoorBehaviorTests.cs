
using ZeldaRemake.Doors;
using ZeldaRemake.Models;

namespace ZeldaRemake.UnitTests.Dungeon;

public class DoorBehaviorTests
{
    [Theory]
    [InlineData("north")]
    [InlineData("south")]
    [InlineData("east")]
    [InlineData("west")]
    public void IsOpen_WhenDoorIsStandard_ReturnsTrue(
        string direction
    )
    {
        // Arrange
        var state = new DungeonState();
        var behavior = new StandardDoorBehavior();

        var door = new DoorData
        {
            Direction = direction,
            Type = "open"
        };

        // Act
        var result = behavior.IsOpen(
            state,
            "test_room",
            door
        );

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void TryOpen_WhenLockedDoorHasNoKey_ReturnsFalse()
    {
        // Arrange
        var state = new DungeonState();
        var behavior = new LockedDoorBehavior();

        var door = new DoorData
        {
            Direction = "north",
            Type = "locked"
        };

        // Act
        var result = behavior.TryOpen(
            state,
            "test_room",
            door
        );

        // Assert
        Assert.False(result);
        Assert.False(behavior.IsOpen(state, "test_room", door));
    }

    [Fact]
    public void TryOpen_WhenLockedDoorHasKey_ConsumesOneKey()
    {
        // Arrange
        var state = new DungeonState();
        var behavior = new LockedDoorBehavior();

        var door = new DoorData
        {
            Direction = "north",
            Type = "locked"
        };

        state.TryCollectKey("test_room", "key_01");

        // Act
        var result = behavior.TryOpen(
            state,
            "test_room",
            door
        );

        // Assert
        Assert.True(result);
        Assert.True(behavior.IsOpen(state, "test_room", door));
        Assert.Equal(0, state.Keys);
    }

    [Fact]
    public void TryOpen_WhenShutterIsClosed_DoesNotOpenIt()
    {
        // Arrange
        var state = new DungeonState();
        var behavior = new ShutterDoorBehavior();

        var door = new DoorData
        {
            Direction = "south",
            Type = "shutter"
        };

        // Act
        var result = behavior.TryOpen(
            state,
            "test_room",
            door
        );

        // Assert
        Assert.False(result);
        Assert.False(behavior.IsOpen(state, "test_room", door));
    }

    [Fact]
    public void IsOpen_WhenShutterIsReset_ReturnsFalse()
    {
        // Arrange
        var state = new DungeonState();
        var behavior = new ShutterDoorBehavior();

        var door = new DoorData
        {
            Direction = "south",
            Type = "shutter"
        };

        state.TryOpenShutter("test_room", door);

        // Act
        state.ResetShutters("test_room", [door]);

        // Assert
        Assert.False(behavior.IsOpen(state, "test_room", door));
    }

    [Fact]
    public void Restore_WhenShutterWasOpen_ClosesShutter()
    {
        // Arrange
        var state = new DungeonState();

        var door = new DoorData
        {
            Direction = "south",
            Type = "shutter"
        };

        state.TryOpenShutter("test_room", door);

        // Act
        state.Restore(new DungeonSaveData());

        // Assert
        Assert.False(state.IsDoorOpen("test_room", door));
    }
}
