
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
    public async Task Movement_WhenClimbingDownAndUp_RemainsCentred()
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

        // The scene test project doesn't define these actions.
        var createdUp = !InputMap.HasAction("move_up");
        var createdDown = !InputMap.HasAction("move_down");

        if (createdUp)
        {
            InputMap.AddAction("move_up");
        }

        if (createdDown)
        {
            InputMap.AddAction("move_down");
        }

        try
        {
            cellar.Activate();

            var startingY = link.Position.Y;

            // Act - climb down.
            runner.SimulateActionPress("move_down");

            await runner.AwaitInputProcessed();
            await runner.SimulateFrames(20);

            var descendedY = link.Position.Y;

            // Assert - Link moved down and remains centred.
            AssertBool(descendedY > startingY).IsTrue();

            AssertBool(
                Mathf.IsEqualApprox(
                    link.Position.X + link.Size.X / 2.0f,
                    32.0f
                )
            ).IsTrue();

            // Act - climb back up.
            runner.SimulateActionRelease("move_down");
            await runner.AwaitInputProcessed();

            runner.SimulateActionPress("move_up");

            await runner.AwaitInputProcessed();
            await runner.SimulateFrames(10);

            // Assert - Link moved upwards.
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
            runner.SimulateActionRelease("move_down");
            runner.SimulateActionRelease("move_up");

            await runner.AwaitInputProcessed();

            if (createdUp)
            {
                InputMap.EraseAction("move_up");
            }

            if (createdDown)
            {
                InputMap.EraseAction("move_down");
            }
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
