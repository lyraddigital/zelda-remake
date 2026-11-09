namespace ZeldaRemake.UnitTests.Dungeon;

public class DungeonSaveCoordinatorTests
{
    [Fact]
    public void Restore_WhenSaveExists_RestoresWithoutAutoSaving()
    {
        // Arrange
        var state = new DungeonState();
        var saveCount = 0;

        using var coordinator = new DungeonSaveCoordinator(
            "test_dungeon",
            state,
            _ => new DungeonSaveData
            {
                DungeonId = "test_dungeon",
                Keys = 2,
                CollectedObjects = ["entrance:key_01"]
            },
            _ => saveCount++,
            () => false,
            _ => { }
        );

        // Act
        coordinator.Restore();

        // Assert
        Assert.Equal(2, state.Keys);
        Assert.True(
            state.IsObjectCollected("entrance", "key_01")
        );
        Assert.Equal(0, saveCount);
    }

    [Fact]
    public void StateChanged_WhenActive_AutoSavesCurrentState()
    {
        // Arrange
        var state = new DungeonState();
        DungeonSaveData? captured = null;

        using var coordinator = new DungeonSaveCoordinator(
            "test_dungeon",
            state,
            _ => null,
            save => captured = save,
            () => false,
            _ => { }
        );

        coordinator.Restore();

        // Act
        state.TryCollectKey("entrance", "key_01");

        // Assert
        Assert.NotNull(captured);
        Assert.Equal("test_dungeon", captured.DungeonId);
        Assert.Equal(1, captured.Keys);
        Assert.Contains(
            "entrance:key_01",
            captured.CollectedObjects
        );
    }

    [Fact]
    public void Save_WhenTransitioning_DoesNotWrite()
    {
        // Arrange
        var state = new DungeonState();
        var transitioning = true;
        var saveCount = 0;

        using var coordinator = new DungeonSaveCoordinator(
            "test_dungeon",
            state,
            _ => null,
            _ => saveCount++,
            () => transitioning,
            _ => { }
        );

        coordinator.Restore();

        // Act - both autosave and manual save
        // should be suppressed during transitions.
        state.TryCollectKey("entrance", "key_01");
        var savedDuringTransition = coordinator.Save();

        // Assert
        Assert.False(savedDuringTransition);
        Assert.Equal(0, saveCount);

        // Act - saving is allowed again afterward.
        transitioning = false;
        var savedAfterTransition = coordinator.Save();

        // Assert
        Assert.True(savedAfterTransition);
        Assert.Equal(1, saveCount);
    }

    [Fact]
    public void Save_WhenCalledManually_WritesCurrentState()
    {
        // Arrange
        var state = new DungeonState();
        DungeonSaveData? captured = null;

        using var coordinator = new DungeonSaveCoordinator(
            "test_dungeon",
            state,
            _ => null,
            save => captured = save,
            () => false,
            _ => { }
        );

        coordinator.Restore();

        // Act
        var result = coordinator.Save();

        // Assert
        Assert.True(result);
        Assert.NotNull(captured);
        Assert.Equal("test_dungeon", captured.DungeonId);
        Assert.Equal(0, captured.Keys);
    }

    [Fact]
    public void Dispose_WhenStateChanges_DoesNotAutoSave()
    {
        // Arrange
        var state = new DungeonState();
        var saveCount = 0;

        var coordinator = new DungeonSaveCoordinator(
            "test_dungeon",
            state,
            _ => null,
            _ => saveCount++,
            () => false,
            _ => { }
        );

        coordinator.Restore();

        // Act
        coordinator.Dispose();
        state.TryCollectKey("entrance", "key_01");

        // Assert
        Assert.Equal(0, saveCount);
    }

    [Fact]
    public void AutoSave_WhenPersistenceFails_ReportsError()
    {
        // Arrange
        var state = new DungeonState();
        Exception? capturedError = null;

        using var coordinator = new DungeonSaveCoordinator(
            "test_dungeon",
            state,
            _ => null,
            _ => throw new IOException("Disk write failed."),
            () => false,
            exception => capturedError = exception
        );

        coordinator.Restore();

        // Act - the state mutation must not fail
        // because the automatic save failed.
        var collected = state.TryCollectKey(
            "entrance",
            "key_01"
        );

        // Assert
        Assert.True(collected);
        Assert.IsType<IOException>(capturedError);

        // Explicit manual saves still propagate errors
        // to the manager's existing try/catch.
        Assert.Throws<IOException>(
            () => coordinator.Save()
        );
    }
}
