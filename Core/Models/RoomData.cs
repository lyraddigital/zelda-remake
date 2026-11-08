using System.Collections.Generic;

namespace ZeldaRemake.Models;

public sealed class RoomData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public GridPosition Position { get; set; } = new();

    public string DefaultFloor { get; set; } = "grass";
    public List<InteriorWallData> InteriorWalls { get; set; } = [];
    public List<FloorOverrideData> FloorOverrides { get; set; } = [];
    public List<DoorData> Doors { get; set; } = [];
    public List<RoomObjectData> Objects { get; set; } = [];
    //public List<RoomObjectData> Enemies { get; set; } = [];
}