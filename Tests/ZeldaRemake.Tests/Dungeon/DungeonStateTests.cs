using ZeldaRemake.Models;

namespace ZeldaRemake.UnitTests.Dungeon;

public class DungeonStateTests
{
    [Fact]
    public void TryCollectKey_WhenKeyIsUncollected_IncrementsKeys()
    {
        // Arrange
        var state = new DungeonState();

        // Act
        var collected = state.TryCollectKey(
            "entrance",
            "key_01"
        );

        // Assert
        Assert.True(collected);
        Assert.Equal(1, state.Keys);
    }

    [Fact]
    public void TryCollectKey_WhenKeyAlreadyCollected_DoesNotIncrementKeys()
    {
        // Arrange
        var state = new DungeonState();
        state.TryCollectKey("entrance", "key_01");

        // Act
        var result = state.TryCollectKey(
            "entrance",
            "key_01"
        );

        // Assert
        Assert.False(result);
        Assert.Equal(1, state.Keys);
    }

    [Fact]
    public void TryUnlockDoor_WhenNoKeys_ReturnsFalse()
    {
        // Arrange
        var state = new DungeonState();

        var door = new DoorData
        {
            Direction = "east",
            Type = "locked"
        };

        // Act
        var result = state.TryUnlockDoor(
            "entrance",
            door
        );

        // Assert
        Assert.False(result);
        Assert.False(state.IsDoorOpen("entrance", door));
    }

    [Fact]
    public void TryUnlockDoor_WhenPlayerHasKey_UnlocksDoorAndConsumesKey()
    {
        // Arrange
        var state = new DungeonState();

        var door = new DoorData
        {
            Direction = "east",
            Type = "locked"
        };

        state.TryCollectKey("entrance", "key_01");

        // Act
        var result = state.TryUnlockDoor(
            "entrance",
            door
        );

        // Assert
        Assert.True(result);
        Assert.True(state.IsDoorOpen("entrance", door));
        Assert.Equal(0, state.Keys);
    }

    [Fact]
    public void TryUnlockDoor_WhenDoorAlreadyUnlocked_DoesNotConsumeAnotherKey()
    {
        // Arrange
        var state = new DungeonState();

        var door = new DoorData
        {
            Direction = "east",
            Type = "locked"
        };

        state.TryCollectKey("entrance", "key_01");
        state.TryCollectKey("entrance", "key_02");
        state.TryUnlockDoor("entrance", door);

        // Act
        var result = state.TryUnlockDoor(
            "entrance",
            door
        );

        // Assert
        Assert.True(result);
        Assert.Equal(1, state.Keys);
    }

    [Fact]
    public void TryOpenShutter_WhenShutterClosed_OpensShutter()
    {
        // Arrange
        var state = new DungeonState();

        var door = new DoorData
        {
            Direction = "south",
            Type = "shutter"
        };

        // Act
        var result = state.TryOpenShutter(
            "entrance",
            door
        );

        // Assert
        Assert.True(result);
        Assert.True(state.IsDoorOpen("entrance", door));
    }

    [Fact]
    public void TryOpenShutter_WhenShutterAlreadyOpen_ReturnsFalse()
    {
        // Arrange
        var state = new DungeonState();

        var door = new DoorData
        {
            Direction = "south",
            Type = "shutter"
        };

        state.TryOpenShutter("entrance", door);

        // Act
        var result = state.TryOpenShutter(
            "entrance",
            door
        );

        // Assert
        Assert.False(result);
        Assert.True(state.IsDoorOpen("entrance", door));
    }

    [Fact]
    public void ResetShutters_WhenShutterOpen_ClosesShutter()
    {
        // Arrange
        var state = new DungeonState();

        var door = new DoorData
        {
            Direction = "south",
            Type = "shutter"
        };

        state.TryOpenShutter("entrance", door);

        // Act
        state.ResetShutters("entrance", [door]);

        // Assert
        Assert.False(state.IsDoorOpen("entrance", door));
    }

    [Fact]
    public void ResetShutters_WhenOtherRoomHasOpenShutter_DoesNotCloseOtherRoomShutter()
    {
        // Arrange
        var state = new DungeonState();

        var door = new DoorData
        {
            Direction = "south",
            Type = "shutter"
        };

        state.TryOpenShutter("entrance", door);
        state.TryOpenShutter("another_room", door);

        // Act
        state.ResetShutters("entrance", [door]);

        // Assert
        Assert.False(state.IsDoorOpen("entrance", door));
        Assert.True(state.IsDoorOpen("another_room", door));
    }

    [Fact]
    public void TryCompletePuzzle_WhenPuzzleIncomplete_MarksPuzzleCompleted()
    {
        // Arrange
        var state = new DungeonState();

        // Act
        var result = state.TryCompletePuzzle(
            "entrance",
            "staircase_01"
        );

        // Assert
        Assert.True(result);

        Assert.True(
            state.IsPuzzleCompleted(
                "entrance",
                "staircase_01"
            )
        );
    }

    [Fact]
    public void TryCompletePuzzle_WhenPuzzleAlreadyCompleted_ReturnsFalse()
    {
        // Arrange
        var state = new DungeonState();

        state.TryCompletePuzzle(
            "entrance",
            "staircase_01"
        );

        // Act
        var result = state.TryCompletePuzzle(
            "entrance",
            "staircase_01"
        );

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Restore_WhenSaveContainsCompletedPuzzle_RestoresPuzzleCompletion()
    {
        // Arrange
        var state = new DungeonState();

        state.TryCompletePuzzle(
            "entrance",
            "staircase_01"
        );

        var save = new DungeonSaveData();
        state.CopyTo(save);

        var restored = new DungeonState();

        // Act
        restored.Restore(save);

        // Assert
        Assert.True(
            restored.IsPuzzleCompleted(
                "entrance",
                "staircase_01"
            )
        );
    }

    [Fact]
    public void Restore_WhenShutterWasOpen_DoesNotRestoreShutter()
    {
        // Arrange
        var state = new DungeonState();

        var door = new DoorData
        {
            Direction = "south",
            Type = "shutter"
        };

        state.TryOpenShutter("entrance", door);

        var save = new DungeonSaveData();
        state.CopyTo(save);

        var restored = new DungeonState();

        // Act
        restored.Restore(save);

        // Assert
        Assert.False(
            restored.IsDoorOpen("entrance", door)
        );
    }
}
