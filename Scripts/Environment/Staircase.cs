
using Godot;
using System;

public partial class Staircase : Area2D
{
    public string? DestinationRoomId { get; private set; }

    public event Action<string>? PlayerEntered;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        UpdateMonitoring();
    }

    public override void _ExitTree()
    {
        BodyEntered -= OnBodyEntered;
    }

    public void Configure(string? destinationRoomId)
    {
        DestinationRoomId = destinationRoomId;
        UpdateMonitoring();
    }

    public void Reveal()
    {
        Show();
        UpdateMonitoring();
    }

    public void Conceal()
    {
        Hide();
        UpdateMonitoring();
    }

    private void UpdateMonitoring()
    {
        // Hidden or unconfigured stairs cannot trigger entry.
        SetDeferred(
            Area2D.PropertyName.Monitoring,
            Visible && !string.IsNullOrWhiteSpace(DestinationRoomId)
        );
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is not Player ||
            !Visible ||
            string.IsNullOrWhiteSpace(DestinationRoomId))
        {
            return;
        }

        PlayerEntered?.Invoke(DestinationRoomId);
    }
}
