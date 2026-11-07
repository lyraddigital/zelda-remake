
using Godot;

public partial class Staircase : Node2D
{
    private DungeonState _state = null!;
    private string _roomId = string.Empty;
    private string _staircaseId = string.Empty;

    public void Configure(
        DungeonState state,
        string roomId,
        string staircaseId)
    {
        _state = state;
        _roomId = roomId;
        _staircaseId = staircaseId;

        _state.Changed += UpdateVisibility;

        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        Visible = _state.IsPuzzleCompleted(
            _roomId,
            _staircaseId
        );
    }

    public override void _ExitTree()
    {
        if (_state is not null)
        {
            _state.Changed -= UpdateVisibility;
        }
    }
}
