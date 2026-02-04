using Laby.Core.Mapping;
using Laby.Core.Tiles;
using Laby.Core.Build;
using Laby.Core.Items;
using Laby.Algorithms;
using Laby.Mapping;

namespace Laby.Tests.Mapping;

public class SharedLabyrinthMapTest
{
    private sealed class ScriptedExplorerStrategy(params ExplorerAction[] actions) : IExplorerStrategy
    {
        private readonly Queue<ExplorerAction> _actions = new(actions);

        public ExplorerAction NextAction(ExplorerContext context) =>
            _actions.TryDequeue(out var action)
                ? action
                : ExplorerAction.TurnLeft;
    }

    [Test]
    public void GetTileTypeReturnsUnknownForUnseenTile()
    {
        var test = new SharedLabyrinthMap();

        Assert.That(test.GetTileType(42, -12), Is.EqualTo(typeof(Unknown)));
    }

    [Test]
    public void ObserveStoresTileType()
    {
        var test = new SharedLabyrinthMap();

        test.Observe(2, 3, typeof(Room));

        Assert.That(test.GetTileType(2, 3), Is.EqualTo(typeof(Room)));
    }

    [Test]
    public void ObserveDifferentTypeOnSamePositionThrows()
    {
        var test = new SharedLabyrinthMap();

        test.Observe(1, 1, typeof(Room));

        Assert.That(
            () => test.Observe(1, 1, typeof(Wall)),
            Throws.TypeOf<InvalidOperationException>()
        );
    }

    [Test]
    public void ObserveKnownTypeAfterUnknownReplacesUnknown()
    {
        var test = new SharedLabyrinthMap();

        test.Observe(5, 7, typeof(Unknown));
        test.Observe(5, 7, typeof(Room));

        Assert.That(test.GetTileType(5, 7), Is.EqualTo(typeof(Room)));
    }

    [Test]
    public void ObserveUnknownAfterKnownKeepsKnown()
    {
        var test = new SharedLabyrinthMap();

        test.Observe(5, 7, typeof(Room));
        test.Observe(5, 7, typeof(Unknown));

        Assert.That(test.GetTileType(5, 7), Is.EqualTo(typeof(Room)));
    }

    [Test]
    public void ObserveIsThreadSafe()
    {
        const int sampleSize = 2_000;
        var test = new SharedLabyrinthMap();

        Parallel.For(0, sampleSize, i =>
        {
            test.Observe(i, -i, typeof(Room));
        });

        var snapshot = test.Snapshot();
        using var all = Assert.EnterMultipleScope();
        Assert.That(snapshot.Count, Is.EqualTo(sampleSize));
        Assert.That(snapshot[new MapPosition(17, -17)], Is.EqualTo(typeof(Room)));
        Assert.That(snapshot[new MapPosition(sampleSize - 1, -(sampleSize - 1))], Is.EqualTo(typeof(Room)));
    }

    [Test]
    public async Task ThreeExplorersShareMapAndOneFindsTheExit()
    {
        var labyrinth = new Laby.Core.Labyrinth(new AsciiParser("""
            +------+
            |x     |
            | +--+ |
              |    |
            | +--+ |
            |      |
            +------+
            """));
        var map = new SharedLabyrinthMap();
        var strategies = ExplorerTeamStrategyFactory.CreateDefault();
        var explorers = strategies
            .Select(strategy => new Explorer(labyrinth.NewCrawler(), strategy, map))
            .ToArray();

        var remainingMoves = await Task.WhenAll(
            explorers.Select(explorer => Task.Run(() => explorer.GetOut(250)))
        );
        var snapshot = map.Snapshot();

        using var all = Assert.EnterMultipleScope();
        Assert.That(remainingMoves.Any(moves => moves > 0), Is.True, "At least one explorer should detect the outside.");
        Assert.That(snapshot.Values, Does.Contain(typeof(Outside)));
        Assert.That(snapshot.Count, Is.GreaterThan(20));
    }

    [Test]
    public async Task OpenedDoorIsPassableForOtherExplorersOnSameLabyrinth()
    {
        var labyrinth = new Laby.Core.Labyrinth(new AsciiParser("""
            +/-+
            |k |
            |x |
            +--+
            """));
        var map = new SharedLabyrinthMap();
        var first = new Explorer(
            labyrinth.NewCrawler(),
            new ScriptedExplorerStrategy(ExplorerAction.Walk, ExplorerAction.Walk),
            map
        );
        var second = new Explorer(
            labyrinth.NewCrawler(),
            new ScriptedExplorerStrategy(ExplorerAction.Walk, ExplorerAction.Walk),
            map
        );

        await first.GetOut(2, new MyInventory());
        await second.GetOut(2, new MyInventory());

        Assert.That(
            (second.Crawler.X, second.Crawler.Y),
            Is.EqualTo((1, 0)),
            "An opened door must stay open for all explorers sharing the same labyrinth."
        );
    }

    [Test]
    public async Task ThreeExplorersEscapeFromDoorAndKeyMaze()
    {
        var labyrinth = new Laby.Core.Labyrinth(new AsciiParser("""
            +--+--------+
            |  /        |
            |  +--+--+  |
            |     |k    |
            +--+  |  +--+
               |k  x    |
            +  +-------/|
            |           |
            +-----------+
            """));
        var map = new SharedLabyrinthMap();
        var explorers = ExplorerTeamStrategyFactory.CreateDefault()
            .Select(strategy => new Explorer(labyrinth.NewCrawler(), strategy, map))
            .ToArray();

        var remainingMoves = await Task.WhenAll(
            explorers.Select(explorer => Task.Run(() => explorer.GetOut(2000)))
        );

        Assert.That(
            remainingMoves.Any(moves => moves > 0),
            Is.True,
            "At least one explorer should reach the outside in the provided key-and-door maze."
        );
    }
}
