using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace semestral_work.Config
{

    internal class AppConfig
    {
        public static IConfiguration Configuration { get; private set; }

        public static void Init()
        {
            LoadConfiguration();
        }
        public static void LoadConfiguration()
        {
            Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }

        public static string GetMapFilePath() => Configuration["MapConfig:FilePath"];
    }
}
