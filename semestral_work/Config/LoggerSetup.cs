using Serilog;
using Microsoft.Extensions.Configuration;
using Serilog.Settings.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
