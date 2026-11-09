
using ZeldaRemake.Dungeon;

namespace ZeldaRemake.UnitTests.Dungeon;

public class DungeonDefinitionTests
{
    private const string ValidJson = """
    {
        "id": "test_dungeon",
        "name": "Test Dungeon",
        "startingRoom": "entrance",
        "rooms": [
            {
                "id": "entrance",
                "position": { "x": 0, "y": 0 },
                "doors": [
                    {
                        "direction": "east",
                        "destination": "hall",
                        "type": "open"
                    }
                ]
            },
            {
                "id": "hall",
                "position": { "x": 1, "y": 0 },
                "doors": [
                    {
                        "direction": "west",
                        "destination": "entrance",
                        "type": "open"
                    }
                ]
            }
        ]
    }
    """;

    [Fact]
    public void Parse_WhenValid_LoadsDungeonAndStartingRoom()
    {
        // Act
        var definition = DungeonDefinition.Parse(ValidJson);

        // Assert
        Assert.Equal("test_dungeon", definition.Data.Id);
        Assert.Equal("entrance", definition.StartingRoom.Id);
        Assert.Equal(2, definition.Data.Rooms.Count);
    }

    [Fact]
    public void FindRoom_WhenRoomExists_ReturnsRoom()
    {
        // Arrange
        var definition = DungeonDefinition.Parse(ValidJson);

        // Act
        var room = definition.FindRoom("hall");

        // Assert
        Assert.NotNull(room);
        Assert.Equal(1, room.Position.X);
        Assert.Equal(0, room.Position.Y);
    }

    [Fact]
    public void FindRoom_WhenRoomDoesNotExist_ReturnsNull()
    {
        // Arrange
        var definition = DungeonDefinition.Parse(ValidJson);

        // Act
        var room = definition.FindRoom("missing");

        // Assert
        Assert.Null(room);
    }

    [Fact]
    public void Parse_WhenRoomIdsAreDuplicated_ThrowsException()
    {
        // Arrange
        const string json = """
        {
            "id": "test",
            "startingRoom": "entrance",
            "rooms": [
                { "id": "entrance" },
                { "id": "entrance" }
            ]
        }
        """;

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => DungeonDefinition.Parse(json)
        );

        // Assert
        Assert.Contains(
            "Duplicate room ID",
            exception.Message
        );
    }

    [Fact]
    public void Parse_WhenStartingRoomDoesNotExist_ThrowsException()
    {
        // Arrange
        const string json = """
        {
            "id": "test",
            "startingRoom": "missing",
            "rooms": [
                { "id": "entrance" }
            ]
        }
        """;

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => DungeonDefinition.Parse(json)
        );

        // Assert
        Assert.Contains(
            "Starting room not found",
            exception.Message
        );
    }

    [Fact]
    public void Parse_WhenJsonPropertyNamesHaveDifferentCasing_LoadsDungeon()
    {
        // Arrange
        const string json = """
        {
            "ID": "test",
            "StartingRoom": "entrance",
            "Rooms": [
                { "ID": "entrance" }
            ]
        }
        """;

        // Act
        var definition = DungeonDefinition.Parse(json);

        // Assert
        Assert.Equal("test", definition.Data.Id);
        Assert.Equal("entrance", definition.StartingRoom.Id);
    }

    [Fact]
    public void Parse_WhenConfigurationIsNull_ThrowsException()
    {
        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => DungeonDefinition.Parse("null")
        );

        // Assert
        Assert.Equal(
            "Dungeon configuration is invalid.",
            exception.Message
        );
    }
}
