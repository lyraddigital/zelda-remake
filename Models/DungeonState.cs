
using System;
using System.Collections.Generic;
using ZeldaRemake.Models;

public sealed class DungeonState
{
    private readonly HashSet<string> _collectedObjects = new(
        StringComparer.OrdinalIgnoreCase
    );

    private readonly HashSet<string> _unlockedDoors = new(
        StringComparer.OrdinalIgnoreCase
    );

    private readonly HashSet<string> _openShutters = new(
        StringComparer.OrdinalIgnoreCase
    );

    public int Keys { get; private set; }

    public event Action? Changed;
    public event Action<string, string>? ShutterOpened;

    public bool IsObjectCollected(string roomId, string objectId)
    {
        return _collectedObjects.Contains($"{roomId}:{objectId}");
    }

    public bool TryCollectKey(string roomId, string objectId)
    {
        if (string.IsNullOrWhiteSpace(objectId))
        {
            throw new ArgumentException("Object ID is required.");
        }

        if (!_collectedObjects.Add($"{roomId}:{objectId}"))
        {
            return false;
        }

        Keys++;
        Changed?.Invoke();

        return true;
    }

    public bool IsDoorOpen(string roomId, DoorData door)
    {
        var doorId = GetDoorId(roomId, door);

        return door.Type.ToLowerInvariant() switch
        {
            "open" => true,
            "locked" => _unlockedDoors.Contains(doorId),
            "shutter" => _openShutters.Contains(doorId),
            _ => false
        };
    }

    public bool TryUnlockDoor(string roomId, DoorData door)
    {
        if (!string.Equals(
                door.Type,
                "locked",
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var doorId = GetDoorId(roomId, door);

        if (_unlockedDoors.Contains(doorId))
        {
            return true;
        }

        if (Keys == 0)
        {
            return false;
        }

        Keys--;
        _unlockedDoors.Add(doorId);

        Changed?.Invoke();

        return true;
    }


    public bool TryOpenShutter(string roomId, DoorData door)
    {
        if (!string.Equals(
            door.Type,
            "shutter",
            StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!_openShutters.Add(GetDoorId(roomId, door)))
        {
            return false;
        }

        ShutterOpened?.Invoke(roomId, door.Direction);

        return true;
    }


    public void CopyTo(DungeonSaveData save)
    {
        save.Keys = Keys;
        save.CollectedObjects = [.. _collectedObjects];
        save.UnlockedDoors = [.. _unlockedDoors];
    }

    public void Restore(DungeonSaveData save)
    {
        if (save.Keys < 0)
        {
            throw new ArgumentException(
                "Saved key count cannot be negative."
            );
        }

        _collectedObjects.Clear();
        _unlockedDoors.Clear();

        foreach (var id in save.CollectedObjects)
        {
            _collectedObjects.Add(id);
        }

        foreach (var id in save.UnlockedDoors)
        {
            _unlockedDoors.Add(id);
        }

        Keys = save.Keys;

        Changed?.Invoke();
    }


    private static string GetDoorId(string roomId, DoorData door)
    {
        return $"{roomId}:{door.Direction}";
    }
}
