using System.Collections.Generic;
using ZeldaRemake.Models;

public static class DoorwayGeometry
{
    public static bool IsOpening(
        IReadOnlyList<DoorData> doors,
        int x,
        int y
    )
    {
        foreach (var door in doors)
        {
            var isOpening = door.Direction.ToLowerInvariant() switch
            {
                "north" => y <= 1 && x >= 7 && x <= 8,
                "south" => y >= 9 && x >= 7 && x <= 8,
                "west" => x <= 1 && y == 5,
                "east" => x >= 14 && y == 5,
                _ => false
            };

            if (isOpening)
            {
                return true;
            }
        }

        return false;
    }
}
