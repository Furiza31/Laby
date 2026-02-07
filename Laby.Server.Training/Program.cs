using Laby.Server.Training;
using Microsoft.AspNetCore.Http;
using Dto = Laby.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<TrainingSessionState>();

var app = builder.Build();

app.MapGet("/", () => "Laby.Server.Training");

app.MapGet("/crawlers", async (Guid? appKey, TrainingSessionState session) =>
    ToHttpResult(await session.GetCrawlersAsync()));

app.MapPost("/crawlers", async (Guid? appKey, Dto.Settings? settings, TrainingSessionState session) =>
    ToHttpResult(await session.CreateCrawlerAsync(settings)));

app.MapGet("/crawlers/{id:guid}", async (Guid id, Guid? appKey, TrainingSessionState session) =>
    ToHttpResult(await session.GetCrawlerAsync(id)));

app.MapPatch("/crawlers/{id:guid}", async (Guid id, Guid? appKey, Dto.Crawler crawler, TrainingSessionState session) =>
    ToHttpResult(await session.UpdateCrawlerAsync(id, crawler)));

app.MapDelete("/crawlers/{id:guid}", async (Guid id, Guid? appKey, TrainingSessionState session) =>
    ToHttpResult(await session.DeleteCrawlerAsync(id)));

app.MapGet("/crawlers/{id:guid}/bag", async (Guid id, Guid? appKey, TrainingSessionState session) =>
    ToHttpResult(await session.GetBagAsync(id)));

app.MapPut("/crawlers/{id:guid}/bag", async (Guid id, Guid? appKey, Dto.InventoryItem[] bag, TrainingSessionState session) =>
    ToHttpResult(await session.PutBagAsync(id, bag)));

app.MapGet("/crawlers/{id:guid}/items", async (Guid id, Guid? appKey, TrainingSessionState session) =>
    ToHttpResult(await session.GetItemsAsync(id)));

app.MapPut("/crawlers/{id:guid}/items", async (Guid id, Guid? appKey, Dto.InventoryItem[] items, TrainingSessionState session) =>
    ToHttpResult(await session.PutItemsAsync(id, items)));

app.MapGet("/Groups", async (TrainingSessionState session) =>
    ToHttpResult(await session.GetGroupsAsync()));

app.MapPost("/session/restart", async (TrainingSessionState session) =>
    ToHttpResult(await session.RestartAsync()));

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

public partial class Program
{
}
