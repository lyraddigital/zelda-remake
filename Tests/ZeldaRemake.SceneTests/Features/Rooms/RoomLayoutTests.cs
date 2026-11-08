
using GdUnit4;
using Godot;
using ZeldaRemake.Models;
using ZeldaRemake.SceneTests.Features.Rooms.Fixtures;
using static GdUnit4.Assertions;

namespace ZeldaRemake.SceneTests.Features.Rooms;

[TestSuite]
[RequireGodotRuntime]
public class RoomLayoutTests
{
    private const string RoomScenePath = "res://Scenes/Room.tscn";

    [TestCase]
    [ThrowsException(typeof(ArgumentException))]
    public void LoadLayout_WhenRoomHasInvalidHeight_ThrowsArgumentException()
    {
        // Arrange
        var (_, room) = LoadRoom();

        var roomData = RoomDataFactory.Create();

        roomData.Walls = roomData.Walls.Take(10).ToArray();

        // Act
        room.LoadLayout(roomData, new DungeonState());

        // Assert
        // The ThrowsException attribute verifies the exception.
    }

    [TestCase]
    [ThrowsException(typeof(ArgumentException))]
    public void LoadLayout_WhenRoomHasInvalidWidth_ThrowsArgumentException()
    {
        // Arrange
        var (_, room) = LoadRoom();

        var roomData = RoomDataFactory.Create();

        roomData.Walls[0] = "...............";

        // Act
        room.LoadLayout(roomData, new DungeonState());

        // Assert
        // The ThrowsException attribute verifies the exception.
    }

    [TestCase]
    public void LoadLayout_WhenRoomIsValid_CreatesExpectedTiles()
    {
        // Arrange
        var (_, room) = LoadRoom();

        var roomData = RoomDataFactory.Create();

        roomData.Walls[4] = "....#..s........";

        // Act
        room.LoadLayout(roomData, new DungeonState());

        // Assert
        var floors = room.GetNode<TileMapLayer>("Floors");
        var walls = room.GetNode<TileMapLayer>("Walls");

        var wallPosition = new Vector2I(4, 4);
        var stonePosition = new Vector2I(7, 4);
        var grassPosition = new Vector2I(5, 4);

        // Every tile position receives a floor.
        AssertBool(
            floors.GetUsedCells().Count == 16 * 11
        ).IsTrue();

        // A wall tile creates a wall.
        AssertBool(
            walls.GetCellSourceId(wallPosition) != -1
        ).IsTrue();

        // An empty tile does not create a wall.
        AssertBool(
            walls.GetCellSourceId(grassPosition) == -1
        ).IsTrue();

        // Stone and grass have different floor tiles.
        AssertBool(
            floors.GetCellAtlasCoords(stonePosition) !=
            floors.GetCellAtlasCoords(grassPosition)
        ).IsTrue();
    }

    [TestCase]
    public void LoadLayout_WhenRoomContainsDoor_CreatesDoor()
    {
        // Arrange
        var (_, room) = LoadRoom();

        var roomData = RoomDataFactory.Create();

        roomData.Doors.Add(new DoorData
        {
            Direction = "north",
            Destination = "another_room",
            Type = "open"
        });

        // Act
        room.LoadLayout(roomData, new DungeonState());

        // Assert
        var doors = room.GetNode<Node2D>("Doors");

        AssertBool(
            doors.GetChildCount() == 1
        ).IsTrue();

        var door = doors.GetChild(0) as Door;

        AssertBool(door is not null).IsTrue();

        AssertBool(
            door!.Position.IsEqualApprox(
                new Vector2(128, 16)
            )
        ).IsTrue();
    }

    private static (ISceneRunner Runner, Room Room) LoadRoom()
    {
        var runner = ISceneRunner.Load(RoomScenePath);

        var room = runner.Scene() as Room
            ?? throw new InvalidOperationException(
                "Failed to load Room scene."
            );

        return (runner, room);
    }
}
