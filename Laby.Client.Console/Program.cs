using Laby.Application;
using Laby.Infrastructure.ApiClient;
using Laby.Core.Build;
using Laby.Core.Crawl;
using Laby.Core.Items;
using Laby.Core.Tiles;
using Dto = Laby.Contracts;
using System.Text.Json;
using Laby.Core;

const int offsetY = 2;

char DirToChar(Direction dir) =>
    "^<v>"[dir.DeltaX * dir.DeltaX + dir.DeltaX + dir.DeltaY + 1];

var tileToChar = new Dictionary<Type, char>
{
    [typeof(Room   )] = ' ',
    [typeof(Wall   )] = '#',
    [typeof(Door   )] = '/'
};

Laby.Core.Labyrinth labyrinth;
ICrawler crawler;
Inventory? bag = null;
ContestSession? contest = null;

if (args.Length < 2)
{
    Console.WriteLine(
        "Commande line usage : https://apiserver.example appKeyGuid [settings.json]"
    );
    labyrinth = new Laby.Core.Labyrinth(new AsciiParser("""
        +--+--------+
        |  /        |
        |  +--+--+  |
        |     |k    |
        +--+  |  +--+
           |k  x    |
        +  +-------/|
        |           |
        +-----------+
        """));
    crawler = labyrinth.NewCrawler();
}
else
{
    Dto.Settings? settings = null;

    if (args.Length > 2)
    {
        settings = JsonSerializer.Deserialize<Dto.Settings>(File.ReadAllText(args[2]));
    }
    contest = await ContestSession.Open(new Uri(args[0]), Guid.Parse(args[1]), settings);
    labyrinth = new Labyrinth(contest.Builder);
    crawler = await contest.NewCrawler();
    bag = contest.Bags.First();
}

var prevX = crawler.X;
var prevY = crawler.Y;
var explorer = new ExplorerCoordinator(crawler);

explorer.DirectionChanged += DrawExplorer;
explorer.PositionChanged  += (s, e) =>
{
    Console.SetCursorPosition(prevX, prevY);
    Console.Write(' ');
    DrawExplorer(s, e);
    (prevX, prevY) = (e.X, e.Y + offsetY);
};

Console.Clear();
Console.SetCursorPosition(0, offsetY);
Console.WriteLine(labyrinth);
await explorer.GetOut(3000, bag);

if (contest is not null)
{
    await contest.Close();
}

void DrawExplorer(object? sender, CrawlingEventArgs e)
{
    var crawler = ((ExplorerCoordinator)sender!).Crawler;
    var facingTileType = crawler.FacingTileType.Result;

    if (facingTileType != typeof(Outside))
    {
        Console.SetCursorPosition(
            e.X + e.Direction.DeltaX, 
            e.Y + e.Direction.DeltaY + offsetY
        );
        Console.Write(tileToChar[facingTileType]);
    }
    Console.SetCursorPosition(e.X, e.Y + offsetY);
    Console.Write(DirToChar(e.Direction));
    Console.SetCursorPosition(0, 0);
    if(crawler is ClientCrawler cc)
    {
        Console.WriteLine($"Bag : { cc.Bag.ItemTypes.Count() } item(s)");
    }
    Thread.Sleep(100);
}
