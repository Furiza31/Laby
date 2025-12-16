using Labyrinth;
using Labyrinth.Crawl;
using Labyrinth.Navigation;
using Labyrinth.Sys;

namespace LabyrinthTest;

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

        public (int X, int Y, Direction Dir)? LastArgs { get; private set; }

        private void CatchEvent(ref int counter, CrawlingEventArgs e)
        {
            counter++;
            LastArgs = (e.X, e.Y, e.Direction);
        }

        private int _directionChangedCount;
        private int _positionChangedCount;
    }

    private class QueueStrategy : IMovementStrategy
    {
        private readonly Queue<MoveAction> _actions;

        public QueueStrategy(IEnumerable<MoveAction> actions) =>
            _actions = new Queue<MoveAction>(actions);

        public Task<MoveAction> NextActionAsync(ICrawler crawler, Labyrinth.Items.Inventory bag) =>
            Task.FromResult(_actions.Count == 0 ? MoveAction.TurnLeft : _actions.Dequeue());
    }

    private static IExplorator NewExplorerFor(
        string labyrinth,
        out ExplorerEventsCatcher events,
        params MoveAction[] actions
    )
    {
        var laby = new Labyrinth.Labyrinth(labyrinth);
        var strategy = new QueueStrategy(actions);

        var explorer = new Explorator(
            laby.NewCrawler(),
            strategy
        );
        events = new ExplorerEventsCatcher(explorer);
        return explorer;
    }

    [Test]
    public Task GetOutNegativeThrowsException()
    {
        int nbTries = -3;
        var test = NewExplorerFor("""
            + +
            |x|
            +-+
            """,
            out var events
        );
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => test.GetOutAsync(nbTries)
        );
        Assert.That(events.DirectionChangedCount, Is.Zero);
        Assert.That(events.PositionChangedCount, Is.Zero);
        return Task.CompletedTask;
    }

    [Test]
    public Task GetOutZeroThrowsException()
    {
        int nbTries = 0;
        var test = NewExplorerFor("""
            + +
            |x|
            +-+
            """,
            out var events
        );
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => test.GetOutAsync(nbTries)
        );
        Assert.That(events.DirectionChangedCount, Is.Zero);
        Assert.That(events.PositionChangedCount, Is.Zero);
        return Task.CompletedTask;
    }

    [Test]
    public async Task GetOutInHole()
    {
        int nbTries = 10;
        var test = NewExplorerFor("""
            +-+
            |x/
            +-+
            |k|
            +-+
            """,
            out var events
        );
        var left = await test.GetOutAsync(nbTries);

        Assert.That(left, Is.Zero);
        Assert.That(events.DirectionChangedCount, Is.EqualTo(nbTries));
        Assert.That(events.PositionChangedCount, Is.Zero);
    }

    [Test]
    public async Task GetOutFacingOutsideAtStart()
    {
        int nbTries = 10;
        var test = NewExplorerFor("""
            | x |
            |   |
            +---+
            """,
            out var events
        );
        var left = await test.GetOutAsync(nbTries);

        Assert.That(left, Is.EqualTo(nbTries));
        Assert.That(events.DirectionChangedCount, Is.Zero);
        Assert.That(events.PositionChangedCount, Is.Zero);
    }

    [Test]
    public async Task GetOutRotatingOnce()
    {
        int nbTries = 10;
        var test = NewExplorerFor("""
            --+
              |
            x |
            --+
            """,
            out var events,
            MoveAction.TurnLeft
        );

        var left = await test.GetOutAsync(nbTries);

        Assert.That(left, Is.EqualTo(9));
        Assert.That(events.DirectionChangedCount, Is.EqualTo(1));
        Assert.That(events.PositionChangedCount, Is.Zero);
        Assert.That(events.LastArgs, Is.EqualTo((0, 2, Direction.West)));
    }

    [Test]
    public async Task GetOutRotatingTwice()
    {
        int nbTries = 10;
        var test = NewExplorerFor("""
            +---+
            |   |
            | x |
            """,
            out var events,
            MoveAction.TurnLeft,
            MoveAction.TurnLeft
        );

        var left = await test.GetOutAsync(nbTries);

        Assert.That(left, Is.EqualTo(8));
        Assert.That(events.DirectionChangedCount, Is.EqualTo(2));
        Assert.That(events.LastArgs, Is.EqualTo((2, 2, Direction.South)));
    }

    [Test]
    public async Task GetOutWalkingOnce()
    {
        int nbTries = 10;
        var test = NewExplorerFor("""
            --+
             x|
            --+
            """,
            out var events,
            // auto turn left
            MoveAction.Walk
        );

        var left = await test.GetOutAsync(nbTries);

        Assert.That(left, Is.EqualTo(8));
        Assert.That(events.PositionChangedCount, Is.EqualTo(1));
        Assert.That(events.DirectionChangedCount, Is.EqualTo(1));
        Assert.That(events.LastArgs, Is.EqualTo((0, 1, Direction.West)));
    }

    [Test]
    public async Task GetOutWalkingExactMoves()
    {
        int nbTries = 10;
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

        var left = await test.GetOutAsync(nbTries);

        Assert.That(left, Is.EqualTo(7));
        Assert.That(events.DirectionChangedCount, Is.EqualTo(1));
        Assert.That(events.PositionChangedCount, Is.EqualTo(2));
        Assert.That(events.LastArgs, Is.EqualTo((0, 1, Direction.West)));
    }

    [Test]
    public async Task GetOutWithMultipleMoves()
    {
        int nbTries = 15;
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

        var left = await test.GetOutAsync(nbTries);

        Assert.That(left, Is.EqualTo(6));
        Assert.That(events.DirectionChangedCount, Is.EqualTo(6));
        Assert.That(events.PositionChangedCount, Is.EqualTo(3));
        Assert.That(events.LastArgs, Is.EqualTo((1, 4, Direction.South)));
    }

    [Test]
    public async Task GetOutPassingADoor()
    {
        int nbTries = 10;
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
        var left = await test.GetOutAsync(nbTries);

        Assert.That(left, Is.EqualTo(8));
        Assert.That(events.DirectionChangedCount, Is.Zero);
        Assert.That(events.PositionChangedCount, Is.EqualTo(2));
        Assert.That(events.LastArgs, Is.EqualTo((2, 0, Direction.North)));
    }

    [Test]
    public async Task GetOutPassingTwoDoors()
    {
        int nbTries = 10;
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
        var left = await test.GetOutAsync(nbTries);

        Assert.That(left, Is.EqualTo(2));
        Assert.That(events.DirectionChangedCount, Is.EqualTo(3));
        Assert.That(events.PositionChangedCount, Is.EqualTo(5));
        Assert.That(events.LastArgs?.X, Is.EqualTo(3));
        Assert.That(events.LastArgs?.Y, Is.EqualTo(3));
        Assert.That(events.LastArgs?.Dir, Is.Not.Null);
    }

    [Test]
    public async Task GetOutPassingTwoKeysBeforeDoors()
    {
        int nbTries = 3;
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
        var left = await test.GetOutAsync(nbTries);

        Assert.That(left, Is.EqualTo(0));
        Assert.That(events.DirectionChangedCount, Is.EqualTo(2));
        Assert.That(events.PositionChangedCount, Is.EqualTo(1));
        Assert.That(events.LastArgs?.X, Is.EqualTo(1));
        Assert.That(events.LastArgs?.Y, Is.EqualTo(1));
        Assert.That(events.LastArgs?.Dir, Is.Not.Null);
    }

    [Test]
    public async Task GetOutPassingMultipleKeysBeforeDoors()
    {
        int nbTries = 20;
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
        var left = await test.GetOutAsync(nbTries);

        Assert.That(left, Is.EqualTo(0));
        Assert.That(events.DirectionChangedCount, Is.EqualTo(13));
        Assert.That(events.PositionChangedCount, Is.EqualTo(7));
        Assert.That(events.LastArgs?.X, Is.EqualTo(1));
        Assert.That(events.LastArgs?.Y, Is.EqualTo(3));
        Assert.That(events.LastArgs?.Dir, Is.Not.Null);
    }
}
