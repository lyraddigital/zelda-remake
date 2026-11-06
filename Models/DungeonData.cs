using System.Collections.Generic;

namespace ZeldaRemake.Models;

public sealed class DungeonData
{
    public int Version { get; set; } = 1;
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string StartingRoom { get; set; } = string.Empty;

    public List<RoomData> Rooms { get; set; } = [];
}