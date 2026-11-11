
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace ZeldaRemake.SceneTests.Features.Staircases;

[TestSuite]
[RequireGodotRuntime]
public class CellarSceneTests
{
    [TestCase]
    public void Activate_StartsLinkAtTopOfCentredLadder()
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

        // Act
        cellar.Activate();

        // Assert - the ladder's centre is X=32.
        AssertBool(
            Mathf.IsEqualApprox(
                link.Position.X + link.Size.X / 2.0f,
                32.0f
            )
        ).IsTrue();

        AssertBool(
            Mathf.IsEqualApprox(link.Position.Y, 64.0f)
        ).IsTrue();

        AssertBool(cellar.TryExit()).IsFalse();
        AssertBool(cellar.IsActive).IsTrue();
    }


    [TestCase]
    public void Movement_WhenClimbingDownAndUp_RemainsCentred()
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

        cellar.Activate();

        var startingY = link.Position.Y;

        try
        {
            // Act - climb down for a quarter of a second.
            Input.ActionPress("move_down");
            cellar._PhysicsProcess(0.25);

            var descendedY = link.Position.Y;

            // Assert - Link has moved down.
            AssertBool(descendedY > startingY).IsTrue();

            AssertBool(
                Mathf.IsEqualApprox(
                    link.Position.X + link.Size.X / 2.0f,
                    32.0f
                )
            ).IsTrue();

            // Act - climb back up.
            Input.ActionRelease("move_down");
            Input.ActionPress("move_up");

            cellar._PhysicsProcess(0.125);

            // Assert - Link has moved up.
            AssertBool(link.Position.Y < descendedY).IsTrue();

            AssertBool(
                Mathf.IsEqualApprox(
                    link.Position.X + link.Size.X / 2.0f,
                    32.0f
                )
            ).IsTrue();

            AssertBool(cellar.IsActive).IsTrue();
        }
        finally
        {
            Input.ActionRelease("move_down");
            Input.ActionRelease("move_up");
        }
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
        link.Position = new Vector2(26, 188);
        AssertBool(cellar.TryExit()).IsFalse();

        // Top of the ladder, still visible.
        link.Position = new Vector2(26, 64);
        AssertBool(cellar.TryExit()).IsFalse();

        // One pixel remains visible.
        link.Position = new Vector2(26, 53);
        AssertBool(cellar.TryExit()).IsFalse();

        // Above the screen, but not on the ladder.
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

        // Link has climbed entirely above the playfield.
        link.Position = new Vector2(26, 52);

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
