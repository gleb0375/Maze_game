using Serilog;

namespace semestral_work.Config
{
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
