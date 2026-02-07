using System.Text.Json.Serialization;

namespace Laby.Server.Training;

public sealed class GroupInfo
{
    public string Name { get; init; } = "training";

    [JsonPropertyName("app-keys")]
    public int AppKeys { get; init; }

    [JsonPropertyName("active-crawlers")]
    public int ActiveCrawlers { get; init; }

    [JsonPropertyName("api-calls")]
    public long ApiCalls { get; init; }
}
