using Labyrinth.Crawl;
using Labyrinth.Items;
using Labyrinth.Tiles;

namespace LabyrinthTest.Crawl;

[TestFixture(Description = "Integration test for the crawler implementation in the labyrinth")]
public class LabyrinthCrawlerTest
{
    private static ICrawler NewCrawlerFor(string ascii_map) =>
        new Labyrinth.Labyrinth(ascii_map).NewCrawler();

    private static async Task AssertThatAsync(ICrawler test, int x, int y, Direction dir, Type facingTileType)
    {
        using var all = Assert.EnterMultipleScope();

        Assert.That(test.X, Is.EqualTo(x));
        Assert.That(test.Y, Is.EqualTo(y));
        Assert.That(test.Direction, Is.EqualTo(dir));
        Assert.That(await test.GetFacingTileAsync(), Is.TypeOf(facingTileType));
    }

    private static async Task DrainAsync(MyInventory target, Inventory source)
    {
        while (await target.TryMoveItemFromAsync(source))
        {
            ;
        }
    }

    #region Initialization
    [Test]
    public async Task InitWithCenteredX() =>
        await AssertThatAsync(
            NewCrawlerFor("""
                +--+
                | x|
                +--+
                """
            ),
            x: 2, y: 1,
            Direction.North,
            typeof(Wall)
        );

    [Test]
    public async Task InitWithMultipleXUsesLastOne() =>
        await AssertThatAsync(
            NewCrawlerFor("""
                +--+
                | x|
                |x |
                +--+
                """
            ),
            x: 1, y: 2,
            Direction.North,
            typeof(Room)
        );

    [Test]
    public void InitWithNoXThrowsArgumentException() =>
        Assert.Throws<ArgumentException>(() =>
            new Labyrinth.Labyrinth("""
                +--+
                |  |
                +--+
                """
            )
        );
    #endregion

    #region Labyrinth borders
    [Test]
    public async Task FacingNorthOnUpperTileReturnsOutside() =>
         await AssertThatAsync(
            NewCrawlerFor("""
                +x+
                | |
                +-+
                """
            ),
            x: 1, y: 0,
            Direction.North,
            typeof(Outside)
        );

    [Test]
    public async Task FacingWestOnFarLeftTileReturnsOutside()
    {
        var test = NewCrawlerFor("""
            +-+
            x |
            +-+
            """
        );
        test.Direction.TurnLeft();
        await AssertThatAsync(test,
            x: 0, y: 1,
            Direction.West,
            typeof(Outside)
        );
    }

    [Test]
    public async Task FacingEastOnFarRightTileReturnsOutside()
    {
        var test = NewCrawlerFor("""
            +-+
            | x
            +-+
            """
        );
        test.Direction.TurnRight();
        await AssertThatAsync(test,
            x: 2, y: 1,
            Direction.East,
            typeof(Outside)
        );
    }

    [Test]
    public async Task FacingSouthOnBottomTileReturnsOutside()
    {
        var test = NewCrawlerFor("""
            +-+
            | |
            +x+
            """
        );
        test.Direction.TurnLeft();
        test.Direction.TurnLeft();
        await AssertThatAsync(test,
            x: 1, y: 2,
            Direction.South,
            typeof(Outside)
        );
    }
    #endregion

    #region Moves
    [Test]
    public async Task TurnLeftFacesWestTile()
    {
        var test = NewCrawlerFor("""
            +---+
            |/xk|
            +---+
            """
        );
        test.Direction.TurnLeft();
        await AssertThatAsync(test,
            x: 2, y: 1,
            Direction.West,
            typeof(Door)
        );
    }
    [Test]
    public async Task WalkReturnsInventoryAndChangesPositionAndFacingTile()
    {
        var test = NewCrawlerFor("""
            +/-+
            |  |
            |xk|
            +--+
            """
        );
        var walkResult = await test.TryWalkAsync(new MyInventory());

        Assert.That(walkResult.Success, Is.True);
        Assert.That(walkResult.Inventory, Is.Not.Null);
        Assert.That(walkResult.Inventory!.HasItems, Is.False);
        await AssertThatAsync(test,
            x: 1, y: 1,
            Direction.North,
            typeof(Door)
        );
    }

    [Test]
    public async Task TurnAndWalkReturnsInventoryChangesPositionAndFacingTile()
    {
        var test = NewCrawlerFor("""
            +--+
            |x |
            +--+
            """
        );
        test.Direction.TurnRight();

        var walkResult = await test.TryWalkAsync(new MyInventory());

        Assert.That(walkResult.Success, Is.True);
        Assert.That(walkResult.Inventory, Is.Not.Null);
        Assert.That(walkResult.Inventory!.HasItems, Is.False);
        await AssertThatAsync(test,
            x: 2, y: 1,
            Direction.East,
            typeof(Wall)
        );
    }

    [Test]
    public async Task WalkOnNonTraversableTileFailsAndDontMove()
    {
        var test = NewCrawlerFor("""
            +--+
            |/-+
            |xk|
            +--+
            """
        );
        var result = await test.TryWalkAsync(new MyInventory());

        Assert.That(result.Success, Is.False);
        await AssertThatAsync(test,
            x: 1, y: 2,
            Direction.North,
            typeof(Door)
        );
    }

    [Test]
    public async Task WalkOutsideFailsAndDontMove()
    {
        var test = NewCrawlerFor("""
            |x|
            | |
            +-+
            """
        );
        var result = await test.TryWalkAsync(new MyInventory());

        Assert.That(result.Success, Is.False);
        await AssertThatAsync(test,
            x: 1, y: 0,
            Direction.North,
            typeof(Outside)
        );
    }
    #endregion

    #region Items and doors
    [Test]
    public async Task WalkInARoomWithAnItem()
    {
        var test = NewCrawlerFor("""
        +---+
        |  k|
        |/ x|
        +---+
        """
        );
        var result = await test.TryWalkAsync(new MyInventory());

        using var all = Assert.EnterMultipleScope();

        Assert.That(result.Success, Is.True);
        var itemTypes = await result.Inventory!.ListItemTypesAsync();
        Assert.That(itemTypes.First(), Is.EqualTo(typeof(Key)));
    }

    [Test]
    public async Task WalkUseAWrongKeyToOpenADoor()
    {
        var test = NewCrawlerFor("""
            +----+
            |xk /|
            | /k |
            +----+
            """);

        var bag = new MyInventory();

        test.Direction.TurnRight();
        var keyRoom = await test.TryWalkAsync(bag);
        Assert.That(keyRoom.Success, Is.True);
        await DrainAsync(bag, keyRoom.Inventory!);

        test.Direction.TurnRight();
        var doorResult = await test.TryWalkAsync(bag);

        using var all = Assert.EnterMultipleScope();

        Assert.That(doorResult.Success, Is.False);
        Assert.That(bag.HasItems, Is.True);
        await AssertThatAsync(test,
            x: 2, y: 1,
            Direction.South,
            typeof(Door)
        );
    }

    [Test]
    public async Task WalkUseKeyToOpenADoorAndPass()
    {
        var test = NewCrawlerFor("""
                +---+
                |xk/|
                +---+
                """);

        var bag = new MyInventory();

        test.Direction.TurnRight();
        var keyRoom = await test.TryWalkAsync(bag);
        await DrainAsync(bag, keyRoom.Inventory!);

        var doorPass = await test.TryWalkAsync(bag);

        using var all = Assert.EnterMultipleScope();

        await DrainAsync(bag, doorPass.Inventory!);

        Assert.That(doorPass.Success, Is.True);
        Assert.That(bag.HasItems, Is.True);
        await AssertThatAsync(test,
            x: 3, y: 1,
            Direction.East,
            typeof(Wall)
        );
    }
    #endregion
}
