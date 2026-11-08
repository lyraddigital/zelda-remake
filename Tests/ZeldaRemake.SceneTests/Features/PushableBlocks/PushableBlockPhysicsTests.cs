
using GdUnit4;
using Godot;
using ZeldaRemake.SceneTests.Helpers;
using static GdUnit4.Assertions;

namespace ZeldaRemake.SceneTests.Features.PushableBlocks;

[TestSuite]
[RequireGodotRuntime]
public class PushableBlockPhysicsSceneTests
{
    private const string FixturePath =
        "res://Features/PushableBlocks/Fixtures/PushableBlockFixture.tscn";

    private const float TileSize = 16.0f;

    [TestCase]
    public async Task TryPush_WhenDestinationIsClear_MovesOneTile()
    {
        // Arrange
        var (runner, block, player, _) = LoadFixture();

        var initialPosition = block.Position;

        await runner.SimulateFrames(2);

        // Act
        var result = block.TryPush(Vector2.Up, player);

        await runner.SimulateFrames(20);

        // Assert
        AssertBool(result).IsTrue();

        AssertBool(
            block.Position.IsEqualApprox(
                initialPosition + Vector2.Up * TileSize
            )
        ).IsTrue();
    }

    [TestCase]
    public async Task TryPush_WhenDestinationIsBlocked_ReturnsFalse()
    {
        // Arrange
        var (runner, block, player, wall) = LoadFixture();

        var initialPosition = block.Position;

        wall.Position = initialPosition + Vector2.Up * TileSize;

        await runner.SimulateFrames(2);

        // Act
        var result = block.TryPush(Vector2.Up, player);

        // Assert
        AssertBool(result).IsFalse();

        AssertBool(
            block.Position.IsEqualApprox(initialPosition)
        ).IsTrue();
    }

    [TestCase]
    public async Task TryPush_WhenBlockAlreadyMoved_ReturnsFalse()
    {
        // Arrange
        var (runner, block, player, _) = LoadFixture();

        await runner.SimulateFrames(2);

        var firstResult = block.TryPush(Vector2.Up, player);

        await runner.SimulateFrames(20);

        var positionAfterFirstPush = block.Position;

        // Act
        var secondResult = block.TryPush(Vector2.Up, player);

        // Assert
        AssertBool(firstResult).IsTrue();
        AssertBool(secondResult).IsFalse();

        AssertBool(
            block.Position.IsEqualApprox(positionAfterFirstPush)
        ).IsTrue();
    }

    [TestCase]
    public async Task Reset_WhenBlockHasMoved_RestoresInitialPosition()
    {
        // Arrange
        var (runner, block, player, _) = LoadFixture();

        var initialPosition = block.Position;
        var expectedMovedPosition = initialPosition + Vector2.Up * TileSize;

        await runner.SimulateFrames(2);

        var pushResult = block.TryPush(Vector2.Up, player);

        await runner.SimulateFrames(20);

        var movedPosition = block.Position;
        var movementDifference = movedPosition.DistanceTo(expectedMovedPosition);

        AssertBool(pushResult).IsTrue();

        AssertBool(
            movementDifference <= 0.1f
        ).IsTrue();

        // Act
        block.Reset();

        await block.WaitForPositionAsync(
            initialPosition
        );

        // Assert
        AssertBool(
            block.Position.IsEqualApprox(initialPosition)
        ).IsTrue();
    }

    private static (
        ISceneRunner Runner,
        PushableBlock Block,
        Player Player,
        StaticBody2D Wall
    ) LoadFixture()
    {
        var runner = ISceneRunner.Load(FixturePath, true);

        var scene = runner.Scene();

        var block = scene!.GetNode<PushableBlock>("Block");
        var player = scene!.GetNode<Player>("Player");
        var wall = scene!.GetNode<StaticBody2D>("Wall");

        // Prevent player input from interfering with test execution.
        player.SetPhysicsProcess(false);

        // Configure the test block.
        block.Configure("north");

        // Shorter tween duration keeps physics tests fast.
        block.PushDurationSeconds = 0.05f;

        return (runner, block, player, wall);
    }
}
