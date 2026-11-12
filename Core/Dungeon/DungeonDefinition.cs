
using System.Text.Json;
using ZeldaRemake.Models;

namespace ZeldaRemake.Dungeon;

public sealed class DungeonDefinition
{
    private readonly Dictionary<string, RoomData> _rooms = new();

    public DungeonData Data { get; }
    public RoomData StartingRoom { get; }

    private DungeonDefinition(DungeonData data)
    {
        Data = data;

        foreach (var room in data.Rooms)
        {
            if (!_rooms.TryAdd(room.Id, room))
            {
                throw new InvalidOperationException(
                    $"Duplicate room ID: {room.Id}"
                );
            }
        }

        if (!_rooms.TryGetValue(
                data.StartingRoom,
                out var startingRoom
            ))
        {
            throw new InvalidOperationException(
                $"Starting room not found: {data.StartingRoom}"
            );
        }

        StartingRoom = startingRoom;
    }

    public static DungeonDefinition Parse(string json)
    {
        var data = JsonSerializer.Deserialize<DungeonData>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }
        ) ?? throw new InvalidOperationException(
            "Dungeon configuration is invalid."
        );

        return new DungeonDefinition(data);
    }

    public RoomData? FindRoom(string roomId)
    {
        return _rooms.TryGetValue(roomId, out var room)
            ? room
            : null;
    }

    public CellarData? FindCellar(string cellarId)
    {
        return Data.Cellars.Find(cellar =>
            string.Equals(
                cellar.Id,
                cellarId,
                StringComparison.OrdinalIgnoreCase
            )
        );
    }

}
