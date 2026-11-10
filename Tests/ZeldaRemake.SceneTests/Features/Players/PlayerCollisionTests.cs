
using GdUnit4;
using Godot;
using ZeldaRemake.Models;
using ZeldaRemake.SceneTests.Features.Rooms.Fixtures;
using static GdUnit4.Assertions;

namespace ZeldaRemake.SceneTests.Features.Players;

[TestSuite]
[RequireGodotRuntime]
public class PlayerCollisionTests
{
    private const string PlayerScenePath =
        "res://Scenes/Player.tscn";

    private static readonly Vector2 WallCenter = new(128, 88);

    [TestCase("move_up", 0, -1)]
    [TestCase("move_down", 0, 1)]
    [TestCase("move_left", -1, 0)]
    [TestCase("move_right", 1, 0)]
    public async Task Movement_WhenApproachingStaticWall_StopsAtCollider(
        string action,
        int directionX,
        int directionY
    )
    {
        // Arrange
        var (runner, room) = RoomTestFixture.Load();

        var wall = AddWall(
            room,
            WallCenter,
            new Vector2(16, 16)
        );

        var direction = new Vector2(
            directionX,
            directionY
        );

        var player = CreatePlayer(
            room,
            WallCenter - direction * 32.0f
        );

        var actions = EnsureMovementActions();

        try
        {
            await runner.SimulateFrames(2);

            // Act
            runner.SimulateActionPress(action);

            await runner.AwaitInputProcessed();
            await runner.SimulateFrames(35);

            var separation = (
                player.Position - wall.Position
            ).Dot(direction);

            var perpendicular = new Vector2(
                -direction.Y,
                direction.X
            );

            var lateralError = Mathf.Abs(
                (player.Position - wall.Position)
                    .Dot(perpendicular)
            );

            // Assert - Link approaches the wall.
            AssertBool(
                separation > -30.0f
            ).IsTrue();

            // 8px wall half-size + 6px player half-size.
            // Link must remain on the approach side.
            AssertBool(
                separation <= -13.5f
            ).IsTrue();

            AssertBool(
                lateralError < 0.5f
            ).IsTrue();
        }
        finally
        {
            runner.SimulateActionRelease(action);
            await runner.AwaitInputProcessed();

            RemoveCreatedActions(actions);
        }
    }

    [TestCase]
    public async Task Movement_WhenMovingDiagonallyIntoWall_SlidesAlongSurface()
    {
        // Arrange
        var (runner, room) = RoomTestFixture.Load();

        // A long horizontal wall.
        AddWall(
            room,
            new Vector2(128, 96),
            new Vector2(160, 16)
        );

        var player = CreatePlayer(
            room,
            new Vector2(96, 118)
        );

        var actions = EnsureMovementActions();

        try
        {
            await runner.SimulateFrames(2);

            var initialPosition = player.Position;

            // Act - move diagonally up and right.
            runner.SimulateActionPress("move_up");
            runner.SimulateActionPress("move_right");

            await runner.AwaitInputProcessed();
            await runner.SimulateFrames(28);

            // Assert - horizontal movement continues.
            AssertBool(
                player.Position.X > initialPosition.X + 10.0f
            ).IsTrue();

            // Link approaches the wall vertically.
            AssertBool(
                player.Position.Y < initialPosition.Y - 4.0f
            ).IsTrue();

            // Wall bottom = 104, player half-height = 6.
            // Link's centre cannot pass approximately Y=110.
            AssertBool(
                player.Position.Y >= 109.5f
            ).IsTrue();
        }
        finally
        {
            runner.SimulateActionRelease("move_up");
            runner.SimulateActionRelease("move_right");

            await runner.AwaitInputProcessed();

            RemoveCreatedActions(actions);
        }
    }

    [TestCase]
    public async Task Movement_WhenMovingIntoCorner_DoesNotPassThroughWalls()
    {
        // Arrange
        var (runner, room) = RoomTestFixture.Load();

        // Two walls forming an inside corner.
        AddWall(
            room,
            new Vector2(128, 88),
            new Vector2(16, 160)
        );

        AddWall(
            room,
            new Vector2(88, 128),
            new Vector2(160, 16)
        );

        var player = CreatePlayer(
            room,
            new Vector2(100, 100)
        );

        var actions = EnsureMovementActions();

        try
        {
            await runner.SimulateFrames(2);

            // Act - move diagonally into the corner.
            runner.SimulateActionPress("move_down");
            runner.SimulateActionPress("move_right");

            await runner.AwaitInputProcessed();
            await runner.SimulateFrames(40);

            // Assert - movement occurred before contact.
            AssertBool(player.Position.X > 108.0f).IsTrue();
            AssertBool(player.Position.Y > 108.0f).IsTrue();

            // Both walls begin at coordinate 120.
            // The player's half-size is 6.
            AssertBool(
                player.Position.X <= 114.5f
            ).IsTrue();

            AssertBool(
                player.Position.Y <= 114.5f
            ).IsTrue();
        }
        finally
        {
            runner.SimulateActionRelease("move_down");
            runner.SimulateActionRelease("move_right");

            await runner.AwaitInputProcessed();

            RemoveCreatedActions(actions);
        }
    }

    [TestCase("move_up", 0, -1)]
    [TestCase("move_down", 0, 1)]
    [TestCase("move_left", -1, 0)]
    [TestCase("move_right", 1, 0)]
    public async Task Movement_WhenApproachingDungeonWall_StopsAtTileCollision(
        string action,
        int directionX,
        int directionY
    )
    {
        // Arrange
        var (runner, room) = RoomTestFixture.Load();

        var roomData = RoomDataFactory.Create();

        roomData.InteriorWalls.Add(new InteriorWallData
        {
            X = 7,
            Y = 4,
            Width = 1,
            Height = 1
        });

        room.LoadLayout(roomData, new DungeonState());

        var walls = room.GetNode<TileMapLayer>("Walls");

        AssertBool(
            walls.GetCellSourceId(new Vector2I(7, 4)) != -1
        ).IsTrue();

        var direction = new Vector2(
            directionX,
            directionY
        );

        // Tile (7,4) has centre (120,72).
        var wallCenter = new Vector2(120, 72);

        var player = CreatePlayer(
            room,
            wallCenter - direction * 32.0f
        );

        var actions = EnsureMovementActions();

        try
        {
            await runner.SimulateFrames(3);

            // Act
            runner.SimulateActionPress(action);

            await runner.AwaitInputProcessed();
            await runner.SimulateFrames(35);

            var separation = (
                player.Position - wallCenter
            ).Dot(direction);

            // Assert - Link approached the tile.
            AssertBool(
                separation > -30.0f
            ).IsTrue();

            // The tile is 16px and Link is 12px.
            // Their centres must not overlap.
            AssertBool(
                separation <= -13.5f
            ).IsTrue();
        }
        finally
        {
            runner.SimulateActionRelease(action);
            await runner.AwaitInputProcessed();

            RemoveCreatedActions(actions);
        }
    }

    private static StaticBody2D AddWall(
        Room room,
        Vector2 position,
        Vector2 size
    )
    {
        var wall = new StaticBody2D
        {
            Position = position
        };

        var collision = new CollisionShape2D
        {
            Shape = new RectangleShape2D
            {
                Size = size
            }
        };

        wall.AddChild(collision);
        room.AddChild(wall);

        return wall;
    }

    private static Player CreatePlayer(
        Room room,
        Vector2 position
    )
    {
        var scene = ResourceLoader.Load<PackedScene>(
            PlayerScenePath
        );

        var player = scene.Instantiate<Player>();

        player.Position = position;
        room.AddChild(player);

        return player;
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
