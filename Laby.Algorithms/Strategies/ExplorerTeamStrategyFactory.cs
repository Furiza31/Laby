namespace Laby.Algorithms
{
    public static class ExplorerTeamStrategyFactory
    {
        public static IReadOnlyList<IExplorerStrategy> CreateDefault() =>
        [
            NewAdaptiveStrategy(rotationOffset: 0),
            NewAdaptiveStrategy(rotationOffset: 1),
            NewAdaptiveStrategy(rotationOffset: 2)
        ];

        private static AdaptiveExplorerStrategy NewAdaptiveStrategy(int rotationOffset) =>
            new(
                new OutsideDijkstraStrategy(rotationOffset),
                new DoorDijkstraStrategy(rotationOffset),
                new FrontierDijkstraStrategy(rotationOffset),
                new LeftWallFollowerStrategy()
            );
    }
}
