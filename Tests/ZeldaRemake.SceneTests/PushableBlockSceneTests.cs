
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace ZeldaRemake.SceneTests;

[TestSuite]
[RequireGodotRuntime]
public class PushableBlockSceneTests
{
    [TestCase]
    public void Load_WhenSceneExists_InstantiatesPushableBlock()
    {
        // Arrange
        const string path = "res://Scenes/PushableBlock.tscn";

        // Act
        var packedScene = ResourceLoader.Load<PackedScene>(path);
        var instance = packedScene?.Instantiate();

        try
        {
            // Assert
            AssertBool(packedScene is not null).IsTrue();
            AssertBool(instance is PushableBlock).IsTrue();

            AssertBool(
                instance?.GetNodeOrNull<CollisionShape2D>(
                    "CollisionShape2D"
                ) is not null
            ).IsTrue();
        }
        finally
        {
            instance?.Free();
        }
    }


    [TestCase]
    public void TryPush_WhenDirectionIsNotAllowed_ReturnsFalse()
    {
        // Arrange
        var runner = ISceneRunner.Load(
            "res://Scenes/PushableBlock.tscn"
        );

        var block = runner.Scene() as PushableBlock;

        using var player = new Player();

        block!.Configure("north");

        // Act
        var result = block.TryPush(
            Vector2.Left,
            player
        );

        // Assert
        AssertBool(result).IsFalse();
    }

    [TestCase]
    public void TryPush_WhenPushingIsDisabled_ReturnsFalse()
    {
        // Arrange
        var runner = ISceneRunner.Load(
            "res://Scenes/PushableBlock.tscn"
        );

        var block = runner.Scene() as PushableBlock;

        using var player = new Player();

        block!.Configure("north");
        block!.DisablePushing();

        // Act
        var result = block!.TryPush(
            Vector2.Up,
            player
        );

        // Assert
        AssertBool(result).IsFalse();
    }

    [TestCase]
    public void Reset_WhenPushingIsDisabled_DoesNotEnablePushing()
    {
        // Arrange
        var runner = ISceneRunner.Load(
            "res://Scenes/PushableBlock.tscn"
        );

        var block = runner.Scene() as PushableBlock;

        using var player = new Player();

        block!.Configure("north");
        block!.DisablePushing();

        // Act
        block!.Reset();

        var result = block!.TryPush(
            Vector2.Up,
            player
        );

        // Assert
        AssertBool(result).IsFalse();
    }
}
