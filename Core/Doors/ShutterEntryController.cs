
using ZeldaRemake.Geometry;
using ZeldaRemake.Models;

namespace ZeldaRemake.Doors;

public sealed class ShutterEntryController
{
    private const float ClearanceDistance = 38.0f;

    private readonly DungeonState _state;

    private RoomData? _pendingRoom;
    private DoorData? _pendingShutter;

    public ShutterEntryController(DungeonState state)
    {
        _state = state;
    }

    public DoorData? PrepareEntry(
        RoomData destination,
        string travelDirection
    )
    {
        // A new transition cancels any previous
        // incoming-shutter closure.
        ClearPendingEntry();

        var entryDirection = DungeonRoomGeometry.OppositeDirection(
            travelDirection
        );

        var entryDoor = destination.Doors.Find(door =>
            string.Equals(
                door.Direction,
                entryDirection,
                StringComparison.OrdinalIgnoreCase
            )
        );

        if (entryDoor is null ||
            !string.Equals(
                entryDoor.Type,
                "shutter",
                StringComparison.OrdinalIgnoreCase
            ))
        {
            return null;
        }

        // Allow Link to pass through the destination shutter.
        if (!_state.IsDoorOpen(destination.Id, entryDoor))
        {
            _state.TryOpenShutter(destination.Id, entryDoor);
        }

        return entryDoor;
    }

    public void BeginEntry(
        RoomData destination,
        DoorData? shutter
    )
    {
        // Start tracking only after the room transition
        // finishes. The shutter must not close mid-transition.
        _pendingRoom = shutter is null
            ? null
            : destination;

        _pendingShutter = shutter;
    }

    public bool TryCloseAfterEntry(
        float playerWorldX,
        float playerWorldY
    )
    {
        if (_pendingRoom is null || _pendingShutter is null)
        {
            return false;
        }

        var origin = DungeonRoomGeometry.GetRoomOrigin(
            _pendingRoom.Position
        );

        var playerX = playerWorldX - origin.X;
        var playerY = playerWorldY - origin.Y;

        var hasClearedDoor = _pendingShutter.Direction
            .ToLowerInvariant() switch
        {
            "north" => playerY > ClearanceDistance,

            "south" => playerY <
                DungeonRoomGeometry.RoomHeight - ClearanceDistance,

            "west" => playerX > ClearanceDistance,

            "east" => playerX <
                DungeonRoomGeometry.RoomWidth - ClearanceDistance,

            _ => throw new InvalidOperationException(
                $"Invalid door direction: {_pendingShutter.Direction}"
            )
        };

        if (!hasClearedDoor)
        {
            return false;
        }

        _state.ResetShutters(
            _pendingRoom.Id,
            [_pendingShutter]
        );

        ClearPendingEntry();

        return true;
    }

    private void ClearPendingEntry()
    {
        _pendingRoom = null;
        _pendingShutter = null;
    }
}
