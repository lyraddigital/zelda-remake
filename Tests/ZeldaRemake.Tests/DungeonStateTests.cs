namespace ZeldaRemake.Tests;

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

        // Act
        state.TryCollectKey("entrance", "key_01");
        var result = state.TryCollectKey(
            "entrance",
            "key_01"
        );

        // Assert
        Assert.False(result);
        Assert.Equal(1, state.Keys);
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

        // Act
        state.CopyTo(save);

        var restored = new DungeonState();
        restored.Restore(save);

        // Assert
        Assert.True(
            restored.IsPuzzleCompleted(
                "entrance",
                "staircase_01"
            )
        );
    }
}
