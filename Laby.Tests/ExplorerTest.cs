using Laby.Algorithms;
using Laby.Core.Build;
using Laby.Core.Crawl;
using Laby.Algorithms.Sys;
using Moq;

namespace Laby.Tests;

public class ExplorerTest
{
    private class ExplorerEventsCatcher
    {
        public ExplorerEventsCatcher(Explorer explorer)
        {
            explorer.PositionChanged  += (s, e) => CatchEvent(ref _positionChangedCount , e);
            explorer.DirectionChanged += (s, e) => CatchEvent(ref _directionChangedCount, e);
        }
        public int PositionChangedCount => _positionChangedCount;
        public int DirectionChangedCount => _directionChangedCount;

        public (int X, int Y, Direction Dir)? LastArgs { get; private set; } = null;

        private void CatchEvent(ref int counter, CrawlingEventArgs e)
        {
            counter++;
            LastArgs = (e.X, e.Y, e.Direction);
        }
        private int _directionChangedCount = 0, _positionChangedCount = 0;
    }

    private Explorer NewExplorerFor(
        string labyrinth, 
        out ExplorerEventsCatcher events,
        params ExplorerAction[] actions
    ) {
        var laby = new Laby.Core.Labyrinth(new AsciiParser(labyrinth));
        var mockRnd = new Mock<IEnumRandomizer<ExplorerAction>>();

        mockRnd.Setup(r => r.Next()).Returns(
            new Queue<ExplorerAction>(actions).Dequeue
        );
        var explorer = new Explorer(
            laby.NewCrawler(),
            new RandExplorerStrategy(mockRnd.Object)
        );
        events = new ExplorerEventsCatcher(explorer);
        return explorer;
    }
    
    [Test]
    public void GetOutNegativeThrowsException()
    {
        var test = NewExplorerFor("""
            + +
            |x|
            +-+
            """,
            out var events
        );
        Assert.That(
            () => test.GetOut(-3), 
            Throws.TypeOf<ArgumentOutOfRangeException>()
        );
        Assert.That(events.DirectionChangedCount, Is.EqualTo(0));
        Assert.That(events.PositionChangedCount , Is.EqualTo(0));
    }

    [Test]
    public void GetOutZeroThrowsException()
    {
        var test = NewExplorerFor("""
            + +
            |x|
            +-+
            """,
            out var events
        );
        Assert.That(
            () => test.GetOut(0), 
            Throws.TypeOf<ArgumentOutOfRangeException>()
        );
        Assert.That(events.DirectionChangedCount, Is.EqualTo(0));
        Assert.That(events.PositionChangedCount , Is.EqualTo(0));
    }

    [Test]
    public async Task GetOutInHole()
    {
        var test = NewExplorerFor("""
            +-+
            |x/
            +-+
            |k|
            +-+
            """,
            out var events,
            ExplorerAction.Walk,
            ExplorerAction.TurnLeft,
            ExplorerAction.TurnLeft,
            ExplorerAction.TurnLeft
        );
        var left = await test.GetOut(10);

        Assert.That(left, Is.EqualTo(0));
        Assert.That(events.DirectionChangedCount, Is.EqualTo(10));
        Assert.That(events.PositionChangedCount , Is.EqualTo(0));
    }

    [Test]
    public async Task GetOutFacingOutsideAtStart()
    {
        var test = NewExplorerFor("""
            | x |
            |   |
            +---+
            """,
            out var events
        );
        var left = await test.GetOut(10);

        Assert.That(left, Is.EqualTo(10));
        Assert.That(events.DirectionChangedCount, Is.EqualTo(0));
        Assert.That(events.PositionChangedCount , Is.EqualTo(0));
    }

    [Test]
    public async Task GetOutRotatingOnce()
    {
        var test = NewExplorerFor("""
            --+
              |
            x |
            --+
            """, 
            out var events,
            ExplorerAction.TurnLeft
        );

        var left = await test.GetOut(10);

        Assert.That(left, Is.EqualTo(9));
        Assert.That(events.DirectionChangedCount, Is.EqualTo(1));
        Assert.That(events.PositionChangedCount , Is.EqualTo(0));
        Assert.That(events.LastArgs, Is.EqualTo((0, 2, Direction.West)));
    }

    [Test]
    public async Task GetOutRotatingTwice()
    {
        var test = NewExplorerFor("""
            +---+
            |   |
            | x |
            """,
            out var events,
            ExplorerAction.TurnLeft,
            ExplorerAction.TurnLeft
        );

        var left = await test.GetOut(10);

        Assert.That(left, Is.EqualTo(8));
        Assert.That(events.DirectionChangedCount, Is.EqualTo(2));
        Assert.That(events.LastArgs, Is.EqualTo((2, 2, Direction.South)));
    }

    [Test]
    public async Task GetOutWalkingOnce()
    {
        var test = NewExplorerFor("""
            --+
             x|
            --+
            """,
            out var events,
            // auto turn left
            ExplorerAction.Walk
        );

        var left = await test.GetOut(10);

        Assert.That(left, Is.EqualTo(8));
        Assert.That(events.PositionChangedCount , Is.EqualTo(1));
        Assert.That(events.DirectionChangedCount, Is.EqualTo(1));
        Assert.That(events.LastArgs, Is.EqualTo((0, 1, Direction.West)));
    }

    [Test]
    public async Task GetOutWalkingExactMoves()
    {
        var test = NewExplorerFor("""
            ---+
              x|
            ---+
            """,
            out var events,
            // auto turn left
            ExplorerAction.Walk,
            ExplorerAction.Walk
        );

        var left = await test.GetOut(3);

        Assert.That(left, Is.EqualTo(0));
        Assert.That(events.DirectionChangedCount, Is.EqualTo(1));
        Assert.That(events.PositionChangedCount , Is.EqualTo(2));
        Assert.That(events.LastArgs, Is.EqualTo((0, 1, Direction.West)));
    }

    [Test]
    public async Task GetOutWithMultipleMoves()
    {
        var test = NewExplorerFor("""
            +---+
               k|
            + -/+
            |  x|
            | --+
            """,
            out var events,
            ExplorerAction.Walk,
            // auto turn left
            ExplorerAction.Walk,
            ExplorerAction.Walk,
            // auto turn left
            ExplorerAction.TurnLeft,
            ExplorerAction.TurnLeft,
            ExplorerAction.Walk,
            ExplorerAction.Walk,
            // auto turn left
            ExplorerAction.Walk
        );

        var left = await test.GetOut(15);

        Assert.That(left, Is.EqualTo(5));
        Assert.That(events.DirectionChangedCount, Is.EqualTo(5));
        Assert.That(events.PositionChangedCount , Is.EqualTo(5));
        Assert.That(events.LastArgs, Is.EqualTo((0, 1, Direction.West)));
    }

    [Test]
    public async Task GetOutPassingADoor()
    {
        var test = NewExplorerFor("""
            +-/-+
            | k |
            | x |
            +---+
            """,
            out var events,
            ExplorerAction.Walk,
            ExplorerAction.Walk
        );
        var left = await test.GetOut(10);

        Assert.That(left, Is.EqualTo(8));
        Assert.That(events.DirectionChangedCount, Is.EqualTo(0));
        Assert.That(events.PositionChangedCount , Is.EqualTo(2));
        Assert.That(events.LastArgs, Is.EqualTo((2, 0, Direction.North)));
    }

    [Test]
    public async Task GetOutPassingTwoDoors()
    {
        var test = NewExplorerFor("""
            +--+
            |kx|
            +/-+
            | k/
            +--+
            """,
            out var events,
            // auto turn left
            ExplorerAction.Walk, // key
            // auto turn left
            ExplorerAction.Walk, // door
            ExplorerAction.Walk,
            // auto turn left
            ExplorerAction.Walk, // key
            ExplorerAction.Walk  // door
        );
        var left = await test.GetOut(10);

        Assert.That(left, Is.EqualTo(2));
        Assert.That(events.DirectionChangedCount, Is.EqualTo(3));
        Assert.That(events.PositionChangedCount , Is.EqualTo(5));
        Assert.That(events.LastArgs, Is.EqualTo((3, 3, Direction.East)));
    }

    [Test]
    public async Task GetOutPassingTwoKeysBeforeDoors()
    {
        var test = NewExplorerFor("""
            +--+
            |kx/
            | k/
            +--+
            """,
            out var events,
            ExplorerAction.Walk,// key
            ExplorerAction.Walk,
            ExplorerAction.Walk,// swap keys
            ExplorerAction.Walk // door
        );
        var left = await test.GetOut(10);

        Assert.That(left, Is.EqualTo(3));
        Assert.That(events.DirectionChangedCount, Is.EqualTo(3));
        Assert.That(events.PositionChangedCount , Is.EqualTo(4));
        Assert.That(events.LastArgs, Is.EqualTo((3, 2, Direction.East)));
    }

    [Test]
    public async Task GetOutPassingMultipleKeysBeforeDoors()
    {
        var test = NewExplorerFor("""
            +---+
            |kx /
            |k|/|
            |k/ |
            +---+
            """,
            out var events,
            ExplorerAction.Walk,// key
            // auto turn left
            ExplorerAction.Walk,// key 
            ExplorerAction.Walk,// key
            // auto turn left
            ExplorerAction.Walk,// door
            ExplorerAction.Walk,
            // auto turn left
            ExplorerAction.Walk,
            ExplorerAction.Walk,// door
            // auto turn left
            ExplorerAction.TurnLeft,
            ExplorerAction.TurnLeft,
            ExplorerAction.Walk // door
        );
        var left = await test.GetOut(20);

        Assert.That(left, Is.EqualTo(5));
        Assert.That(events.DirectionChangedCount, Is.EqualTo(7));
        Assert.That(events.PositionChangedCount, Is.EqualTo(8));
        Assert.That(events.LastArgs, Is.EqualTo((4, 1, Direction.East)));
    }

}
