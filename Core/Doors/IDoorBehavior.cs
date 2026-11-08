
using ZeldaRemake.Models;

namespace ZeldaRemake.Doors;

public interface IDoorBehavior
{
    bool IsOpen(
        DungeonState state,
        string roomId,
        DoorData door
    );

    bool TryOpen(
        DungeonState state,
        string roomId,
        DoorData door
    );
}
