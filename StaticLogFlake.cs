namespace NLogFlake;

public static class StaticLogFlake
{
    private static LogFlake? _instance;

    public static LogFlake Instance
    {
        get { return _instance ?? throw new MemberAccessException("Call Configure method before accessing the instance."); }        
    }

    public static void Configure(string appId, string endpoint)
    {
        _instance = new LogFlake(appId, endpoint);
    }
}