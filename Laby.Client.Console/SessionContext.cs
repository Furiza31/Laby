using Laby.Application;
using Laby.Core;
using Laby.Core.Crawl;
using Laby.Core.Items;
using Laby.Infrastructure.ApiClient;

namespace Laby.Client.Console;

internal sealed class SessionContext
{
    public SessionContext(
        Labyrinth labyrinth,
        ICrawler crawler,
        Inventory? bag,
        ContestSession? contest)
    {
        Labyrinth = labyrinth;
        Crawler = crawler;
        Bag = bag;
        Contest = contest;
        Explorer = new ExplorerCoordinator(crawler);
    }

    public Labyrinth Labyrinth { get; }
    public ICrawler Crawler { get; }
    public Inventory? Bag { get; }
    public ContestSession? Contest { get; }
    public ExplorerCoordinator Explorer { get; }
}
