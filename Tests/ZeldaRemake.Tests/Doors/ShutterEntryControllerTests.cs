
using ZeldaRemake.Doors;
using ZeldaRemake.Geometry;
using ZeldaRemake.Models;

namespace ZeldaRemake.UnitTests.Doors;

public class ShutterEntryControllerTests
{
    [Theory]
    [InlineData("north", "south")]
    [InlineData("south", "north")]
    [InlineData("east", "west")]
    [InlineData("west", "east")]
    public void PrepareEntry_WhenIncomingDoorIsShutter_OpensIt(
        string travelDirection,
        string entryDirection
    )
    {
        // Arrange
        var state = new DungeonState();
        var controller = new ShutterEntryController(state);

        var (room, shutter) = CreateRoom(entryDirection);

        // Act
        var result = controller.PrepareEntry(
            room,
            travelDirection
        );

        // Assert
        Assert.Same(shutter, result);
        Assert.True(state.IsDoorOpen(room.Id, shutter));
    }

    [Theory]
    [InlineData("open")]
    [InlineData("locked")]
    public void PrepareEntry_WhenIncomingDoorIsNotShutter_DoesNotOpenIt(
        string type
    )
    {
        // Arrange
        var state = new DungeonState();
        var controller = new ShutterEntryController(state);

        var (room, door) = CreateRoom("west", type);

        // Act
        var result = controller.PrepareEntry(room, "east");

        // Assert
        Assert.Null(result);
        Assert.Equal(
            type == "open",
            state.IsDoorOpen(room.Id, door)
        );
    }

    [Theory]
    [InlineData("north", 128, 38, 128, 39)]
    [InlineData("south", 128, 138, 128, 137)]
    [InlineData("west", 38, 88, 39, 88)]
    [InlineData("east", 218, 88, 217, 88)]
    public void TryCloseAfterEntry_ClosesOnlyAfterPlayerClearsDoor(
        string entryDirection,
        float beforeX,
        float beforeY,
        float afterX,
        float afterY
    )
    {
        // Arrange
        var state = new DungeonState();
        var controller = new ShutterEntryController(state);

        var (room, shutter) = CreateRoom(entryDirection);

        var travelDirection = DungeonRoomGeometry.OppositeDirection(
            entryDirection
        );

        var incoming = controller.PrepareEntry(
            room,
            travelDirection
        );

        controller.BeginEntry(room, incoming);

        var origin = DungeonRoomGeometry.GetRoomOrigin(
            room.Position
        );

        // Act - Link has not cleared the doorway.
        var closedEarly = controller.TryCloseAfterEntry(
            origin.X + beforeX,
            origin.Y + beforeY
        );

        // Assert - still open.
        Assert.False(closedEarly);
        Assert.True(state.IsDoorOpen(room.Id, shutter));

        // Act - Link moves completely clear.
        var closed = controller.TryCloseAfterEntry(
            origin.X + afterX,
            origin.Y + afterY
        );

        // Assert - shutter closes.
        Assert.True(closed);
        Assert.False(state.IsDoorOpen(room.Id, shutter));

        // The closure should only happen once.
        Assert.False(controller.TryCloseAfterEntry(
            origin.X + afterX,
            origin.Y + afterY
        ));
    }

    [Fact]
    public void TryCloseAfterEntry_BeforeTransitionCompletes_DoesNotClose()
    {
        // Arrange
        var state = new DungeonState();
        var controller = new ShutterEntryController(state);

        var (room, shutter) = CreateRoom("west");

        controller.PrepareEntry(room, "east");

        var origin = DungeonRoomGeometry.GetRoomOrigin(
            room.Position
        );

        // Act - BeginEntry has not been called yet.
        var result = controller.TryCloseAfterEntry(
            origin.X + 40,
            origin.Y + 88
        );

        // Assert
        Assert.False(result);
        Assert.True(state.IsDoorOpen(room.Id, shutter));
    }

    [Fact]
    public void PrepareEntry_WhenAnotherTransitionStarts_CancelsPendingClosure()
    {
        // Arrange
        var state = new DungeonState();
        var controller = new ShutterEntryController(state);

        var (firstRoom, shutter) = CreateRoom("west");

        var incoming = controller.PrepareEntry(
            firstRoom,
            "east"
        );

        controller.BeginEntry(firstRoom, incoming);

        var nextRoom = new RoomData
        {
            Id = "another_room"
        };

        // Act - Link starts another transition before
        // clearing the previous shutter.
        controller.PrepareEntry(nextRoom, "east");

        var origin = DungeonRoomGeometry.GetRoomOrigin(
            firstRoom.Position
        );

        var result = controller.TryCloseAfterEntry(
            origin.X + 40,
            origin.Y + 88
        );

        // Assert - no stale closure occurs.
        Assert.False(result);
        Assert.True(state.IsDoorOpen(firstRoom.Id, shutter));
    }

    private static (RoomData Room, DoorData Door) CreateRoom(
        string direction,
        string type = "shutter"
    )
    {
        var door = new DoorData
        {
            Direction = direction,
            Destination = "start",
            Type = type
        };

        var room = new RoomData
        {
            Id = "destination",
            Position = new GridPosition
            {
                X = 1,
                Y = 2
            },
            Doors = [door]
        };

        return (room, door);
    }
}
