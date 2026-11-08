namespace ZeldaRemake.Doors;

public static class DoorAlignment
{
    private const float Response = 32.0f;
    private const float DeadZone = 0.2f;

    public static float GetLateralVelocity(
        float playerCoordinate,
        float doorCoordinate,
        float tolerance = 5.0f,
        float maximumSpeed = 120.0f
    )
    {
        if (tolerance < 0 || maximumSpeed < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tolerance),
                "Alignment values cannot be negative."
            );
        }

        var difference = doorCoordinate - playerCoordinate;
        var distance = MathF.Abs(difference);

        if (distance > tolerance || distance <= DeadZone)
        {
            return 0.0f;
        }

        return Math.Clamp(
            difference * Response,
            -maximumSpeed,
            maximumSpeed
        );
    }
}