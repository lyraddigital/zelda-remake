
using ZeldaRemake.Models;

namespace ZeldaRemake.Doors;

public sealed class StandardDoorBehavior : IDoorBehavior
{
    public bool IsOpen(
        DungeonState state,
        string roomId,
        DoorData door
    )
    {
        return true;
    }

    public bool TryOpen(
        DungeonState state,
        string roomId,
        DoorData door
    )
    {
        // Standard doors are always open.
        return false;
    }
}
