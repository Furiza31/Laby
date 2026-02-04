using Laby.Algorithms;
using Laby.Core.Crawl;
using Laby.Core.Items;
using Laby.Core.Tiles;
using Laby.Mapping;
using Moq;

namespace Laby.Tests;

public class StrategySwitcherExplorerStrategyTest
{
    [Test]
    public void NextActionUsesOutsideStrategyWhenOutsideIsKnown()
    {
        var map = new SharedLabyrinthMap();
        map.Observe(1, 1, typeof(Room));
        map.Observe(2, 1, typeof(Room));
        map.Observe(3, 1, typeof(Room));
        map.Observe(4, 1, typeof(Outside));

        var crawler = NewCrawlerAt(1, 1, Direction.East);
        var strategy = NewAdaptiveStrategy();

        var action = strategy.NextAction(new ExplorerContext(
            crawler.Object,
            typeof(Room),
            new MyInventory(),
            map
        ));

        using var all = Assert.EnterMultipleScope();
        Assert.That(action, Is.EqualTo(ExplorerAction.Walk));
        Assert.That(strategy.CurrentStrategyName, Is.EqualTo(nameof(OutsideDijkstraStrategy)));
    }

    [Test]
    public void NextActionUsesFrontierStrategyWhenNoOutsideWasSeen()
    {
        var map = new SharedLabyrinthMap();
        map.Observe(1, 1, typeof(Room));
        map.Observe(2, 1, typeof(Room));
        map.Observe(1, 0, typeof(Wall));
        map.Observe(1, 2, typeof(Wall));
        map.Observe(0, 1, typeof(Wall));

        var crawler = NewCrawlerAt(1, 1, Direction.East);
        var strategy = NewAdaptiveStrategy();

        var action = strategy.NextAction(new ExplorerContext(
            crawler.Object,
            typeof(Room),
            new MyInventory(),
            map
        ));

        using var all = Assert.EnterMultipleScope();
        Assert.That(action, Is.EqualTo(ExplorerAction.Walk));
        Assert.That(strategy.CurrentStrategyName, Is.EqualTo(nameof(FrontierDijkstraStrategy)));
    }

    [Test]
    public void FrontierStrategyLetsSecondExplorerPickAnotherFrontierWhenFirstOneAlreadyClaimed()
    {
        var map = new SharedLabyrinthMap();
        map.Observe(1, 1, typeof(Room));
        map.Observe(2, 1, typeof(Room));
        map.Observe(1, 2, typeof(Room));
        map.Observe(1, 0, typeof(Wall));
        map.Observe(0, 1, typeof(Wall));

        var firstCrawler = NewCrawlerAt(1, 1, Direction.East);
        var secondCrawler = NewCrawlerAt(1, 1, Direction.East);
        var firstStrategy = new FrontierDijkstraStrategy();
        var secondStrategy = new FrontierDijkstraStrategy();

        var firstAction = firstStrategy.NextAction(new ExplorerContext(
            firstCrawler.Object,
            typeof(Room),
            new MyInventory(),
            map,
            memory: null,
            explorerId: 101
        ));
        var secondAction = secondStrategy.NextAction(new ExplorerContext(
            secondCrawler.Object,
            typeof(Room),
            new MyInventory(),
            map,
            memory: null,
            explorerId: 202
        ));

        using var all = Assert.EnterMultipleScope();
        Assert.That(firstAction, Is.EqualTo(ExplorerAction.Walk));
        Assert.That(secondAction, Is.EqualTo(ExplorerAction.TurnLeft));
    }

    [Test]
    public void NextActionUsesDoorStrategyWhenBagContainsKeyAndDoorIsKnown()
    {
        var map = new SharedLabyrinthMap();
        map.Observe(1, 1, typeof(Room));
        map.Observe(2, 1, typeof(Door));

        var crawler = NewCrawlerAt(1, 1, Direction.East);
        var strategy = NewAdaptiveStrategy();

        var action = strategy.NextAction(new ExplorerContext(
            crawler.Object,
            typeof(Door),
            new MyInventory(new Key()),
            map
        ));

        using var all = Assert.EnterMultipleScope();
        Assert.That(action, Is.EqualTo(ExplorerAction.Walk));
        Assert.That(strategy.CurrentStrategyName, Is.EqualTo(nameof(DoorDijkstraStrategy)));
    }

    [Test]
    public void AfterFailedDoorAttemptWithSameKeySetAdaptiveFallsBackToFrontier()
    {
        var map = new SharedLabyrinthMap();
        map.Observe(1, 1, typeof(Room));
        map.Observe(2, 1, typeof(Door));
        map.Observe(1, 0, typeof(Wall));
        map.Observe(0, 1, typeof(Wall));
        map.Observe(1, 2, typeof(Room));

        var crawler = NewCrawlerAt(1, 1, Direction.East);
        var strategy = NewAdaptiveStrategy();
        var bag = new MyInventory(new Key());
        var memory = new ExplorerMemory();

        var firstAction = strategy.NextAction(new ExplorerContext(
            crawler.Object,
            typeof(Door),
            bag,
            map,
            memory
        ));

        memory.MarkDoorBlocked(new Laby.Core.Mapping.MapPosition(2, 1), bag);
        crawler.Object.Direction.TurnLeft(); // explorer turns left when walk failed

        var secondAction = strategy.NextAction(new ExplorerContext(
            crawler.Object,
            typeof(Wall),
            bag,
            map,
            memory
        ));

        using var all = Assert.EnterMultipleScope();
        Assert.That(firstAction, Is.EqualTo(ExplorerAction.Walk));
        Assert.That(strategy.CurrentStrategyName, Is.EqualTo(nameof(FrontierDijkstraStrategy)));
        Assert.That(secondAction, Is.EqualTo(ExplorerAction.TurnLeft));
    }

    private static AdaptiveExplorerStrategy NewAdaptiveStrategy() =>
        new(
            new OutsideDijkstraStrategy(),
            new DoorDijkstraStrategy(),
            new FrontierDijkstraStrategy(),
            new LeftWallFollowerStrategy()
        );

    private static Mock<ICrawler> NewCrawlerAt(int x, int y, Direction direction)
    {
        var crawler = new Mock<ICrawler>();
        crawler.SetupGet(c => c.X).Returns(x);
        crawler.SetupGet(c => c.Y).Returns(y);
        crawler.SetupGet(c => c.Direction).Returns(direction);
        crawler.SetupGet(c => c.FacingTileType).Returns(Task.FromResult(typeof(Room)));
        return crawler;
    }
}
