
using ZeldaRemake.Models;

namespace ZeldaRemake.SceneTests.Features.Rooms.Fixtures;

public static class RoomDataFactory
{
    public static RoomData Create()
    {
        return new RoomData
        {
            Id = "test_room",
            Name = "Test Room",
            DefaultFloor = "grass",
            Walls = Enumerable.Repeat(
                new string('.', 16),
                11
            ).ToArray()
        };
    }
}
