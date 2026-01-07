using Laby.Algorithms.Navigation;
using Laby.Algorithms.Sys;
using Laby.Application.Navigation;
using Laby.Core;
using Laby.Core.Crawl;

char DirToChar(Direction dir) =>
    "^<v>"[dir.DeltaX * dir.DeltaX + dir.DeltaX + dir.DeltaY + 1];

void DrawExplorer(object? sender, CrawlingEventArgs e)
{
    Console.SetCursorPosition(e.X, e.Y);
    Console.Write(DirToChar(e.Direction));
    Console.SetCursorPosition(0, 0);
    Thread.Sleep(50);
}

var labyrinth = new Labyrinth("""
    +--+--------+
    |  /        |
    |  +--+--+  |
    |     |k    |
    +--+  |  +--+
       |k  x    |
    +  +-------/|
    |           |
    +-----------+
    """);
var crawler = labyrinth.NewCrawler();
var prevX = crawler.X;
var prevY = crawler.Y;
var moveRandomizer = new BasicEnumRandomizer<MoveAction>();
var strategy = new RandomMovementStrategy(moveRandomizer);
var explorer = new Explorator(
    crawler,
    strategy
);

explorer.DirectionChanged += DrawExplorer;
explorer.PositionChanged += (s, e) =>
{
    Console.SetCursorPosition(prevX, prevY);
    Console.Write(' ');
    DrawExplorer(s, e);
    (prevX, prevY) = (e.X, e.Y);
};

Console.Clear();
Console.WriteLine(labyrinth);
explorer.GetOut(1000);
