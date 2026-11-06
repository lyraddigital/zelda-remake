
using Godot;
using System;
using System.IO;
using System.Text.Json;

public static class DungeonSaveService
{
    private const string SaveDirectory = "user://Saves";

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static void Save(DungeonSaveData save)
    {
        var result = DirAccess.MakeDirRecursiveAbsolute(
            SaveDirectory
        );

        if (result != Error.Ok)
        {
            throw new IOException(
                $"Could not create save directory: {result}"
            );
        }

        var path = GetSavePath(save.DungeonId);
        var json = JsonSerializer.Serialize(save, Options);

        using var file = Godot.FileAccess.Open(
            path,
            Godot.FileAccess.ModeFlags.Write
        ) ?? throw new IOException(
            $"Could not open save file: {path}"
        );

        file.StoreString(json);
        file.Flush();

        GD.Print($"Game saved: {path}");
    }

    public static DungeonSaveData? Load(string dungeonId)
    {
        var path = GetSavePath(dungeonId);

        if (!Godot.FileAccess.FileExists(path))
        {
            return null;
        }

        using var file = Godot.FileAccess.Open(
            path,
            Godot.FileAccess.ModeFlags.Read
        ) ?? throw new IOException(
            $"Could not open save file: {path}"
        );

        var save = JsonSerializer.Deserialize<DungeonSaveData>(
            file.GetAsText(),
            Options
        ) ?? throw new JsonException(
            $"Invalid save file: {path}"
        );

        if (save.Version != 1)
        {
            throw new InvalidOperationException(
                $"Unsupported save version: {save.Version}"
            );
        }

        if (save.DungeonId != dungeonId)
        {
            throw new InvalidOperationException(
                "Save file belongs to another dungeon."
            );
        }

        return save;
    }

    private static string GetSavePath(string dungeonId)
    {
        if (string.IsNullOrWhiteSpace(dungeonId) ||
            dungeonId.IndexOfAny(['/', '\\', ':']) >= 0)
        {
            throw new ArgumentException("Invalid dungeon ID.");
        }

        return $"{SaveDirectory}/{dungeonId}.json";
    }
}
