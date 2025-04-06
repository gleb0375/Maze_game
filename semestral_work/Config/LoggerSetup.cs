using Serilog;

namespace semestral_work.Config
{
    /// <summary>
    /// Nastavení globálního loggeru pomocí Serilog podle konfigurace v appsettings.json.
    /// </summary>
    internal class LoggerSetup
    {
        public static void InitializeLogger()
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(AppConfig.Configuration) 
                .CreateLogger();
        }
    }
}
