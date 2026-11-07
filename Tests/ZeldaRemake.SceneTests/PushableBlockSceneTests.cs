
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
}
