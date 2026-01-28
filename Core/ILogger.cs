namespace RoboSouls.JudgeSystem;

public interface ILogger
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }

    public void Log(LogLevel level, string message, params object[] args);
}

public static class LoggerExtensions
{
    public static void Info(this ILogger logger, string message, params object[] args)
    {
        logger.Log(ILogger.LogLevel.Info, message, args);
    }

    public static void Debug(this ILogger logger, string message, params object[] args)
    {
        logger.Log(ILogger.LogLevel.Debug, message, args);
    }

    public static void Warning(this ILogger logger, string message, params object[] args)
    {
        logger.Log(ILogger.LogLevel.Warning, message, args);
    }

    public static void Error(this ILogger logger, string message, params object[] args)
    {
        logger.Log(ILogger.LogLevel.Error, message, args);
    }
}