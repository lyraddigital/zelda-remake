
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

namespace ZeldaRemake.SceneTests.Features.Staircases;

[TestSuite]
[RequireGodotRuntime]
public class CellarSceneTests
{
    private const string ScenePath =
        "res://Scenes/ItemCellar.tscn";

    [TestCase]
    public void SpawnAtEntrance_StartsLinkCentredAtTop()
    {
        // Arrange
        var runner = ISceneRunner.Load(ScenePath);
        var cellar = runner.Scene();

        var player = cellar.GetNode<Player>("Link");
        var ladder = cellar.GetNode<Ladder>(
            "LeftLadderTrigger"
        );

        player.SetPhysicsProcess(false);
        player.MovementMode =
            PlayerMovementMode.SideView;

        // Act
        player.SpawnAtLadder(ladder);

        var bounds = ladder.Bounds;

        var expectedPosition = new Vector2(
            bounds.GetCenter().X,
            bounds.Position.Y + 6.0f
        );

        // Assert
        AssertBool(
            player.GlobalPosition.IsEqualApprox(
                expectedPosition
            )
        ).IsTrue();

        AssertBool(
            player.ActiveLadder == ladder
        ).IsTrue();
    }

    [TestCase]
    public void EntranceLadder_DoesNotImmediatelyExit()
    {
        // Arrange
        var runner = ISceneRunner.Load(ScenePath);
        var cellar = runner.Scene();

        var player = cellar.GetNode<Player>("Link");
        var ladder = cellar.GetNode<Ladder>(
            "LeftLadderTrigger"
        );

        player.SetPhysicsProcess(false);
        player.SpawnAtLadder(ladder);

        var topExitCount = 0;

        ladder.PlayerExited += (_, direction) =>
        {
            if (direction == LadderExitDirection.Top)
            {
                topExitCount++;
            }
        };

        // Act
        player.LeaveLadder(ladder);

        // Assert
        AssertBool(topExitCount == 0).IsTrue();
    }

    [TestCase]
    public async Task EntranceLadder_WhenDescendingAndReturning_ExitsAtTop()
    {
        // Arrange
        var runner = ISceneRunner.Load(ScenePath);
        var cellar = runner.Scene();

        var player = cellar.GetNode<Player>("Link");
        var ladder = cellar.GetNode<Ladder>(
            "LeftLadderTrigger"
        );

        player.MovementMode =
            PlayerMovementMode.SideView;

        player.SpawnAtLadder(ladder);

        var createdDown =
            !InputMap.HasAction("move_down");

        var createdUp =
            !InputMap.HasAction("move_up");

        if (createdDown)
        {
            InputMap.AddAction("move_down");
        }

        if (createdUp)
        {
            InputMap.AddAction("move_up");
        }

        var exitCount = 0;

        ladder.PlayerExited += (_, direction) =>
        {
            if (direction == LadderExitDirection.Top)
            {
                exitCount++;
            }
        };

        try
        {
            // Descend to the bottom.
            runner.SimulateActionPress("move_down");

            await runner.AwaitInputProcessed();
            await runner.SimulateFrames(160);

            runner.SimulateActionRelease("move_down");

            await runner.AwaitInputProcessed();

            AssertBool(
                player.GlobalPosition.Y >=
                ladder.Bounds.End.Y - 7.0f
            ).IsTrue();

            AssertBool(exitCount == 0).IsTrue();

            // Climb back to the entrance.
            runner.SimulateActionPress("move_up");

            await runner.AwaitInputProcessed();
            await runner.SimulateFrames(170);

            // Assert
            AssertBool(exitCount == 1).IsTrue();

            AssertBool(
                player.ActiveLadder is null
            ).IsTrue();
        }
        finally
        {
            runner.SimulateActionRelease("move_down");
            runner.SimulateActionRelease("move_up");

            await runner.AwaitInputProcessed();

            if (createdDown)
            {
                InputMap.EraseAction("move_down");
            }

            if (createdUp)
            {
                InputMap.EraseAction("move_up");
            }
        }
    }

    [TestCase]
    public async Task Bow_WhenTouched_IsCollectedOnlyOnce()
    {
        // Arrange
        var runner = ISceneRunner.Load(ScenePath);
        var cellar = runner.Scene();

        var player = cellar.GetNode<Player>("Link");

        var treasure = cellar.GetNode<TreasurePickup>(
            "Treasure"
        );

        var state = new DungeonState();

        treasure.Configure(
            state,
            "bow_cellar",
            "bow"
        );

        player.MovementMode =
            PlayerMovementMode.SideView;

        // Act
        player.GlobalPosition =
            treasure.GlobalPosition;

        await runner.SimulateFrames(3);

        // Assert
        AssertBool(state.HasItem("bow")).IsTrue();

        AssertBool(
            state.IsObjectCollected(
                "bow_cellar",
                "bow"
            )
        ).IsTrue();

        AssertBool(treasure.Visible).IsFalse();

        var save = new DungeonSaveData
        {
            DungeonId = "test_dungeon"
        };

        state.CopyTo(save);

        AssertBool(
            save.OwnedItems.Contains("bow")
        ).IsTrue();
    }

    [TestCase]
    public void Bow_WhenAlreadyOwned_DoesNotReappear()
    {
        // Arrange
        var save = new DungeonSaveData
        {
            DungeonId = "test_dungeon",
            OwnedItems = ["bow"],
            CollectedObjects = ["bow_cellar:bow"]
        };

        var state = new DungeonState();
        state.Restore(save);

        var runner = ISceneRunner.Load(ScenePath);
        var cellar = runner.Scene();

        var treasure = cellar.GetNode<TreasurePickup>(
            "Treasure"
        );

        // Act
        treasure.Configure(
            state,
            "bow_cellar",
            "bow"
        );

        // Assert
        AssertBool(state.HasItem("bow")).IsTrue();
        AssertBool(treasure.Visible).IsFalse();
    }
}
