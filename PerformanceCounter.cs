using System.Diagnostics;

namespace NLogFlake;

internal class PerformanceCounter : IPerformanceCounter
{
    private readonly LogFlake _instance;
    private readonly string _label;
    private readonly Stopwatch _internalStopwatch;

    private bool AlreadySent { get; set; }

    internal PerformanceCounter(LogFlake instance, string label)
    {
        _instance = instance;
        _label = label;
        _internalStopwatch = Stopwatch.StartNew();
    }

    ~PerformanceCounter()
    {
        if (!AlreadySent) Stop();
    }

    public void Start() => _internalStopwatch.Start();

    public void Restart() => _internalStopwatch.Restart();

    public long Stop() => Stop(true);

    public long Pause() => Stop(false);

    private long Stop(bool shouldSend)
    {
        _internalStopwatch.Stop();
        if (!shouldSend)
        {
            return _internalStopwatch.ElapsedMilliseconds;
        }

        AlreadySent = true;
        _instance.SendPerformance(_label, _internalStopwatch.ElapsedMilliseconds);

        return _internalStopwatch.ElapsedMilliseconds;
    }
}
