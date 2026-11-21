
using Godot;

public partial class TreasurePickup : Area2D
{
    private DungeonState? _state;
    private string _locationId = string.Empty;
    private string _itemId = string.Empty;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    public override void _ExitTree()
    {
        BodyEntered -= OnBodyEntered;
    }

    public void Configure(
        DungeonState state,
        string locationId,
        string itemId
    )
    {
        _state = state;
        _locationId = locationId;
        _itemId = itemId;

        var alreadyCollected = state.HasItem(itemId);

        Visible = !alreadyCollected;

        SetDeferred(
            Area2D.PropertyName.Monitoring,
            !alreadyCollected
        );
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is not Player ||
            _state is null ||
            !Visible)
        {
            return;
        }

        if (!_state.TryCollectItem(
            _locationId,
            _itemId
        ))
        {
            return;
        }

        GD.Print($"Collected item: {_itemId}");

        Hide();

        SetDeferred(
            Area2D.PropertyName.Monitoring,
            false
        );
    }
}
