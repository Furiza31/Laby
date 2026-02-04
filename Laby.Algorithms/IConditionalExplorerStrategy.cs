namespace Laby.Algorithms
{
    /// <summary>
    /// Strategy that can decline to act so another strategy can be used.
    /// </summary>
    public interface IConditionalExplorerStrategy : IExplorerStrategy
    {
        bool TryNextAction(ExplorerContext context, out ExplorerAction action);
    }
}
