
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace ZeldaRemake.SceneTests.Features.Staircases;

[TestSuite]
[RequireGodotRuntime]
public class CellarSceneTests
{
    [TestCase]
    public void TryExit_WhenAwayFromLadder_DoesNotExit()
    {
        // Arrange
        var runner = ISceneRunner.Load(
            "res://Scenes/Cellar.tscn"
        );

        var cellar = runner.Scene() as Cellar
            ?? throw new InvalidOperationException(
                "Failed to load cellar scene."
            );

        var exitRequested = false;
        cellar.ExitRequested += () => exitRequested = true;

        cellar.Activate();

        // Act
        var exited = cellar.TryExit();

        // Assert
        AssertBool(exited).IsFalse();
        AssertBool(exitRequested).IsFalse();
        AssertBool(cellar.IsActive).IsTrue();
    }

    [TestCase]
    public void TryExit_WhenLinkIsStillVisible_DoesNotExit()
    {
        // Arrange
        var runner = ISceneRunner.Load(
            "res://Scenes/Cellar.tscn"
        );

        var cellar = runner.Scene() as Cellar
            ?? throw new InvalidOperationException(
                "Failed to load cellar scene."
            );

        var link = cellar.GetNode<ColorRect>("Link");

        var exitCount = 0;
        cellar.ExitRequested += () => exitCount++;

        cellar.Activate();

        // Bottom of the ladder.
        link.Position = new Vector2(24, 188);
        AssertBool(cellar.TryExit()).IsFalse();

        // Link has reached the upper gameplay boundary.
        link.Position = new Vector2(24, 64);
        AssertBool(cellar.TryExit()).IsFalse();

        // One pixel of Link remains in the gameplay area.
        link.Position = new Vector2(24, 53);
        AssertBool(cellar.TryExit()).IsFalse();

        // Link is above the screen but not on the ladder.
        link.Position = new Vector2(48, 52);
        AssertBool(cellar.TryExit()).IsFalse();

        // Assert
        AssertBool(exitCount == 0).IsTrue();
        AssertBool(cellar.IsActive).IsTrue();
    }

    [TestCase]
    public void TryExit_WhenLinkLeavesTop_RequestsReturnOnce()
    {
        // Arrange
        var runner = ISceneRunner.Load(
            "res://Scenes/Cellar.tscn"
        );

        var cellar = runner.Scene() as Cellar
            ?? throw new InvalidOperationException(
                "Failed to load cellar scene."
            );

        var link = cellar.GetNode<ColorRect>("Link");

        var exitCount = 0;
        cellar.ExitRequested += () => exitCount++;

        cellar.Activate();

        // Link has fully climbed out of the playfield.
        link.Position = new Vector2(24, 52);

        // Act
        var firstExit = cellar.TryExit();
        var secondExit = cellar.TryExit();

        // Assert
        AssertBool(firstExit).IsTrue();
        AssertBool(secondExit).IsFalse();
        AssertBool(exitCount == 1).IsTrue();
        AssertBool(cellar.IsActive).IsFalse();
    }
}
