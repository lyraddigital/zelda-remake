
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
        // Stone walls filling the playable area.
        DrawRect(
            new Rect2(0, 64, 256, 176),
            Stone
        );

        DrawBrickPattern();

        // Main underground passage.
        DrawRect(
            new Rect2(16, 176, 224, 24),
            Darkness
        );

        // Smaller raised room containing the Bow.
        DrawRect(
            new Rect2(144, 128, 96, 48),
            Darkness
        );

        // Raised floor, leaving an opening for the ladder.
        DrawRect(
            new Rect2(144, 176, 24, 16),
            Stone
        );

        DrawRect(
            new Rect2(184, 176, 56, 16),
            Stone
        );

        // Long entrance staircase on the left.
        DrawStaircase(
            new Rect2(24, 64, 16, 136),
            Stairs
        );

        // Short ladder into the treasure compartment.
        DrawStaircase(
            new Rect2(168, 164, 16, 36),
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
