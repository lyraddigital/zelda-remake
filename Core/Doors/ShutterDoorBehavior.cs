
using ZeldaRemake.Models;

namespace ZeldaRemake.Doors;

public sealed class ShutterDoorBehavior : IDoorBehavior
{
    public bool IsOpen(
        DungeonState state,
        string roomId,
        DoorData door
    )
    {
        return state.IsDoorOpen(roomId, door);
    }

    public bool TryOpen(
        DungeonState state,
        string roomId,
        DoorData door
    )
    {
        // Shutters are opened by room events,
        // not by the player's contact.
        return false;
    }
}
