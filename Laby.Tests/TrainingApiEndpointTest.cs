using System.Net;
using System.Net.Http.Json;
using Laby.Contracts;
using Laby.Server.Training;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Laby.Tests.ServerTraining;

public class TrainingApiEndpointTest
{
    private const string WalkableNorthMap = """
        |   |
        | x |
        +---+
        """;

    private const string BlockedNorthMap = """
        +-+
        |x|
        +-+
        """;

    [Test]
    public async Task GetCrawlersReturnsEmptyListInitially()
    {
        using var factory = NewFactory(WalkableNorthMap);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/crawlers");
        var crawlers = await response.Content.ReadFromJsonAsync<Crawler[]>();

        using var all = Assert.EnterMultipleScope();
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(crawlers, Is.Empty);
    }

    [Test]
    public async Task PostCrawlersCreatesCrawler()
    {
        using var factory = NewFactory(WalkableNorthMap);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/crawlers", (Settings?)null);
        var created = await response.Content.ReadFromJsonAsync<Crawler>();

        using var all = Assert.EnterMultipleScope();
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(created, Is.Not.Null);
        Assert.That(created!.Id, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public async Task GetCrawlerReturnsExistingCrawler()
    {
        using var factory = NewFactory(WalkableNorthMap);
        using var client = factory.CreateClient();
        var created = await CreateCrawlerAsync(client);

        var response = await client.GetAsync($"/crawlers/{created.Id}");
        var crawler = await response.Content.ReadFromJsonAsync<Crawler>();

        using var all = Assert.EnterMultipleScope();
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(crawler, Is.Not.Null);
        Assert.That(crawler!.Id, Is.EqualTo(created.Id));
    }

    [Test]
    public async Task PatchCrawlerUpdatesCrawlerState()
    {
        using var factory = NewFactory(WalkableNorthMap);
        using var client = factory.CreateClient();
        var created = await CreateCrawlerAsync(client);

        var response = await client.PatchAsJsonAsync(
            $"/crawlers/{created.Id}",
            new Crawler
            {
                Dir = Direction.North,
                Walking = true
            }
        );
        var updated = await response.Content.ReadFromJsonAsync<Crawler>();

        using var all = Assert.EnterMultipleScope();
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(updated, Is.Not.Null);
        Assert.That(updated!.Id, Is.EqualTo(created.Id));
        Assert.That(updated.Y, Is.LessThan(created.Y));
    }

    [Test]
    public async Task DeleteCrawlerRemovesCrawler()
    {
        using var factory = NewFactory(BlockedNorthMap);
        using var client = factory.CreateClient();
        var created = await CreateCrawlerAsync(client);

        var deleteResponse = await client.DeleteAsync($"/crawlers/{created.Id}");
        var getAfterDelete = await client.GetAsync($"/crawlers/{created.Id}");

        using var all = Assert.EnterMultipleScope();
        Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(getAfterDelete.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task GetBagReturnsCrawlerBag()
    {
        using var factory = NewFactory(BlockedNorthMap);
        using var client = factory.CreateClient();
        var created = await CreateCrawlerAsync(client);

        var response = await client.GetAsync($"/crawlers/{created.Id}/bag");
        var bag = await response.Content.ReadFromJsonAsync<InventoryItem[]>();

        using var all = Assert.EnterMultipleScope();
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(bag, Is.Empty);
    }

    [Test]
    public async Task PutBagAcceptsPayloadAndReturnsBag()
    {
        using var factory = NewFactory(BlockedNorthMap);
        using var client = factory.CreateClient();
        var created = await CreateCrawlerAsync(client);

        var response = await client.PutAsJsonAsync(
            $"/crawlers/{created.Id}/bag",
            Array.Empty<InventoryItem>()
        );
        var bag = await response.Content.ReadFromJsonAsync<InventoryItem[]>();

        using var all = Assert.EnterMultipleScope();
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(bag, Is.Empty);
    }

    [Test]
    public async Task GetItemsReturnsCurrentTileItems()
    {
        using var factory = NewFactory(BlockedNorthMap);
        using var client = factory.CreateClient();
        var created = await CreateCrawlerAsync(client);

        var response = await client.GetAsync($"/crawlers/{created.Id}/items");
        var items = await response.Content.ReadFromJsonAsync<InventoryItem[]>();

        using var all = Assert.EnterMultipleScope();
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(items, Is.Empty);
    }

    [Test]
    public async Task PutItemsAcceptsPayloadAndReturnsItems()
    {
        using var factory = NewFactory(BlockedNorthMap);
        using var client = factory.CreateClient();
        var created = await CreateCrawlerAsync(client);

        var response = await client.PutAsJsonAsync(
            $"/crawlers/{created.Id}/items",
            Array.Empty<InventoryItem>()
        );
        var items = await response.Content.ReadFromJsonAsync<InventoryItem[]>();

        using var all = Assert.EnterMultipleScope();
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(items, Is.Empty);
    }

    [Test]
    public async Task GetGroupsReturnsCurrentServerInfo()
    {
        using var factory = NewFactory(BlockedNorthMap);
        using var client = factory.CreateClient();
        _ = await CreateCrawlerAsync(client);

        var response = await client.GetAsync("/Groups");
        var groups = await response.Content.ReadFromJsonAsync<GroupInfo[]>();
        var firstGroup = groups is { Length: > 0 }
            ? groups[0]
            : throw new AssertionException("Expected at least one group in /Groups response.");

        using var all = Assert.EnterMultipleScope();
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(groups, Has.Length.EqualTo(1));
        Assert.That(firstGroup.Name, Is.EqualTo("training"));
        Assert.That(firstGroup.ActiveCrawlers, Is.EqualTo(1));
    }

    [Test]
    public async Task RestartEndpointClearsSession()
    {
        using var factory = NewFactory(BlockedNorthMap);
        using var client = factory.CreateClient();
        _ = await CreateCrawlerAsync(client);

        var restartResponse = await client.PostAsync("/session/restart", null);
        var crawlersAfterRestart = await client.GetFromJsonAsync<Crawler[]>("/crawlers");

        using var all = Assert.EnterMultipleScope();
        Assert.That(restartResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(crawlersAfterRestart, Is.Empty);
    }

    private static async Task<Crawler> CreateCrawlerAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync($"/crawlers?appKey={Guid.NewGuid()}", (Settings?)null);
        var created = await response.Content.ReadFromJsonAsync<Crawler>();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(created, Is.Not.Null);

        return created!;
    }

    private static TrainingApiFactory NewFactory(params string[] maps) => new(maps);

    private sealed class TrainingApiFactory(params string[] maps) : WebApplicationFactory<Program>
    {
        private readonly string[] _maps = maps;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<TrainingSessionState>();
                services.AddSingleton(new TrainingSessionState(_maps));
            });
        }
    }
}
