
using ZeldaRemake.Doors;

namespace ZeldaRemake.UnitTests.Doors;

public class DoorAlignmentTests
{
    [Fact]
    public void GetLateralVelocity_WhenPlayerIsCentered_ReturnsZero()
    {
        // Arrange
        const float player = 128.0f;
        const float door = 128.0f;

        // Act
        var velocity = DoorAlignment.GetLateralVelocity(
            player,
            door
        );

        // Assert
        Assert.Equal(0.0f, velocity);
    }

    [Fact]
    public void GetLateralVelocity_WhenPlayerIsLeftOfDoor_ReturnsPositiveVelocity()
    {
        // Arrange
        const float player = 124.0f;
        const float door = 128.0f;

        // Act
        var velocity = DoorAlignment.GetLateralVelocity(
            player,
            door
        );

        // Assert
        Assert.Equal(120.0f, velocity);
    }

    [Fact]
    public void GetLateralVelocity_WhenPlayerIsRightOfDoor_ReturnsNegativeVelocity()
    {
        // Arrange
        const float player = 132.0f;
        const float door = 128.0f;

        // Act
        var velocity = DoorAlignment.GetLateralVelocity(
            player,
            door
        );

        // Assert
        Assert.Equal(-120.0f, velocity);
    }

    [Fact]
    public void GetLateralVelocity_WhenOutsideTolerance_ReturnsZero()
    {
        // Arrange
        const float player = 100.0f;
        const float door = 128.0f;

        // Act
        var velocity = DoorAlignment.GetLateralVelocity(
            player,
            door
        );

        // Assert
        Assert.Equal(0.0f, velocity);
    }

    [Fact]
    public void GetLateralVelocity_WhenOutsideAlignmentTolerance_ReturnsZero()
    {
        // Arrange
        const float player = 112.0f;
        const float door = 128.0f;

        // Act
        var velocity = DoorAlignment.GetLateralVelocity(
            player,
            door
        );

        // Assert
        Assert.Equal(0.0f, velocity);
    }

    [Fact]
    public void GetLateralVelocity_WhenInsideDeadZone_ReturnsZero()
    {
        // Arrange
        const float player = 127.9f;
        const float door = 128.0f;

        // Act
        var velocity = DoorAlignment.GetLateralVelocity(
            player,
            door
        );

        // Assert
        Assert.Equal(0.0f, velocity);
    }

    [Fact]
    public void GetLateralVelocity_WhenAtMaximumTolerance_CapsSpeed()
    {
        // Arrange
        const float player = 123.0f;
        const float door = 128.0f;

        // Act
        var velocity = DoorAlignment.GetLateralVelocity(
            player,
            door
        );

        // Assert
        Assert.Equal(120.0f, velocity);
    }

    [Fact]
    public void GetLateralVelocity_WhenSevenPixelsOffCentre_CorrectsPosition()
    {
        // Arrange
        const float player = 121.0f;
        const float door = 128.0f;

        // Act
        var velocity = DoorAlignment.GetLateralVelocity(
            player,
            door
        );

        // Assert
        Assert.Equal(120.0f, velocity);
    }

    [Fact]
    public void GetLateralVelocity_WhenEightPixelsOffCentre_DoesNotCorrect()
    {
        // Arrange
        const float player = 120.0f;
        const float door = 128.0f;

        // Act
        var velocity = DoorAlignment.GetLateralVelocity(
            player,
            door
        );

        // Assert
        Assert.Equal(0.0f, velocity);
    }

}
