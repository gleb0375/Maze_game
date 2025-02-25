using Microsoft.Extensions.Configuration;
using System;

namespace semestral_work.Config
{
    internal class AppConfig
    {
        public static IConfiguration Configuration { get; private set; } = null!;

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

        public static string GetMapFilePath() =>
            Configuration["MapConfig:FilePath"] ?? throw new Exception("Map file path is not configured in appsettings.json.");

        public static (int Width, int Height) GetWindowDimensions()
        {
            if (!int.TryParse(Configuration["Window:Width"], out int width))
            {
                throw new Exception("Invalid Window:Width value in appsettings.json");
            }
            if (!int.TryParse(Configuration["Window:Height"], out int height))
            {
                throw new Exception("Invalid Window:Height value in appsettings.json");
            }
            return (width, height);
        }

        public static string GetVertexShaderPath() =>
            Configuration["ShaderConfig:VertexShaderPath"] ?? throw new Exception("Vertex shader path is not configured.");

        public static string GetFragmentShaderPath() =>
            Configuration["ShaderConfig:FragmentShaderPath"] ?? throw new Exception("Fragment shader path is not configured.");

        public static string GetFloorTexturePath() =>
            Configuration["TextureConfig:FloorTexturePath"] ?? throw new Exception("Floor texture path is not configured.");

        public static string GetWallTexturePath() =>
            Configuration["TextureConfig:WallTexturePath"] ?? throw new Exception("Wall texture path is not configured.");
    }
}
