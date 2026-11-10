
using GdUnit4;
using Godot;
using System.Text.Json;
using ZeldaRemake.SceneTests.Features.Rooms.Fixtures;
using ZeldaRemake.SceneTests.Helpers;
using static GdUnit4.Assertions;

namespace ZeldaRemake.SceneTests.Features.Staircases;

[TestSuite]
[RequireGodotRuntime]
public class StaircaseSceneTests
{
    [TestCase]
    public async Task PlayerEnteringVisibleStaircase_RaisesDestinationEvent()
    {
        // Arrange
        var (runner, room) = RoomTestFixture.Load();

        var roomData = RoomDataFactory.Create();
        roomData.Objects.Add(CreateStaircase("basement_01"));

        string? sourceRoom = null;
        string? staircaseId = null;
        string? destination = null;

        room.StaircaseEntered += (source, id, target) =>
        {
            sourceRoom = source;
            staircaseId = id;
            destination = target;
        };

        room.LoadLayout(roomData, new DungeonState());

        var staircase = room.GetNode<Node2D>("Objects")
            .GetChild<Staircase>(0);

        var player = CreatePlayer(
            room,
            staircase.Position + new Vector2(32, 0)
        );

        await runner.SimulateFrames(3);

        // Act
        player.Position = staircase.Position;

        await room.WaitUntilAsync(
            () => destination is not null,
            "staircase entry event"
        );

        // Assert
        AssertBool(sourceRoom == roomData.Id).IsTrue();
        AssertBool(staircaseId == "stairs_01").IsTrue();
        AssertBool(destination == "basement_01").IsTrue();
    }

    [TestCase]
    public async Task PlayerEnteringHiddenStaircase_DoesNotTriggerUntilRevealed()
    {
        // Arrange
        var (runner, room) = RoomTestFixture.Load();

        var roomData = RoomDataFactory.Create();

        roomData.Objects.Add(
            CreateStaircase("basement_01")
        );

        roomData.Objects.Add(new RoomObjectData
        {
            Id = "block_01",
            Type = "pushable_block",
            X = 6,
            Y = 5,
            Properties = new()
            {
                ["pushDirection"] =
                    JsonSerializer.SerializeToElement("north"),

                ["revealsStaircase"] =
                    JsonSerializer.SerializeToElement("stairs_01")
            }
        });

        var entryCount = 0;

        room.StaircaseEntered += (_, _, _) => entryCount++;

        room.LoadLayout(roomData, new DungeonState());

        var staircase = room.GetNode<Node2D>("Objects")
            .GetChild<Staircase>(0);

        var player = CreatePlayer(
            room,
            staircase.Position + new Vector2(32, 0)
        );

        await runner.SimulateFrames(3);

        AssertBool(staircase.Visible).IsFalse();

        // Act - step onto the concealed staircase.
        player.Position = staircase.Position;

        await runner.SimulateFrames(3);

        // Assert - nothing happens while hidden.
        AssertBool(entryCount == 0).IsTrue();

        // Move away before revealing the staircase.
        player.Position = staircase.Position + new Vector2(32, 0);

        await runner.SimulateFrames(3);

        staircase.Reveal();

        await runner.SimulateFrames(3);

        player.Position = staircase.Position;

        await room.WaitUntilAsync(
            () => entryCount == 1,
            "revealed staircase entry event"
        );

        AssertBool(entryCount == 1).IsTrue();
    }

    [TestCase]
    public async Task PlayerEnteringStaircaseWithoutDestination_DoesNotTrigger()
    {
        // Arrange
        var (runner, room) = RoomTestFixture.Load();

        var roomData = RoomDataFactory.Create();
        roomData.Objects.Add(CreateStaircase(null));

        var entryCount = 0;

        room.StaircaseEntered += (_, _, _) => entryCount++;

        room.LoadLayout(roomData, new DungeonState());

        var staircase = room.GetNode<Node2D>("Objects")
            .GetChild<Staircase>(0);

        var player = CreatePlayer(
            room,
            staircase.Position + new Vector2(32, 0)
        );

        await runner.SimulateFrames(3);

        // Act
        player.Position = staircase.Position;

        await runner.SimulateFrames(4);

        // Assert
        AssertBool(staircase.Visible).IsTrue();
        AssertBool(entryCount == 0).IsTrue();
    }

    private static RoomObjectData CreateStaircase(
        string? destinationRoom
    )
    {
        var staircase = new RoomObjectData
        {
            Id = "stairs_01",
            Type = "staircase",
            X = 7,
            Y = 5
        };

        if (destinationRoom is not null)
        {
            staircase.Properties["destinationRoom"] =
                JsonSerializer.SerializeToElement(destinationRoom);
        }

        return staircase;
    }

    private static Player CreatePlayer(
        Room room,
        Vector2 position
    )
    {
        var scene = ResourceLoader.Load<PackedScene>(
            "res://Scenes/Player.tscn"
        );

        var player = scene.Instantiate<Player>();

        player.Position = position;
        room.AddChild(player);
        player.SetPhysicsProcess(false);

        return player;
    }
}
