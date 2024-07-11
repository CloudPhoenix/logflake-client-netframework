using Newtonsoft.Json;

namespace NLogFlake.Models;

internal class LogObject
{
    [JsonProperty("datetime")]
    public DateTime Date = DateTime.UtcNow;

    [JsonProperty("hostname")]
    public string? Hostname { get; set; }

    [JsonProperty("level")]
    public LogLevels Level { get; set; }

    [JsonProperty("content")]
    public string? Content { get; set; }

    [JsonProperty("correlation")]
    public string? Correlation { get; set; }

    [JsonProperty("params")]
    public Dictionary<string, object>? Parameters { get; set; }

    [JsonProperty("label")]
    public string? Label { get; set; }

    [JsonProperty("duration")]
    public long Duration { get; set; }

    public override string ToString() => JsonConvert.SerializeObject(this);
}
