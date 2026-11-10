
using GdUnit4;
using Godot;
using ZeldaRemake.SceneTests.Helpers;
using static GdUnit4.Assertions;

namespace ZeldaRemake.SceneTests.Features.Players;

[TestSuite]
[RequireGodotRuntime]
public class PlayerSceneTests
{
    private const string PlayerScenePath =
        "res://Scenes/Player.tscn";

    private const string BlockFixturePath =
        "res://Features/PushableBlocks/Fixtures/PushableBlockFixture.tscn";

    [TestCase("move_up", 0, -1)]
    [TestCase("move_down", 0, 1)]
    [TestCase("move_left", -1, 0)]
    [TestCase("move_right", 1, 0)]
    public async Task Movement_WhenDirectionPressed_MovesAtConfiguredSpeed(
        string action,
        int expectedX,
        int expectedY
    )
    {
        // Arrange
        var runner = ISceneRunner.Load(PlayerScenePath);

        var player = runner.Scene() as Player
            ?? throw new InvalidOperationException(
                "Failed to load player scene."
            );

        var createdActions = EnsureMovementActions();

        try
        {
            player.Position = new Vector2(128, 128);

            await runner.SimulateFrames(2);

            var initialPosition = player.Position;
            var expectedDirection = new Vector2(
                expectedX,
                expectedY
            );

            // Act
            runner.SimulateActionPress(action);

            await runner.AwaitInputProcessed();
            await runner.SimulateFrames(8);

            var displacement = player.Position - initialPosition;

            // Assert
            AssertBool(
                displacement.Dot(expectedDirection) > 4.0f
            ).IsTrue();

            AssertBool(
                player.Velocity.IsEqualApprox(
                    expectedDirection * player.Speed
                )
            ).IsTrue();
        }
        finally
        {
            runner.SimulateActionRelease(action);

            await runner.AwaitInputProcessed();

            RemoveCreatedActions(createdActions);
        }
    }

    [TestCase]
    public async Task Movement_WhenInputReleased_StopsImmediately()
    {
        // Arrange
        var runner = ISceneRunner.Load(PlayerScenePath);

        var player = runner.Scene() as Player
            ?? throw new InvalidOperationException(
                "Failed to load player scene."
            );

        var createdActions = EnsureMovementActions();

        try
        {
            await runner.SimulateFrames(2);

            runner.SimulateActionPress("move_right");

            await runner.AwaitInputProcessed();
            await runner.SimulateFrames(8);

            AssertBool(
                player.Velocity.X > 0.0f
            ).IsTrue();

            // Act
            runner.SimulateActionRelease("move_right");

            await runner.AwaitInputProcessed();
            await runner.SimulateFrames(2);

            var stoppedPosition = player.Position;

            await runner.SimulateFrames(5);

            // Assert
            AssertBool(
                player.Velocity.IsEqualApprox(Vector2.Zero)
            ).IsTrue();

            AssertBool(
                player.Position.DistanceTo(stoppedPosition) < 0.1f
            ).IsTrue();
        }
        finally
        {
            runner.SimulateActionRelease("move_right");

            await runner.AwaitInputProcessed();

            RemoveCreatedActions(createdActions);
        }
    }

    [TestCase]
    public async Task Movement_WhenWallBlocksPath_DoesNotPassThrough()
    {
        // Arrange
        var (runner, _, player, _, wall) = LoadBlockFixture();

        var createdActions = EnsureMovementActions();

        player.Position = wall.Position + new Vector2(0, 32);

        try
        {
            await runner.SimulateFrames(2);

            var initialY = player.Position.Y;

            // Act
            runner.SimulateActionPress("move_up");

            await runner.AwaitInputProcessed();
            await runner.SimulateFrames(30);

            // Player is 12px high, wall is 16px high.
            const float minimumSeparation = 14.0f;

            // Assert - player moved towards the wall.
            AssertBool(
                player.Position.Y < initialY - 3.0f
            ).IsTrue();

            // Player did not cross the wall's collision boundary.
            AssertBool(
                player.Position.Y >=
                wall.Position.Y + minimumSeparation - 0.5f
            ).IsTrue();

            AssertBool(
                Mathf.Abs(player.Position.X - wall.Position.X) < 0.5f
            ).IsTrue();
        }
        finally
        {
            runner.SimulateActionRelease("move_up");

            await runner.AwaitInputProcessed();

            RemoveCreatedActions(createdActions);
        }
    }

    [TestCase]
    public async Task Movement_WhenHoldingAgainstBlock_PushesAfterDelay()
    {
        // Arrange
        var (runner, root, player, block, _) = LoadBlockFixture();

        var createdActions = EnsureMovementActions();

        block.PushDelaySeconds = 0.27f;
        block.PushDurationSeconds = 0.05f;

        // Start close enough to make contact quickly.
        player.Position = block.Position + new Vector2(0, 15);

        var initialPosition = block.Position;
        var expectedPosition = initialPosition + Vector2.Up * 16.0f;

        var pushed = false;
        block.Pushed += () => pushed = true;

        try
        {
            await runner.SimulateFrames(2);

            // Act
            runner.SimulateActionPress("move_up");

            await runner.AwaitInputProcessed();
            await runner.SimulateFrames(5);

            // The block must not move immediately.
            AssertBool(
                block.Position.IsEqualApprox(initialPosition)
            ).IsTrue();

            // Continue holding until the push finishes.
            await root.WaitUntilAsync(
                () => pushed,
                "player to push the block",
                5000
            );

            // Assert
            AssertBool(pushed).IsTrue();

            AssertBool(
                block.Position.IsEqualApprox(expectedPosition)
            ).IsTrue();
        }
        finally
        {
            runner.SimulateActionRelease("move_up");

            await runner.AwaitInputProcessed();

            RemoveCreatedActions(createdActions);
        }
    }

    [TestCase]
    public async Task Movement_WhenPushIsInterrupted_ResetsPushDelay()
    {
        // Arrange
        var (runner, _, player, block, _) = LoadBlockFixture();

        var createdActions = EnsureMovementActions();

        block.PushDelaySeconds = 0.35f;
        block.PushDurationSeconds = 0.05f;

        player.Position = block.Position + new Vector2(0, 15);

        var initialPosition = block.Position;

        var pushed = false;
        block.Pushed += () => pushed = true;

        try
        {
            await runner.SimulateFrames(2);

            // Act - hold for less than the required delay.
            runner.SimulateActionPress("move_up");

            await runner.AwaitInputProcessed();
            await runner.SimulateFrames(13);

            runner.SimulateActionRelease("move_up");

            await runner.AwaitInputProcessed();
            await runner.SimulateFrames(4);

            // Start another push attempt.
            runner.SimulateActionPress("move_up");

            await runner.AwaitInputProcessed();
            await runner.SimulateFrames(13);

            runner.SimulateActionRelease("move_up");

            await runner.AwaitInputProcessed();
            await runner.SimulateFrames(6);

            // Assert - two separate short attempts
            // must not combine into one completed push.
            AssertBool(pushed).IsFalse();

            AssertBool(
                block.Position.IsEqualApprox(initialPosition)
            ).IsTrue();
        }
        finally
        {
            runner.SimulateActionRelease("move_up");

            await runner.AwaitInputProcessed();

            RemoveCreatedActions(createdActions);
        }
    }

    [TestCase]
    public async Task Movement_WhenMovingDiagonally_DoesNotPushBlock()
    {
        // Arrange
        var (runner, _, player, block, _) = LoadBlockFixture();

        var createdActions = EnsureMovementActions();

        // Any cardinal direction is allowed, but not diagonal.
        block.Configure("any");
        block.PushDelaySeconds = 0.1f;

        player.Position = block.Position + new Vector2(0, 15);

        var initialPosition = block.Position;

        var pushed = false;
        block.Pushed += () => pushed = true;

        try
        {
            await runner.SimulateFrames(2);

            // Act
            runner.SimulateActionPress("move_up");
            runner.SimulateActionPress("move_right");

            await runner.AwaitInputProcessed();
            await runner.SimulateFrames(45);

            // Assert
            AssertBool(pushed).IsFalse();

            AssertBool(
                block.Position.IsEqualApprox(initialPosition)
            ).IsTrue();
        }
        finally
        {
            runner.SimulateActionRelease("move_up");
            runner.SimulateActionRelease("move_right");

            await runner.AwaitInputProcessed();

            RemoveCreatedActions(createdActions);
        }
    }

    private static (
        ISceneRunner Runner,
        Node2D Root,
        Player Player,
        PushableBlock Block,
        StaticBody2D Wall
    ) LoadBlockFixture()
    {
        var runner = ISceneRunner.Load(
            BlockFixturePath,
            true
        );

        var root = runner.Scene() as Node2D
            ?? throw new InvalidOperationException(
                "Failed to load block fixture."
            );

        var player = root.GetNode<Player>("Player");
        var block = root.GetNode<PushableBlock>("Block");
        var wall = root.GetNode<StaticBody2D>("Wall");

        block.Configure("north");

        return (runner, root, player, block, wall);
    }

    private static List<string> EnsureMovementActions()
    {
        var createdActions = new List<string>();

        foreach (var action in new[]
        {
            "move_up",
            "move_down",
            "move_left",
            "move_right"
        })
        {
            if (InputMap.HasAction(action))
            {
                continue;
            }

            InputMap.AddAction(action);
            createdActions.Add(action);
        }

        return createdActions;
    }

    private static void RemoveCreatedActions(
        IEnumerable<string> actions
    )
    {
        foreach (var action in actions)
        {
            InputMap.EraseAction(action);
        }
    }
}
