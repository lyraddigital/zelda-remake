
using ZeldaRemake.Geometry;
using ZeldaRemake.Models;

namespace ZeldaRemake.UnitTests.Geometry;

public class DungeonRoomGeometryTests
{
    [Theory]
    [InlineData(0, 0, 0, 64, 128, 120)]
    [InlineData(1, 0, 256, 64, 384, 120)]
    [InlineData(0, 1, 0, 240, 128, 296)]
    [InlineData(-1, -1, -256, -112, -128, -56)]
    public void GetRoomCoordinates_ReturnsExpectedWorldPositions(
        int gridX,
        int gridY,
        int expectedOriginX,
        int expectedOriginY,
        int expectedCameraX,
        int expectedCameraY
    )
    {
        var position = new GridPosition
        {
            X = gridX,
            Y = gridY
        };

        var origin = DungeonRoomGeometry.GetRoomOrigin(position);
        var camera = DungeonRoomGeometry.GetCameraPosition(position);

        Assert.Equal((float)expectedOriginX, origin.X);
        Assert.Equal((float)expectedOriginY, origin.Y);
        Assert.Equal((float)expectedCameraX, camera.X);
        Assert.Equal((float)expectedCameraY, camera.Y);
    }

    [Theory]
    [InlineData("north", 3, 3)]
    [InlineData("south", 3, 5)]
    [InlineData("east", 4, 4)]
    [InlineData("west", 2, 4)]
    public void IsAdjacent_WhenRoomIsInCorrectDirection_ReturnsTrue(
        string direction,
        int destinationX,
        int destinationY
    )
    {
        var current = new GridPosition { X = 3, Y = 4 };

        var destination = new GridPosition
        {
            X = destinationX,
            Y = destinationY
        };

        Assert.True(
            DungeonRoomGeometry.IsAdjacent(
                current,
                destination,
                direction
            )
        );
    }

    [Theory]
    [InlineData("north", 4, 4)]
    [InlineData("south", 3, 6)]
    [InlineData("east", 2, 4)]
    [InlineData("west", 3, 4)]
    public void IsAdjacent_WhenRoomIsNotAdjacent_ReturnsFalse(
        string direction,
        int destinationX,
        int destinationY
    )
    {
        var current = new GridPosition { X = 3, Y = 4 };

        var destination = new GridPosition
        {
            X = destinationX,
            Y = destinationY
        };

        Assert.False(
            DungeonRoomGeometry.IsAdjacent(
                current,
                destination,
                direction
            )
        );
    }

    [Theory]
    [InlineData("north", 128, 216)]
    [InlineData("south", 128, 88)]
    [InlineData("east", 280, 152)]
    [InlineData("west", 488, 152)]
    public void GetEntryPosition_PreservesPerpendicularCoordinate(
        string direction,
        int expectedX,
        int expectedY
    )
    {
        var destination = new GridPosition { X = 1, Y = 0 };

        var position = DungeonRoomGeometry.GetEntryPosition(
            destination,
            direction,
            128.0f,
            152.0f
        );

        Assert.Equal((float)expectedX, position.X);
        Assert.Equal((float)expectedY, position.Y);
    }

    [Theory]
    [InlineData("north", "south")]
    [InlineData("south", "north")]
    [InlineData("east", "west")]
    [InlineData("west", "east")]
    public void OppositeDirection_ReturnsCorrectDirection(
        string direction,
        string expected
    )
    {
        Assert.Equal(
            expected,
            DungeonRoomGeometry.OppositeDirection(direction)
        );
    }
}
