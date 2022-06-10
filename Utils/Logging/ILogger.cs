namespace NetworkTransport.Utils
{
    public interface ILogger
    {
        void Log(string msg);
        void LogWarning(string msg);
        void LogError(string msg);
        void LogException(System.Exception exc);
    }
}

