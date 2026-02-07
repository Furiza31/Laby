using Laby.Core;
using Laby.Core.Build;
using Laby.Core.Crawl;
using Laby.Core.Items;
using Laby.Core.Tiles;
using Microsoft.AspNetCore.Http;
using Dto = Laby.Contracts;

namespace Laby.Server.Training;

public sealed class TrainingSessionState
{
    private const int MaxCrawlerCount = 3;

    public TrainingSessionState() : this(TrainingLabyrinthCatalog.All)
    {
    }

    public TrainingSessionState(IEnumerable<string> maps)
    {
        ArgumentNullException.ThrowIfNull(maps);

        _maps = maps
            .Where(map => !string.IsNullOrWhiteSpace(map))
            .ToArray();
        if (_maps.Length == 0)
        {
            throw new ArgumentException("At least one predefined labyrinth is required.", nameof(maps));
        }

        _mapIndex = 0;
        _labyrinth = BuildLabyrinth(_maps[_mapIndex]);
    }

    public async Task<TrainingOperationResult<IReadOnlyList<Dto.Crawler>>> GetCrawlersAsync()
    {
        CountApiCall();
        await _sync.WaitAsync();
        try
        {
            var crawlers = new List<Dto.Crawler>(_crawlers.Count);
            foreach (var crawler in _crawlers.Values)
            {
                crawlers.Add(await ToDtoAsync(crawler));
            }

            return TrainingOperationResult<IReadOnlyList<Dto.Crawler>>.Success(
                StatusCodes.Status200OK,
                crawlers
            );
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task<TrainingOperationResult<Dto.Crawler>> CreateCrawlerAsync(Dto.Settings? _ = null)
    {
        CountApiCall();
        await _sync.WaitAsync();
        try
        {
            if (_crawlers.Count >= MaxCrawlerCount)
            {
                return Conflict<Dto.Crawler>(
                    StatusCodes.Status403Forbidden,
                    "Crawler limit reached for this training session."
                );
            }

            var crawler = new SessionCrawler(Guid.NewGuid(), _labyrinth.NewCrawler());
            crawler.CurrentItems = GetOrAddTileInventory(crawler.Crawler.X, crawler.Crawler.Y);
            _crawlers.Add(crawler.Id, crawler);

            return TrainingOperationResult<Dto.Crawler>.Success(
                StatusCodes.Status201Created,
                await ToDtoAsync(crawler)
            );
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task<TrainingOperationResult<Dto.Crawler>> GetCrawlerAsync(Guid id)
    {
        CountApiCall();
        await _sync.WaitAsync();
        try
        {
            if (!TryGetCrawler(id, out var crawler))
            {
                return UnknownCrawler<Dto.Crawler>(id);
            }

            return TrainingOperationResult<Dto.Crawler>.Success(
                StatusCodes.Status200OK,
                await ToDtoAsync(crawler)
            );
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task<TrainingOperationResult<Dto.Crawler>> UpdateCrawlerAsync(Guid id, Dto.Crawler update)
    {
        ArgumentNullException.ThrowIfNull(update);

        CountApiCall();
        await _sync.WaitAsync();
        try
        {
            if (!TryGetCrawler(id, out var crawler))
            {
                return UnknownCrawler<Dto.Crawler>(id);
            }

            SetDirection(crawler.Crawler, update.Dir);
            if (update.Walking)
            {
                if (await crawler.Crawler.TryWalk(crawler.Bag) is not Inventory walkedTo)
                {
                    return Conflict<Dto.Crawler>(
                        StatusCodes.Status409Conflict,
                        "Crawler cannot walk through the facing tile."
                    );
                }

                crawler.CurrentItems = walkedTo;
                _tileInventories[(crawler.Crawler.X, crawler.Crawler.Y)] = walkedTo;
            }

            var dto = await ToDtoAsync(crawler);
            if (await AllCrawlersReachedExitAsync())
            {
                AdvanceSessionInternal();
            }

            return TrainingOperationResult<Dto.Crawler>.Success(
                StatusCodes.Status200OK,
                dto
            );
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task<TrainingOperationResult<bool>> DeleteCrawlerAsync(Guid id)
    {
        CountApiCall();
        await _sync.WaitAsync();
        try
        {
            if (!_crawlers.Remove(id))
            {
                return UnknownCrawler<bool>(id);
            }

            return TrainingOperationResult<bool>.Success(StatusCodes.Status204NoContent, true);
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task<TrainingOperationResult<Dto.InventoryItem[]>> GetBagAsync(Guid id)
    {
        CountApiCall();
        await _sync.WaitAsync();
        try
        {
            if (!TryGetCrawler(id, out var crawler))
            {
                return UnknownCrawler<Dto.InventoryItem[]>(id);
            }

            return TrainingOperationResult<Dto.InventoryItem[]>.Success(
                StatusCodes.Status200OK,
                ToDtoItems(crawler.Bag)
            );
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task<TrainingOperationResult<Dto.InventoryItem[]>> GetItemsAsync(Guid id)
    {
        CountApiCall();
        await _sync.WaitAsync();
        try
        {
            if (!TryGetCrawler(id, out var crawler))
            {
                return UnknownCrawler<Dto.InventoryItem[]>(id);
            }

            return TrainingOperationResult<Dto.InventoryItem[]>.Success(
                StatusCodes.Status200OK,
                ToDtoItems(crawler.CurrentItems)
            );
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task<TrainingOperationResult<Dto.InventoryItem[]>> PutBagAsync(Guid id, Dto.InventoryItem[] items)
    {
        ArgumentNullException.ThrowIfNull(items);

        CountApiCall();
        await _sync.WaitAsync();
        try
        {
            if (!TryGetCrawler(id, out var crawler))
            {
                return UnknownCrawler<Dto.InventoryItem[]>(id);
            }

            return await MoveItemsAsync(
                source: crawler.Bag,
                destination: crawler.CurrentItems,
                payload: items,
                resultInventory: crawler.Bag
            );
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task<TrainingOperationResult<Dto.InventoryItem[]>> PutItemsAsync(Guid id, Dto.InventoryItem[] items)
    {
        ArgumentNullException.ThrowIfNull(items);

        CountApiCall();
        await _sync.WaitAsync();
        try
        {
            if (!TryGetCrawler(id, out var crawler))
            {
                return UnknownCrawler<Dto.InventoryItem[]>(id);
            }

            return await MoveItemsAsync(
                source: crawler.CurrentItems,
                destination: crawler.Bag,
                payload: items,
                resultInventory: crawler.CurrentItems
            );
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task<TrainingOperationResult<bool>> RestartAsync()
    {
        CountApiCall();
        await _sync.WaitAsync();
        try
        {
            AdvanceSessionInternal();
            return TrainingOperationResult<bool>.Success(StatusCodes.Status204NoContent, true);
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task<TrainingOperationResult<IReadOnlyList<GroupInfo>>> GetGroupsAsync()
    {
        CountApiCall();
        await _sync.WaitAsync();
        try
        {
            return TrainingOperationResult<IReadOnlyList<GroupInfo>>.Success(
                StatusCodes.Status200OK,
                [
                    new GroupInfo
                    {
                        Name = "training",
                        AppKeys = 1,
                        ActiveCrawlers = _crawlers.Count,
                        ApiCalls = _apiCalls
                    }
                ]
            );
        }
        finally
        {
            _sync.Release();
        }
    }

    private async Task<TrainingOperationResult<Dto.InventoryItem[]>> MoveItemsAsync(
        Inventory source,
        Inventory destination,
        IReadOnlyList<Dto.InventoryItem> payload,
        Inventory resultInventory
    )
    {
        var movesRequired = BuildMovesRequired(source, payload);
        if (movesRequired is null)
        {
            return Conflict<Dto.InventoryItem[]>(
                StatusCodes.Status409Conflict,
                "Inventory changed since the last read."
            );
        }

        if (!await destination.TryMoveItemsFrom(source, movesRequired))
        {
            return Conflict<Dto.InventoryItem[]>(
                StatusCodes.Status409Conflict,
                "Failed to transfer inventory items."
            );
        }

        return TrainingOperationResult<Dto.InventoryItem[]>.Success(
            StatusCodes.Status200OK,
            ToDtoItems(resultInventory)
        );
    }

    private async Task<bool> AllCrawlersReachedExitAsync()
    {
        if (_crawlers.Count == 0)
        {
            return false;
        }

        foreach (var crawler in _crawlers.Values)
        {
            if (await crawler.Crawler.FacingTileType != typeof(Outside))
            {
                return false;
            }
        }

        return true;
    }

    private void AdvanceSessionInternal()
    {
        _mapIndex = (_mapIndex + 1) % _maps.Length;
        _labyrinth = BuildLabyrinth(_maps[_mapIndex]);
        _crawlers.Clear();
        _tileInventories.Clear();
    }

    private Inventory GetOrAddTileInventory(int x, int y)
    {
        var key = (x, y);
        if (_tileInventories.TryGetValue(key, out var inventory))
        {
            return inventory;
        }

        inventory = new MyInventory();
        _tileInventories[key] = inventory;
        return inventory;
    }

    private static Labyrinth BuildLabyrinth(string map) =>
        new(new AsciiParser(map));

    private static Dto.InventoryItem[] ToDtoItems(Inventory inventory) =>
        [.. inventory.ItemTypes.Select(_ => new Dto.InventoryItem { Type = Dto.ItemType.Key })];

    private static Dto.Crawler BuildDto(
        SessionCrawler crawler,
        Dto.TileType facingTile
    ) =>
        new()
        {
            Id = crawler.Id,
            X = crawler.Crawler.X,
            Y = crawler.Crawler.Y,
            Dir = ToDtoDirection(crawler.Crawler.Direction),
            Walking = false,
            FacingTile = facingTile,
            Bag = ToDtoItems(crawler.Bag),
            Items = ToDtoItems(crawler.CurrentItems)
        };

    private static IList<bool>? BuildMovesRequired(
        Inventory source,
        IReadOnlyList<Dto.InventoryItem> payload
    )
    {
        var sourceCount = source.ItemTypes.Count();
        if (payload.Count != sourceCount)
        {
            return null;
        }

        return payload.Select(item => item.MoveRequired == true).ToList();
    }

    private static void SetDirection(ICrawler crawler, Dto.Direction direction)
    {
        var target = ToCoreDirection(direction);
        for (var i = 0; i < 4 && crawler.Direction != target; i++)
        {
            crawler.Direction.TurnLeft();
        }
    }

    private static Dto.Direction ToDtoDirection(Direction direction)
    {
        if (direction == Direction.North) return Dto.Direction.North;
        if (direction == Direction.East) return Dto.Direction.East;
        if (direction == Direction.South) return Dto.Direction.South;
        if (direction == Direction.West) return Dto.Direction.West;
        throw new NotSupportedException("Unknown crawler direction.");
    }

    private static Direction ToCoreDirection(Dto.Direction direction) =>
        direction switch
        {
            Dto.Direction.North => Direction.North,
            Dto.Direction.East => Direction.East,
            Dto.Direction.South => Direction.South,
            Dto.Direction.West => Direction.West,
            _ => throw new NotSupportedException("Unknown DTO direction.")
        };

    private static Dto.TileType ToDtoTile(Type tileType)
    {
        if (tileType == typeof(Outside)) return Dto.TileType.Outside;
        if (tileType == typeof(Room)) return Dto.TileType.Room;
        if (tileType == typeof(Wall)) return Dto.TileType.Wall;
        if (tileType == typeof(Door)) return Dto.TileType.Door;
        throw new NotSupportedException($"Unsupported tile type: {tileType.Name}");
    }

    private async Task<Dto.Crawler> ToDtoAsync(SessionCrawler crawler) =>
        BuildDto(crawler, ToDtoTile(await crawler.Crawler.FacingTileType));

    private bool TryGetCrawler(Guid id, out SessionCrawler crawler) =>
        _crawlers.TryGetValue(id, out crawler!);

    private static TrainingOperationResult<T> UnknownCrawler<T>(Guid id) =>
        TrainingOperationResult<T>.Failure(
            StatusCodes.Status404NotFound,
            "Unknown crawler",
            $"Crawler '{id}' was not found."
        );

    private static TrainingOperationResult<T> Conflict<T>(int statusCode, string detail) =>
        TrainingOperationResult<T>.Failure(
            statusCode,
            "Conflict",
            detail
        );

    private void CountApiCall() => Interlocked.Increment(ref _apiCalls);

    private sealed class SessionCrawler
    {
        public SessionCrawler(Guid id, ICrawler crawler)
        {
            Id = id;
            Crawler = crawler;
        }

        public Guid Id { get; }

        public ICrawler Crawler { get; }

        public MyInventory Bag { get; } = new();

        public Inventory CurrentItems { get; set; } = new MyInventory();
    }

    private readonly SemaphoreSlim _sync = new(1, 1);
    private readonly Dictionary<Guid, SessionCrawler> _crawlers = new();
    private readonly Dictionary<(int X, int Y), Inventory> _tileInventories = new();
    private readonly string[] _maps;
    private Labyrinth _labyrinth;
    private int _mapIndex;
    private long _apiCalls;
}
