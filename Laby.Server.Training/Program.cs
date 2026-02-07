using Laby.Server.Training;
using Dto = Laby.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(_ => new TrainingSessionState());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "Laby Training API v1");
    options.RoutePrefix = "swagger";
    options.DisplayRequestDuration();
});

app.MapGet("/", () => "Laby.Server.Training")
    .WithName("Root")
    .WithSummary("Endpoint racine")
    .WithDescription("Renvoie une chaîne indiquant le service.")
    .WithTags("Info");

app.MapGet("/crawlers", async (Guid? appKey, TrainingSessionState session) =>
        ToHttpResult(await session.GetCrawlersAsync()))
    .WithName("GetCrawlers")
    .WithSummary("Liste des crawlers")
    .WithDescription("Retourne la liste complète des crawlers d'entraînement. Le paramètre 'appKey' est optionnel.")
    .WithTags("Crawlers")
    .Produces<Dto.Crawler[]>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status500InternalServerError);

app.MapPost("/crawlers", async (Guid? appKey, Dto.Settings? settings, TrainingSessionState session) =>
        ToHttpResult(await session.CreateCrawlerAsync(settings)))
    .WithName("CreateCrawler")
    .WithSummary("Crée un crawler")
    .WithDescription("Crée un nouveau crawler avec les paramètres fournis.")
    .WithTags("Crawlers")
    .Produces<Dto.Crawler>(StatusCodes.Status201Created)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status500InternalServerError);

app.MapGet("/crawlers/{id:guid}", async (Guid id, Guid? appKey, TrainingSessionState session) =>
        ToHttpResult(await session.GetCrawlerAsync(id)))
    .WithName("GetCrawlerById")
    .WithSummary("Récupère un crawler")
    .WithDescription("Récupère un crawler par identifiant unique.")
    .WithTags("Crawlers")
    .Produces<Dto.Crawler>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status500InternalServerError);

app.MapPatch("/crawlers/{id:guid}", async (Guid id, Guid? appKey, Dto.Crawler crawler, TrainingSessionState session) =>
        ToHttpResult(await session.UpdateCrawlerAsync(id, crawler)))
    .WithName("UpdateCrawler")
    .WithSummary("Met à jour un crawler")
    .WithDescription("Met à jour les propriétés d'un crawler existant.")
    .WithTags("Crawlers")
    .Produces<Dto.Crawler>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status500InternalServerError);

app.MapDelete("/crawlers/{id:guid}", async (Guid id, Guid? appKey, TrainingSessionState session) =>
        ToHttpResult(await session.DeleteCrawlerAsync(id)))
    .WithName("DeleteCrawler")
    .WithSummary("Supprime un crawler")
    .WithDescription("Supprime un crawler par identifiant. Renvoie 204 en cas de succès.")
    .WithTags("Crawlers")
    .Produces(StatusCodes.Status204NoContent)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status500InternalServerError);

app.MapGet("/crawlers/{id:guid}/bag", async (Guid id, Guid? appKey, TrainingSessionState session) =>
        ToHttpResult(await session.GetBagAsync(id)))
    .WithName("GetCrawlerBag")
    .WithSummary("Inventaire du sac")
    .WithDescription("Retourne le contenu du sac du crawler.")
    .WithTags("Crawlers")
    .Produces<Dto.InventoryItem[]>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status500InternalServerError);

app.MapPut("/crawlers/{id:guid}/bag", async (Guid id, Guid? appKey, Dto.InventoryItem[] bag, TrainingSessionState session) =>
        ToHttpResult(await session.PutBagAsync(id, bag)))
    .WithName("PutCrawlerBag")
    .WithSummary("Remplace le sac")
    .WithDescription("Remplace le contenu du sac du crawler.")
    .WithTags("Crawlers")
    .Produces<Dto.InventoryItem[]>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status500InternalServerError);

app.MapGet("/crawlers/{id:guid}/items", async (Guid id, Guid? appKey, TrainingSessionState session) =>
        ToHttpResult(await session.GetItemsAsync(id)))
    .WithName("GetCrawlerItems")
    .WithSummary("Liste des objets")
    .WithDescription("Retourne la liste des objets du crawler.")
    .WithTags("Crawlers")
    .Produces<Dto.InventoryItem[]>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status500InternalServerError);

app.MapPut("/crawlers/{id:guid}/items", async (Guid id, Guid? appKey, Dto.InventoryItem[] items, TrainingSessionState session) =>
        ToHttpResult(await session.PutItemsAsync(id, items)))
    .WithName("PutCrawlerItems")
    .WithSummary("Remplace les objets")
    .WithDescription("Remplace la liste des objets du crawler.")
    .WithTags("Crawlers")
    .Produces<Dto.InventoryItem[]>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status404NotFound)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .ProducesProblem(StatusCodes.Status500InternalServerError);

app.MapGet("/Groups", async (TrainingSessionState session) =>
        ToHttpResult(await session.GetGroupsAsync()))
    .WithName("GetGroups")
    .WithSummary("Groupes disponibles")
    .WithDescription("Retourne les groupes d'entraînement disponibles.")
    .WithTags("Groups")
    .Produces<GroupInfo[]>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status500InternalServerError);

app.MapPost("/session/restart", async (TrainingSessionState session) =>
        ToHttpResult(await session.RestartAsync()))
    .WithName("RestartSession")
    .WithSummary("Redémarre la session")
    .WithDescription("Réinitialise l'état de la session d'entraînement.")
    .WithTags("Session")
    .Produces(StatusCodes.Status204NoContent)
    .ProducesProblem(StatusCodes.Status500InternalServerError);

app.Run();

static IResult ToHttpResult<T>(TrainingOperationResult<T> result)
{
    if (result.StatusCode == StatusCodes.Status204NoContent)
    {
        return Results.NoContent();
    }

    if (result.StatusCode >= 200 && result.StatusCode < 300)
    {
        return Results.Json(result.Value, statusCode: result.StatusCode);
    }

    return Results.Problem(
        title: result.Problem?.Title,
        detail: result.Problem?.Detail,
        statusCode: result.StatusCode
    );
}

/// <summary>
/// 
/// </summary>
public partial class Program
{
}
