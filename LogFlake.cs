using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using NLogFlake.Constants;
using NLogFlake.Models;
using Snappier;

namespace NLogFlake;

public class LogFlake
{
    private Uri Server { get; }
    private string? _hostname = Environment.MachineName;
    private string AppId { get; set; }
    private readonly ConcurrentQueue<PendingLog> _logsQueue = new();
    private readonly ManualResetEvent _processLogs = new(false);
    private Thread LogsProcessorThread { get; set; }
    private bool IsShuttingDown { get; set; }

    internal const int FailedPostRetries = 3;
    internal const int PostTimeoutSeconds = 3;

    public void SetHostname() => SetHostname(null);
    public string? GetHostname() => _hostname;
    public void SetHostname(string? hostname) => _hostname = string.IsNullOrWhiteSpace(hostname) ? null : hostname;

    public LogFlake(string appId, string? endpoint)
    {
        if (!string.IsNullOrWhiteSpace(endpoint) && !Uri.IsWellFormedUriString(endpoint, UriKind.Absolute))
        {
            throw new ArgumentException("Parameter must be a fully qualified Uri.", nameof(endpoint));
        }

        AppId = appId;
        Server = new Uri(endpoint ?? ServersConstants.PRODUCTION);

        LogsProcessorThread = new Thread(LogsProcessor);
        LogsProcessorThread.Start();
    }

    ~LogFlake() => Shutdown();

    public void Shutdown()
    {
        IsShuttingDown = true;
        LogsProcessorThread.Join();
    }

    private void LogsProcessor()
    {
        SendLog(LogLevels.DEBUG, $"LogFlake started on {_hostname}");

        _processLogs.WaitOne();

        while (!_logsQueue.IsEmpty)
        {
            _ = _logsQueue.TryDequeue(out PendingLog? log);
            log.Retries++;
            bool success = Post(log.QueueName!, log.JsonString!).GetAwaiter().GetResult();
            if (!success && log.Retries < FailedPostRetries)
            {
                _logsQueue.Enqueue(log);
            }

            _processLogs.Reset();

            if (_logsQueue.IsEmpty && !IsShuttingDown)
            {
                _processLogs.WaitOne();
            }
        }
    }

    private async Task<bool> Post(string queueName, string jsonString)
    {
        if (queueName != QueuesConstants.LOGS && queueName != QueuesConstants.PERFORMANCES)
        {
            return false;
        }

        try
        {
            string requestUri = $"/api/ingestion/{AppId}/{queueName}";
            HttpResponseMessage result = new(System.Net.HttpStatusCode.InternalServerError);
            using HttpClient httpClient = CreateClient();
            httpClient.BaseAddress = Server;

            byte[] jsonStringBytes = Encoding.UTF8.GetBytes(jsonString);
            string base64String = Convert.ToBase64String(jsonStringBytes);
            byte[] compressed = Snappy.CompressToArray(Encoding.UTF8.GetBytes(base64String));
            ByteArrayContent content = new(compressed);
            content.Headers.Remove("Content-Type");
            content.Headers.Add("Content-Type", "application/octet-stream");
            result = await httpClient.PostAsync(requestUri, content);

            return result.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public void SendLog(string content, Dictionary<string, object>? parameters = null) => SendLog(LogLevels.DEBUG, content, parameters);

    public void SendLog(LogLevels level, string content, Dictionary<string, object>? parameters = null) => SendLog(level, null, content, parameters);

    public void SendLog(LogLevels level, string? correlation, string? content, Dictionary<string, object>? parameters = null)
    {
        _logsQueue.Enqueue(new PendingLog
        {
            QueueName = QueuesConstants.LOGS,
            JsonString = new LogObject
            {
                Level = level,
                Hostname = GetHostname(),
                Content = content!,
                Correlation = correlation,
                Parameters = parameters,
            }.ToString()
        });

        _processLogs.Set();
    }

    public void SendException(Exception e) => SendException(e, null);

    public void SendException(Exception e, string? correlation)
    {
        StringBuilder additionalTrace = new();
        if (e.Data.Count > 0)
        {
            additionalTrace.Append($"{Environment.NewLine}Data:");
            additionalTrace.Append($"{Environment.NewLine}{JsonConvert.SerializeObject(e.Data, new JsonSerializerSettings { Formatting = Formatting.Indented })}");
        }

        _logsQueue.Enqueue(new PendingLog
        {
            QueueName = QueuesConstants.LOGS,
            JsonString = new LogObject
            {
                Level = LogLevels.EXCEPTION,
                Hostname = GetHostname(),
                Content = $"{e.Demystify()}{additionalTrace}",
                Correlation = correlation,
            }.ToString()
        });

        _processLogs.Set();
    }

    public void SendPerformance(string label, long duration)
    {
        _logsQueue.Enqueue(new PendingLog
        {
            QueueName = QueuesConstants.PERFORMANCES,
            JsonString = new LogObject
            {
                Label = label,
                Duration = duration,
            }.ToString()
        });

        _processLogs.Set();
    }

    public IPerformanceCounter MeasurePerformance(string label) => new PerformanceCounter(this, label);

    private static HttpClient CreateClient()
    {
        HttpClient client = new()
        {
            Timeout = TimeSpan.FromSeconds(PostTimeoutSeconds)
        };
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        client.DefaultRequestHeaders.Add("User-Agent", "logflake-client-framework/1.6.0");

        return client;
    }
}
