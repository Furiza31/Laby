using Laby.Algorithms;
using Laby.Core;
using Laby.Core.Items;
using Laby.Core.Mapping;
using Laby.Infrastructure.ApiClient;

namespace Laby.Client.Console.Sessions;

internal sealed class RemoteTeamSessionContext
{
    public RemoteTeamSessionContext(
        Labyrinth labyrinth,
        ContestSession contest,
        IReadOnlyList<Explorer> explorers,
        IReadOnlyList<IExplorerStrategy> strategies,
        IReadOnlyList<Inventory> bags,
        ILabyrinthMapReader sharedMap)
    {
        Labyrinth = labyrinth;
        Contest = contest;
        Explorers = explorers;
        Strategies = strategies;
        Bags = bags;
        SharedMap = sharedMap;
    }

    public Labyrinth Labyrinth { get; }
    public ContestSession Contest { get; }
    public IReadOnlyList<Explorer> Explorers { get; }
    public IReadOnlyList<IExplorerStrategy> Strategies { get; }
    public IReadOnlyList<Inventory> Bags { get; }
    public ILabyrinthMapReader SharedMap { get; }
}
