
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
    public void LoadLayout_WhenRoomHasNoInteriorWalls_CreatesPerimeter()
    {
        // Arrange
        var (_, room) = LoadRoom();

        var roomData = RoomDataFactory.Create();

        // Act
        room.LoadLayout(roomData, new DungeonState());

        // Assert
        var floors = room.GetNode<TileMapLayer>("Floors");
        var walls = room.GetNode<TileMapLayer>("Walls");

        AssertBool(
            floors.GetUsedCells().Count == 16 * 11
        ).IsTrue();

        AssertBool(
            walls.GetCellSourceId(new Vector2I(0, 0)) != -1
        ).IsTrue();

        AssertBool(
            walls.GetCellSourceId(new Vector2I(15, 10)) != -1
        ).IsTrue();

        AssertBool(
            walls.GetCellSourceId(new Vector2I(8, 5)) == -1
        ).IsTrue();
    }

    [TestCase]
    public void LoadLayout_WhenInteriorWallsExist_CreatesExpectedWalls()
    {
        // Arrange
        var (_, room) = LoadRoom();

        var roomData = RoomDataFactory.Create();

        roomData.InteriorWalls.Add(new InteriorWallData
        {
            X = 4,
            Y = 3,
            Width = 3,
            Height = 1
        });

        // Act
        room.LoadLayout(roomData, new DungeonState());

        // Assert
        var walls = room.GetNode<TileMapLayer>("Walls");

        for (var x = 4; x <= 6; x++)
        {
            AssertBool(
                walls.GetCellSourceId(new Vector2I(x, 3)) != -1
            ).IsTrue();
        }

        AssertBool(
            walls.GetCellSourceId(new Vector2I(7, 3)) == -1
        ).IsTrue();
    }

    [TestCase]
    [ThrowsException(typeof(ArgumentException))]
    public void LoadLayout_WhenInteriorWallOverlapsPerimeter_ThrowsArgumentException()
    {
        // Arrange
        var (_, room) = LoadRoom();

        var roomData = RoomDataFactory.Create();

        roomData.InteriorWalls.Add(new InteriorWallData
        {
            X = 13,
            Y = 4,
            Width = 2,
            Height = 1
        });

        // Act
        room.LoadLayout(roomData, new DungeonState());

        // Assert
        // The ThrowsException attribute verifies the exception.
    }

    [TestCase]
    public void LoadLayout_WhenNorthDoorExists_LeavesPerimeterOpening()
    {
        // Arrange
        var (_, room) = LoadRoom();

        var roomData = RoomDataFactory.Create();

        roomData.Doors.Add(new DoorData
        {
            Direction = "north",
            Destination = "test_destination",
            Type = "open"
        });

        // Act
        room.LoadLayout(roomData, new DungeonState());

        // Assert
        var walls = room.GetNode<TileMapLayer>("Walls");

        AssertBool(
            walls.GetCellSourceId(new Vector2I(7, 0)) == -1
        ).IsTrue();

        AssertBool(
            walls.GetCellSourceId(new Vector2I(8, 1)) == -1
        ).IsTrue();

        AssertBool(
            walls.GetCellSourceId(new Vector2I(6, 0)) != -1
        ).IsTrue();
    }

    [TestCase]
    public void LoadLayout_WhenFloorOverrideExists_UsesConfiguredFloor()
    {
        // Arrange
        var (_, room) = LoadRoom();

        var roomData = RoomDataFactory.Create();

        roomData.FloorOverrides.Add(new FloorOverrideData
        {
            X = 5,
            Y = 5,
            Floor = "stone"
        });

        // Act
        room.LoadLayout(roomData, new DungeonState());

        // Assert
        var floors = room.GetNode<TileMapLayer>("Floors");

        AssertBool(
            floors.GetCellAtlasCoords(new Vector2I(5, 5)) !=
            floors.GetCellAtlasCoords(new Vector2I(6, 5))
        ).IsTrue();
    }

    [TestCase]
    public void LoadLayout_WhenRoomIsValid_CreatesExpectedTiles()
    {
        // Arrange
        var (_, room) = LoadRoom();

        var roomData = RoomDataFactory.Create();

        roomData.InteriorWalls.Add(new InteriorWallData
        {
            X = 4,
            Y = 4
        });

        roomData.FloorOverrides.Add(new FloorOverrideData
        {
            X = 7,
            Y = 4,
            Floor = "stone"
        });

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

        // An interior wall is created at the configured position.
        AssertBool(
            walls.GetCellSourceId(wallPosition) != -1
        ).IsTrue();

        // An empty interior tile does not create a wall.
        AssertBool(
            walls.GetCellSourceId(grassPosition) == -1
        ).IsTrue();

        // The floor override uses a different tile from the default.
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
