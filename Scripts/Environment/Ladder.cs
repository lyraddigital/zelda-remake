
using Godot;
using System;

public enum LadderExitDirection
{
    Top,
    Bottom,
    Side
}

public partial class Ladder : Area2D
{
    [Export]
    public bool ExitsSceneAtTop { get; set; }

    public event Action<Player, LadderExitDirection>? PlayerExited;

    public Rect2 Bounds
    {
        get
        {
            var collision = GetNode<CollisionShape2D>(
                "CollisionShape2D"
            );

            var rectangle =
                collision.Shape as RectangleShape2D
                ?? throw new InvalidOperationException(
                    $"{Name} requires a RectangleShape2D."
                );

            return new Rect2(
                collision.GlobalPosition -
                    rectangle.Size / 2.0f,
                rectangle.Size
            );
        }
    }

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    public override void _ExitTree()
    {
        BodyEntered -= OnBodyEntered;
        BodyExited -= OnBodyExited;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Player player)
        {
            player.EnterLadder(this);
        }
    }

    private void OnBodyExited(Node2D body)
    {
        if (body is Player player)
        {
            player.LeaveLadder(this);
        }
    }

    public void NotifyPlayerExited(
        Player player,
        LadderExitDirection direction
    )
    {
        PlayerExited?.Invoke(player, direction);
    }
}
