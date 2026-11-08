
using System.Collections.Generic;

public sealed class DungeonSaveData
{
    public int Version { get; set; } = 1;

    public string DungeonId { get; set; } = string.Empty;

    public int Keys { get; set; }

    public List<string> CollectedObjects { get; set; } = [];

    public List<string> UnlockedDoors { get; set; } = [];

    public List<string> CompletedPuzzles { get; set; } = [];
}
