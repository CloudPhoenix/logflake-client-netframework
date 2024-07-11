namespace NLogFlake;

public interface IPerformanceCounter
{
    void Start();

    void Restart();

    long Stop();

    long Pause();
}
