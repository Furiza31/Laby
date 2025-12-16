using Labyrinth.Items;

namespace Labyrinth.Crawl
{
    /// <summary>
    /// Result of a walking attempt.
    /// </summary>
    public record WalkResult(bool Success, Inventory? Inventory);
}
