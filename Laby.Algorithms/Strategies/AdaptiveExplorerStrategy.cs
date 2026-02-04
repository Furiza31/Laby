namespace Laby.Algorithms
{
    /// <summary>
    /// Runs a chain of strategies and selects the first one that can decide an action.
    /// </summary>
    public class AdaptiveExplorerStrategy(params IExplorerStrategy[] strategies) : IExplorerStrategy
    {
        private readonly IReadOnlyList<IExplorerStrategy> _strategies =
            strategies.Length == 0
                ? throw new ArgumentException("At least one strategy is required.", nameof(strategies))
                : strategies;

        public string CurrentStrategyName { get; private set; } = strategies[0].GetType().Name;

        public ExplorerAction NextAction(ExplorerContext context)
        {
            foreach (var strategy in _strategies)
            {
                if (strategy is IConditionalExplorerStrategy conditional)
                {
                    if (!conditional.TryNextAction(context, out var conditionalAction))
                    {
                        continue;
                    }

                    CurrentStrategyName = strategy.GetType().Name;
                    return conditionalAction;
                }

                CurrentStrategyName = strategy.GetType().Name;
                return strategy.NextAction(context);
            }

            CurrentStrategyName = nameof(AdaptiveExplorerStrategy);
            return ExplorerAction.TurnLeft;
        }
    }
}
