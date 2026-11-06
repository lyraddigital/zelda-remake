namespace ZeldaRemake.Models;

public sealed class DoorData
{
    public string Direction { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string Type { get; set; } = "open";
}

