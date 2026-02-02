using Laby.Algorithms;
using Laby.Core.Build;
using Laby.Core.Mapping;
using Laby.Core.Tiles;
using Laby.Mapping;

namespace Laby.Tests;

public class ExplorerSharedMapTest
{
    private sealed class ScriptedExplorerStrategy(params ExplorerAction[] actions) : IExplorerStrategy
    {
        private readonly Queue<ExplorerAction> _actions = new(actions);

        public ExplorerAction NextAction(ExplorerContext context) =>
            _actions.TryDequeue(out var action)
                ? action
                : ExplorerAction.TurnLeft;
    }

    private static Explorer NewExplorerFor(string labyrinth, ILabyrinthMap map, params ExplorerAction[] actions) =>
        new(
            new Laby.Core.Labyrinth(new AsciiParser(labyrinth)).NewCrawler(),
            new ScriptedExplorerStrategy(actions),
            map
        );

    [Test]
    public async Task GetOutUpdatesSharedMapWithSeenTiles()
    {
        var map = new SharedLabyrinthMap();
        var test = NewExplorerFor("""
            +--+
            |x |
            +--+
            """,
            map,
            ExplorerAction.TurnLeft
        );

        await test.GetOut(1);

        using var all = Assert.EnterMultipleScope();
        Assert.That(map.GetTileType(1, 1), Is.EqualTo(typeof(Room)));
        Assert.That(map.GetTileType(1, 0), Is.EqualTo(typeof(Wall)));
    }

    [Test]
    public async Task GetOutFromTwoExplorersMergesObservationsIntoOneMap()
    {
        const string labyrinth = """
            +---+
            |   |
            |x  |
            +---+
            """;
        var map = new SharedLabyrinthMap();
        var first = NewExplorerFor(
            labyrinth,
            map,
            ExplorerAction.Walk
        );
        var second = NewExplorerFor(
            labyrinth,
            map,
            ExplorerAction.TurnLeft,
            ExplorerAction.TurnLeft
        );

        await Task.WhenAll(
            first.GetOut(1),
            second.GetOut(2)
        );

        using var all = Assert.EnterMultipleScope();
        Assert.That(map.GetTileType(1, 2), Is.EqualTo(typeof(Room)));
        Assert.That(map.GetTileType(1, 1), Is.EqualTo(typeof(Room)));
        Assert.That(map.GetTileType(0, 2), Is.EqualTo(typeof(Wall)));
    }
}
