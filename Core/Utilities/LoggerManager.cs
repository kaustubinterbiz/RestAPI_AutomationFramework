using Serilog;

namespace EnterpriseApiAutomationFramework.Core.Utilities;

public static class LoggerManager
{
    public static void ConfigureLogger()
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.File("Reports/logs.txt")
            .CreateLogger();
    }


    public static void LogInformation(string message)
    {
        Log.Information(message);
    }

    public static void LogError(string message)
    {
        Log.Error(message);
    }
}
