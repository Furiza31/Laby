using Laby.Core.Mapping;
using Laby.Core.Tiles;
using Laby.Mapping;

namespace Laby.Tests.Mapping;

public class SharedLabyrinthMapTest
{
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
}
