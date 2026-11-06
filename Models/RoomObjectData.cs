
using System.Collections.Generic;
using System.Text.Json;

public sealed class RoomObjectData
{
    public string Id { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public int X { get; set; }

    public int Y { get; set; }

    public Dictionary<string, JsonElement> Properties { get; set; } = [];
}
