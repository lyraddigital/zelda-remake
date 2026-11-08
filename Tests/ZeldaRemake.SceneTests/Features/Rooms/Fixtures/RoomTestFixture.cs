
using GdUnit4;

namespace ZeldaRemake.SceneTests.Features.Rooms.Fixtures;

public static class RoomTestFixture
{
    private const string RoomScenePath = "res://Scenes/Room.tscn";

    public static (ISceneRunner Runner, Room Room) Load()
    {
        var runner = ISceneRunner.Load(RoomScenePath);

        var room = runner.Scene() as Room
            ?? throw new InvalidOperationException(
                "Failed to load Room scene."
            );

        return (runner, room);
    }
}
