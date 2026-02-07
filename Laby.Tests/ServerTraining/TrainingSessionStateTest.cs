using Laby.Contracts;
using Laby.Server.Training;

namespace Laby.Tests.ServerTraining;

public class TrainingSessionStateTest
{
    private const string BlockedNorthMap = """
        +-+
        |x|
        +-+
        """;

    private const string KeyRoomNorthMap = """
        +---+
        |/k |
        | x |
        +---+
        """;

    private const string ReachExitAfterOneStepMap = """
        |   |
        | x |
        +---+
        """;

    [Test]
    public void DefaultCatalogCanBeLoaded()
    {
        Assert.That(
            () => new TrainingSessionState(),
            Throws.Nothing
        );
    }

    [Test]
    public async Task CreateCrawlerReturnsInitialCrawlerState()
    {
        var test = NewSession(BlockedNorthMap);

        var created = await test.CreateCrawlerAsync();
        var crawlers = await test.GetCrawlersAsync();

        using var all = Assert.EnterMultipleScope();
        Assert.That(created.StatusCode, Is.EqualTo(201));
        Assert.That(created.Value, Is.Not.Null);
        Assert.That(created.Value!.Bag, Is.Empty);
        Assert.That(created.Value.Items, Is.Empty);
        Assert.That(crawlers.StatusCode, Is.EqualTo(200));
        Assert.That(crawlers.Value, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task CreateCrawlerRejectsWhenLimitIsReached()
    {
        var test = NewSession(BlockedNorthMap);

        await test.CreateCrawlerAsync();
        await test.CreateCrawlerAsync();
        await test.CreateCrawlerAsync();
        var rejected = await test.CreateCrawlerAsync();

        Assert.That(rejected.StatusCode, Is.EqualTo(403));
    }

    [Test]
    public async Task PatchCrawlerReturnsConflictWhenFacingWall()
    {
        var test = NewSession(BlockedNorthMap);
        var created = await test.CreateCrawlerAsync();

        var updated = await test.UpdateCrawlerAsync(
            created.Value!.Id,
            new Crawler
            {
                Dir = Direction.North,
                Walking = true
            }
        );

        Assert.That(updated.StatusCode, Is.EqualTo(409));
    }

    [Test]
    public async Task PutItemsMovesSelectedItemsFromRoomToBag()
    {
        var test = NewSession(KeyRoomNorthMap);
        var created = await test.CreateCrawlerAsync();

        var walked = await test.UpdateCrawlerAsync(
            created.Value!.Id,
            new Crawler
            {
                Dir = Direction.North,
                Walking = true
            }
        );
        var itemsBeforeMove = await test.GetItemsAsync(created.Value.Id);
        var afterMove = await test.PutItemsAsync(
            created.Value.Id,
            [new InventoryItem { Type = ItemType.Key, MoveRequired = true }]
        );
        var bag = await test.GetBagAsync(created.Value.Id);

        using var all = Assert.EnterMultipleScope();
        Assert.That(walked.StatusCode, Is.EqualTo(200));
        Assert.That(itemsBeforeMove.Value, Has.Length.EqualTo(1));
        Assert.That(afterMove.StatusCode, Is.EqualTo(200));
        Assert.That(afterMove.Value, Is.Empty);
        Assert.That(bag.Value, Has.Length.EqualTo(1));
    }

    [Test]
    public async Task SessionResetsAutomaticallyWhenAllCrawlersReachExit()
    {
        var test = NewSession(ReachExitAfterOneStepMap);
        var created = await test.CreateCrawlerAsync();

        var walked = await test.UpdateCrawlerAsync(
            created.Value!.Id,
            new Crawler
            {
                Dir = Direction.North,
                Walking = true
            }
        );
        var crawlersAfterReset = await test.GetCrawlersAsync();

        using var all = Assert.EnterMultipleScope();
        Assert.That(walked.StatusCode, Is.EqualTo(200));
        Assert.That(crawlersAfterReset.Value, Is.Empty);
    }

    [Test]
    public async Task RestartSessionClearsAllCrawlers()
    {
        var test = NewSession(BlockedNorthMap);
        await test.CreateCrawlerAsync();

        var restarted = await test.RestartAsync();
        var crawlers = await test.GetCrawlersAsync();

        using var all = Assert.EnterMultipleScope();
        Assert.That(restarted.StatusCode, Is.EqualTo(204));
        Assert.That(crawlers.Value, Is.Empty);
    }

    private static TrainingSessionState NewSession(params string[] maps) => new(maps);
}
