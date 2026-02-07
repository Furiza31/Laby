namespace Laby.Server.Training;

public static class TrainingLabyrinthCatalog
{
    public static IReadOnlyList<string> All { get; } =
    [
        Normalize("""
+---------------+
|               |
| +----/------+ |
| |        k  |k|
| | +--/----+ | |
| / |    k  | | |
| | | +---+ | | |
| | /   |x  | | |
| | | +-+-+ | | |
| | |  k|   | | |
| | +--+--+ | | |
| |        k| | |
| +---------+-+-|
|               /
+---------------+
"""),
        Normalize("""
+---------------+
| x     k  k    |
| +-----/---+   |
| |         |   |
| | +---+   |   |
| | |   |   |   |
| | |   +---+   |
| | |       k   |
| | +-----/-----|
| |             /
+---------------+
"""),
        Normalize("""
+-----------------------+
|               |       |
| +----/------+ | +---+ |
| |        k  |k| |   | |
| | +--/----+ | | | + | |
| / |    k  | | | | | | |
| | | +---+ | | | | | | |
| | /   |x  | | | | | | |
| | | +-+-+ | | | | | | |
| | |  k|   | | | | | | |
| | +--+--+ | | | | | | |
| |        k| | | |   | |
| +---------+-+-| +---+ |
|               /
+---------------+ +-----+
|                 |k    |
| +-------------+ +--/--+
| |     k       |       |
| +--/------+---+ +---+ |
|    |      |k    |   | |
+----+ +----+--/--+ + | |
|      |          | + | |
| +----+ +--------+ + | |
| |   k  |      /   | | |
| +------+ +---------+k|/
+-----------------------+
""")
    ];

    private static string Normalize(string map)
    {
        var lines = map.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var cleaned = lines.Where(line => line.Length > 0).ToArray();
        var width = cleaned.Max(line => line.Length);

        return string.Join('\n', cleaned.Select(line => line.PadRight(width)));
    }
}
