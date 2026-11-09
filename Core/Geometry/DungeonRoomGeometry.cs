
using ZeldaRemake.Models;

namespace ZeldaRemake.Geometry;

public static class DungeonRoomGeometry
{
    public const float RoomWidth = 256.0f;
    public const float RoomHeight = 176.0f;
    public const float HudHeight = 64.0f;
    public const float EntryOffset = 24.0f;

    private const float CameraOffsetY = 56.0f;

    public static (float X, float Y) GetRoomOrigin(
        GridPosition position
    )
    {
        return (
            position.X * RoomWidth,
            position.Y * RoomHeight + HudHeight
        );
    }

    public static (float X, float Y) GetCameraPosition(
        GridPosition position
    )
    {
        var origin = GetRoomOrigin(position);

        return (
            origin.X + RoomWidth / 2.0f,
            origin.Y + CameraOffsetY
        );
    }

    public static (float X, float Y) GetEntryPosition(
        GridPosition destination,
        string direction,
        float playerX,
        float playerY
    )
    {
        var origin = GetRoomOrigin(destination);

        return direction.ToLowerInvariant() switch
        {
            "north" => (
                playerX,
                origin.Y + RoomHeight - EntryOffset
            ),

            "south" => (
                playerX,
                origin.Y + EntryOffset
            ),

            "east" => (
                origin.X + EntryOffset,
                playerY
            ),

            "west" => (
                origin.X + RoomWidth - EntryOffset,
                playerY
            ),

            _ => throw new InvalidOperationException(
                $"Invalid transition direction: {direction}"
            )
        };
    }

    public static bool IsAdjacent(
        GridPosition current,
        GridPosition destination,
        string direction
    )
    {
        var offset = GetDirectionOffset(direction);

        return destination.X == current.X + offset.X &&
               destination.Y == current.Y + offset.Y;
    }

    public static string OppositeDirection(string direction)
    {
        return direction.ToLowerInvariant() switch
        {
            "north" => "south",
            "south" => "north",
            "east" => "west",
            "west" => "east",

            _ => throw new InvalidOperationException(
                $"Invalid direction: {direction}"
            )
        };
    }

    private static (int X, int Y) GetDirectionOffset(
        string direction
    )
    {
        return direction.ToLowerInvariant() switch
        {
            "north" => (0, -1),
            "south" => (0, 1),
            "east" => (1, 0),
            "west" => (-1, 0),

            _ => throw new InvalidOperationException(
                $"Invalid direction: {direction}"
            )
        };
    }
}
