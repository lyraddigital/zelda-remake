
using Godot;

public partial class ItemCellarBackdrop : Control
{
    private static readonly Color Stone = new("#929494");
    private static readonly Color Grout = new("#3B4044");
    private static readonly Color Darkness = new("#08080E");
    private static readonly Color Stairs = new("#D3D4D0");
    private static readonly Color Ladder = new("#B58B42");

    public override void _Ready()
    {
        Size = new Vector2(256, 240);
        MouseFilter = MouseFilterEnum.Ignore;
        QueueRedraw();
    }

    public override void _Draw()
    {
        // Solid stone structure beneath the HUD.
        DrawRect(
            new Rect2(0, 64, 256, 176),
            Stone
        );

        DrawBrickPattern();

        // Lower passage: 48px high, ending at the
        // staircase into the treasure compartment.
        DrawRect(
            new Rect2(16, 176, 144, 48),
            Darkness
        );

        // Raised Bow compartment, cut into the stone.
        DrawRect(
            new Rect2(128, 116, 112, 60),
            Darkness
        );

        // The compartment floor has an opening
        // for the ladder at X=144..160.
        DrawRect(
            new Rect2(128, 176, 16, 16),
            Stone
        );

        DrawRect(
            new Rect2(160, 176, 80, 16),
            Stone
        );

        // Long staircase connecting to the dungeon.
        DrawStaircase(
            new Rect2(24, 64, 16, 160),
            Stairs
        );

        // Treasure ladder spans the lower passage,
        // the platform opening, and the upper chamber.
        // Draw it last so it remains visible over
        // the platform edge.
        DrawStaircase(
            new Rect2(144, 148, 16, 76),
            Ladder
        );
    }

    private void DrawBrickPattern()
    {
        for (var y = 64; y < 240; y += 8)
        {
            DrawLine(
                new Vector2(0, y),
                new Vector2(256, y),
                Grout,
                1
            );

            var row = (y - 64) / 8;
            var offset = row % 2 == 0 ? 0 : 8;

            for (var x = offset; x < 256; x += 16)
            {
                DrawLine(
                    new Vector2(x, y),
                    new Vector2(x, y + 8),
                    Grout,
                    1
                );
            }
        }
    }

    private void DrawStaircase(Rect2 area, Color color)
    {
        DrawRect(area, color);

        for (
            var y = area.Position.Y + 4;
            y < area.End.Y;
            y += 8
        )
        {
            DrawLine(
                new Vector2(area.Position.X + 2, y),
                new Vector2(area.End.X - 2, y),
                Grout,
                2
            );
        }
    }
}
