using Labyrinth;
using Labyrinth.Crawl;
using Labyrinth.Sys;
using Moq;
using static Labyrinth.RandExplorer;

namespace LabyrinthTest;

public class ExplorerTest
{
    private class ExplorerEventsCatcher
    {
        public ExplorerEventsCatcher(RandExplorer explorer)
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

    private static RandExplorer NewExplorerFor(
            string labyrinth,
            out ExplorerEventsCatcher events,
            params Actions[] actions
        )
    {
        var laby = new Labyrinth.Labyrinth(labyrinth);
        var mockRnd = new Mock<IEnumRandomizer<Actions>>();

        var queue = new Queue<Actions>(actions);

        mockRnd.Setup(r => r.Next()).Returns(
            () => queue.Count == 0 ? Actions.TurnLeft : queue.Dequeue()
        );
        var explorer = new RandExplorer(
            laby.NewCrawler(),
            mockRnd.Object
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
        Assert.That(events.DirectionChangedCount, Is.EqualTo(nbTries));
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
            Actions.TurnLeft
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
            Actions.TurnLeft,
            Actions.TurnLeft
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
            Actions.Walk
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
        int nbTries = 3;
        var test = NewExplorerFor("""
            ---+
              x|
            ---+
            """,
            out var events,
            // auto turn left
            Actions.Walk,
            Actions.Walk
        );

        var left = await test.GetOutAsync(nbTries);

        Assert.That(left, Is.Zero);
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
            Actions.Walk,
            Actions.Walk,
            // auto turn left
            Actions.TurnLeft,
            Actions.TurnLeft,
            Actions.Walk,
            Actions.Walk,
            // auto turn left
            Actions.Walk
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
            Actions.Walk,
            Actions.Walk
        );
        var left = await test.GetOutAsync(nbTries);

        Assert.That(left, Is.EqualTo(8));
        Assert.That(events.DirectionChangedCount, Is.EqualTo(0));
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
            Actions.Walk, // key
                          // auto turn left
            Actions.Walk, // door
            Actions.Walk,
            // auto turn left
            Actions.Walk, // key
            Actions.Walk  // door
        );
        var left = await test.GetOutAsync(nbTries);

        Assert.That(left, Is.Zero);
        Assert.That(events.DirectionChangedCount, Is.EqualTo(6));
        Assert.That(events.PositionChangedCount, Is.EqualTo(4));
        Assert.That(events.LastArgs, Is.EqualTo((2, 3, Direction.South)));
    }

    [Test]
    public async Task GetOutPassingTwoKeysBeforeDoors()
    {
        int nbTries = 10;
        var test = NewExplorerFor("""
            +--+
            |kx/
            | k/
            +--+
            """,
            out var events,
            Actions.Walk,// key
            Actions.Walk,
            Actions.Walk,// swap keys
            Actions.Walk // door
        );
        var left = await test.GetOutAsync(nbTries);

        Assert.That(left, Is.Zero);
        Assert.That(events.DirectionChangedCount, Is.EqualTo(7));
        Assert.That(events.PositionChangedCount, Is.EqualTo(3));
        Assert.That(events.LastArgs, Is.EqualTo((2, 2, Direction.East)));
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
            Actions.Walk,// key
                         // auto turn left
            Actions.Walk,// key 
            Actions.Walk,// key
                         // auto turn left
            Actions.Walk,// door
            Actions.Walk,
            // auto turn left
            Actions.Walk,
            Actions.Walk,// door
                         // auto turn left
            Actions.TurnLeft,
            Actions.TurnLeft,
            Actions.Walk // door
        );
        var left = await test.GetOutAsync(nbTries);

        Assert.That(left, Is.Zero);
        Assert.That(events.DirectionChangedCount, Is.EqualTo(13));
        Assert.That(events.PositionChangedCount, Is.EqualTo(7));
        Assert.That(events.LastArgs, Is.EqualTo((1, 3, Direction.West)));
    }

}
