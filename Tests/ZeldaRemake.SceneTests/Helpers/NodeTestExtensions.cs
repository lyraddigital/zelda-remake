
using Godot;

namespace ZeldaRemake.SceneTests.Helpers;

public static class NodeTestExtensions
{
    public static async Task WaitForPositionAsync(
        this Node2D node,
        Vector2 expectedPosition,
        ulong timeoutMilliseconds = 1000
    )
    {
        var startTime = Time.GetTicksMsec();

        while (!node.Position.IsEqualApprox(expectedPosition))
        {
            if (Time.GetTicksMsec() - startTime >= timeoutMilliseconds)
            {
                throw new TimeoutException(
                    $"Expected position {expectedPosition}, " +
                    $"but actual position was {node.Position}."
                );
            }

            await node.ToSignal(
                node.GetTree(),
                SceneTree.SignalName.PhysicsFrame
            );
        }
    }

    public static async Task WaitUntilAsync(
        this Node node,
        Func<bool> condition,
        string expectation,
        ulong timeoutMilliseconds = 3000
    )
    {
        var startTime = Time.GetTicksMsec();

        while (!condition())
        {
            if (Time.GetTicksMsec() - startTime >= timeoutMilliseconds)
            {
                throw new TimeoutException(
                    $"Timed out waiting for {expectation}."
                );
            }

            await node.ToSignal(
                node.GetTree(),
                SceneTree.SignalName.PhysicsFrame
            );
        }
    }

}
