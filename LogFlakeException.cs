namespace NLogFlake;

[Serializable]
public class LogFlakeException : Exception
{
    public LogFlakeException() { }

    public LogFlakeException(string message) : base(message) { }

    public LogFlakeException(string message, Exception innerException) : base(message, innerException) { }
}
