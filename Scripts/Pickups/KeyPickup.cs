
using Godot;

public partial class KeyPickup : Area2D
{
    private DungeonState _state = null!;

    private string _roomId = string.Empty;
    private string _objectId = string.Empty;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    public void Configure(
        DungeonState state,
        string roomId,
        string objectId
    )
    {
        _state = state;
        _roomId = roomId;
        _objectId = objectId;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is not Player)
        {
            return;
        }

        if (!_state.TryCollectKey(_roomId, _objectId))
        {
            return;
        }

        GD.Print($"Key collected. Keys: {_state.Keys}");

        QueueFree();
    }
}
