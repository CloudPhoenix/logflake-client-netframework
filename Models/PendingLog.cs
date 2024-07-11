namespace NLogFlake.Models;

internal class PendingLog
{
    public int Retries { get; set; }

    public string? QueueName { get; set; }

    public string? JsonString { get; set; }
}
