
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
    public void TryExit_WhenLinkLeavesScreen_RequestsReturnOnce()
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

        // Link is at the bottom of the staircase.
        link.Position = new Vector2(24, 188);

        AssertBool(cellar.TryExit()).IsFalse();

        // Link reaches the top of the staircase.
        link.Position = new Vector2(24, 148);

        AssertBool(cellar.TryExit()).IsFalse();

        // Act - Link leaves the screen.
        link.Position = new Vector2(-12, 148);

        var firstExit = cellar.TryExit();
        var secondExit = cellar.TryExit();

        // Assert
        AssertBool(firstExit).IsTrue();
        AssertBool(secondExit).IsFalse();
        AssertBool(exitCount == 1).IsTrue();
        AssertBool(cellar.IsActive).IsFalse();
    }
}
