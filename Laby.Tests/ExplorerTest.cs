using Laby.Algorithms.Navigation;
using Laby.Application.Navigation;
using Laby.Core;
using Laby.Core.Crawl;

namespace Laby.Tests;

public class ExplorerTest
{
    private class ExplorerEventsCatcher
    {
        public ExplorerEventsCatcher(IExplorator explorer)
        {
            explorer.PositionChanged += (s, e) => CatchEvent(ref _positionChangedCount, e);
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

    private class QueueStrategy : IMovementStrategy
    {
        private readonly Queue<MoveAction> _actions;

        public QueueStrategy(IEnumerable<MoveAction> actions) =>
            _actions = new Queue<MoveAction>(actions);

        public MoveAction NextMove(ICrawler crawler) =>
            _actions.Count == 0 ? MoveAction.TurnLeft : _actions.Dequeue();
    }

    private IExplorator NewExplorerFor(
        string labyrinth,
        out ExplorerEventsCatcher events,
        params MoveAction[] actions
    )
    {
        var laby = new Labyrinth(labyrinth);
        var strategy = new QueueStrategy(actions);

        var explorer = new Explorator(
            laby.NewCrawler(),
            strategy
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
        Assert.That(events.PositionChangedCount, Is.EqualTo(0));
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
        Assert.That(events.PositionChangedCount, Is.EqualTo(0));
    }

    [Test]
    public void GetOutInHole()
    {
        var test = NewExplorerFor("""
            +-+
            |x/
            +-+
            |k|
            +-+
            """,
            out var events
        );
        var left = test.GetOut(10);

        Assert.That(left, Is.EqualTo(0));
        Assert.That(events.DirectionChangedCount, Is.EqualTo(10));
        Assert.That(events.PositionChangedCount, Is.EqualTo(0));
    }

    [Test]
    public void GetOutFacingOutsideAtStart()
    {
        var test = NewExplorerFor("""
            | x |
            |   |
            +---+
            """,
            out var events
        );
        var left = test.GetOut(10);

        Assert.That(left, Is.EqualTo(10));
        Assert.That(events.DirectionChangedCount, Is.EqualTo(0));
        Assert.That(events.PositionChangedCount, Is.EqualTo(0));
    }

    [Test]
    public void GetOutRotatingOnce()
    {
        var test = NewExplorerFor("""
            --+
              |
            x |
            --+
            """,
            out var events,
            MoveAction.TurnLeft
        );

        var left = test.GetOut(10);

        Assert.That(left, Is.EqualTo(9));
        Assert.That(events.DirectionChangedCount, Is.EqualTo(1));
        Assert.That(events.PositionChangedCount, Is.EqualTo(0));
        Assert.That(events.LastArgs, Is.EqualTo((0, 2, Direction.West)));
    }

    [Test]
    public void GetOutRotatingTwice()
    {
        var test = NewExplorerFor("""
            +---+
            |   |
            | x |
            """,
            out var events,
            MoveAction.TurnLeft,
            MoveAction.TurnLeft
        );

        var left = test.GetOut(10);

        Assert.That(left, Is.EqualTo(8));
        Assert.That(events.DirectionChangedCount, Is.EqualTo(2));
        Assert.That(events.LastArgs, Is.EqualTo((2, 2, Direction.South)));
    }

    [Test]
    public void GetOutWalkingOnce()
    {
        var test = NewExplorerFor("""
            --+
             x|
            --+
            """,
            out var events,
            // auto turn left
            MoveAction.Walk
        );

        var left = test.GetOut(10);

        Assert.That(left, Is.EqualTo(8));
        Assert.That(events.PositionChangedCount, Is.EqualTo(1));
        Assert.That(events.DirectionChangedCount, Is.EqualTo(1));
        Assert.That(events.LastArgs, Is.EqualTo((0, 1, Direction.West)));
    }

    [Test]
    public void GetOutWalkingExactMoves()
    {
        var test = NewExplorerFor("""
            ---+
              x|
            ---+
            """,
            out var events,
            // auto turn left
            MoveAction.Walk,
            MoveAction.Walk
        );

        var left = test.GetOut(3);

        Assert.That(left, Is.EqualTo(0));
        Assert.That(events.DirectionChangedCount, Is.EqualTo(1));
        Assert.That(events.PositionChangedCount, Is.EqualTo(2));
        Assert.That(events.LastArgs, Is.EqualTo((0, 1, Direction.West)));
    }

    [Test]
    public void GetOutWithMultipleMoves()
    {
        var test = NewExplorerFor("""
            +---+
               k|
            + -/+
            |  x|
            | --+
            """,
            out var events,
            // auto turn left
            MoveAction.Walk,
            MoveAction.Walk,
            // auto turn left
            MoveAction.TurnLeft,
            MoveAction.TurnLeft,
            MoveAction.Walk,
            MoveAction.Walk,
            // auto turn left
            MoveAction.Walk
        );

        var left = test.GetOut(15);

        Assert.That(left, Is.EqualTo(5));
        Assert.That(events.DirectionChangedCount, Is.EqualTo(5));
        Assert.That(events.PositionChangedCount, Is.EqualTo(5));
        Assert.That(events.LastArgs, Is.EqualTo((0, 1, Direction.West)));
    }

    [Test]
    public void GetOutPassingADoor()
    {
        var test = NewExplorerFor("""
            +-/-+
            | k |
            | x |
            +---+
            """,
            out var events,
            MoveAction.Walk,
            MoveAction.Walk
        );
        var left = test.GetOut(10);

        Assert.That(left, Is.EqualTo(8));
        Assert.That(events.DirectionChangedCount, Is.EqualTo(0));
        Assert.That(events.PositionChangedCount, Is.EqualTo(2));
        Assert.That(events.LastArgs, Is.EqualTo((2, 0, Direction.North)));
    }

    [Test]
    public void GetOutPassingTwoDoors()
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
            MoveAction.Walk, // key
                             // auto turn left
            MoveAction.Walk, // door
            MoveAction.Walk,
            // auto turn left
            MoveAction.Walk, // key
            MoveAction.Walk  // door
        );
        var left = test.GetOut(10);

        Assert.That(left, Is.EqualTo(2));
        Assert.That(events.DirectionChangedCount, Is.EqualTo(3));
        Assert.That(events.PositionChangedCount, Is.EqualTo(5));
        Assert.That(events.LastArgs, Is.EqualTo((3, 3, Direction.East)));
    }

    [Test]
    public void GetOutPassingTwoKeysBeforeDoors()
    {
        var test = NewExplorerFor("""
            +--+
            |kx/
            | k/
            +--+
            """,
            out var events,
            MoveAction.Walk,// key
            MoveAction.Walk,
            MoveAction.Walk,// swap keys
            MoveAction.Walk // door
        );
        var left = test.GetOut(10);

        Assert.That(left, Is.EqualTo(3));
        Assert.That(events.DirectionChangedCount, Is.EqualTo(3));
        Assert.That(events.PositionChangedCount, Is.EqualTo(4));
        Assert.That(events.LastArgs, Is.EqualTo((3, 2, Direction.East)));
    }

    [Test]
    public void GetOutPassingMultipleKeysBeforeDoors()
    {
        var test = NewExplorerFor("""
            +---+
            |kx /
            |k|/|
            |k/ |
            +---+
            """,
            out var events,
            MoveAction.Walk,// key
                            // auto turn left
            MoveAction.Walk,// key 
            MoveAction.Walk,// key
                            // auto turn left
            MoveAction.Walk,// door
            MoveAction.Walk,
            // auto turn left
            MoveAction.Walk,
            MoveAction.Walk,// door
                            // auto turn left
            MoveAction.TurnLeft,
            MoveAction.TurnLeft,
            MoveAction.Walk // door
        );
        var left = test.GetOut(20);

        Assert.That(left, Is.EqualTo(5));
        Assert.That(events.DirectionChangedCount, Is.EqualTo(7));
        Assert.That(events.PositionChangedCount, Is.EqualTo(8));
        Assert.That(events.LastArgs, Is.EqualTo((4, 1, Direction.East)));
    }

}
