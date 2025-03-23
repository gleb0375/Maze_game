using Microsoft.Extensions.Configuration;
using System.Globalization;
using Serilog;

namespace semestral_work.Config
{
    internal class AppConfig
    {
        public static IConfiguration Configuration { get; private set; } = null!;

        public static void Init()
        {
            LoadConfiguration();
            Log.Information("AppConfig initialized.");
        }
        public static void LoadConfiguration()
        {
            Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
            Log.Information("Configuration loaded successfully from appsettings.json.");
        }

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
            Log.Information("Window dimensions read: {Width}x{Height}", width, height);
            return (width, height);
        }
        public static string GetMapFilePath() =>
            Configuration["MapConfig:FilePath"] ?? throw new Exception("Map file path is not configured in appsettings.json.");

        public static string GetVertexShaderPath() =>
            Configuration["ShaderConfig:VertexShaderPath"] ?? throw new Exception("Vertex shader path is not configured.");

        public static string GetFragmentShaderPath() =>
            Configuration["ShaderConfig:FragmentShaderPath"] ?? throw new Exception("Fragment shader path is not configured.");

        public static string GetFloorTexturePath() =>
            Configuration["TextureConfig:FloorTexturePath"] ?? throw new Exception("Floor texture path is not configured.");

        public static string GetWallTexturePath() =>
            Configuration["TextureConfig:WallTexturePath"] ?? throw new Exception("Wall texture path is not configured.");

        public static float GetCameraHeight()
        {
            string heightStr = Configuration["Camera:Height"];
            if (!float.TryParse(heightStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float height))
            {
                throw new Exception("Invalid Camera:Height value in appsettings.json: " + heightStr);
            }
            Log.Information("Camera height read: {Height}", height);
            return height;
        }

        public static float GetLightHeight()
        {
            string lightHeightStr = Configuration["Camera:LightHeight"];
            if (!float.TryParse(lightHeightStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float lightHeight))
            {
                throw new Exception("Invalid Camera:LightHeight value in appsettings.json: " + lightHeightStr);
            }
            Log.Information("Light height read: {LightHeight}", lightHeight);
            return lightHeight;
        }

        public static float GetAngleOfDepression()
        {
            string angleOfDepressionStr = Configuration["Camera:AngleOfDepression"];
            if (!float.TryParse(angleOfDepressionStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float angleOfDepression))
            {
                throw new Exception("Invalid Camera:AngleOfDepression value in appsettings.json: " + angleOfDepressionStr);
            }
            Log.Information("Angle of depression read: {Angle}", angleOfDepression);
            return angleOfDepression;
        }

        public static float GetMouseSensivity()
        {
            string mouseSensivityStr = Configuration["Mouse:Sensivity"];
            if (!float.TryParse(mouseSensivityStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float mouseSensivity))
            {
                throw new Exception("Invalid Mouse:Sensivity value in appsettings.json: " + mouseSensivityStr);
            }
            Log.Information("Mouse sensitivity read: {Sensitivity}", mouseSensivity);
            return mouseSensivity;
        }

        public static float GetMovementSpeed()
        {
            string movementSpeedStr = Configuration["Movement:Speed"];
            if (!float.TryParse(movementSpeedStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float movementSpeed))
            {
                throw new Exception("Invalid Mouse:Sensivity value in appsettings.json: " + movementSpeedStr);
            }
            Log.Information("Movement speed read: {Speed}", movementSpeed);
            return movementSpeed;
        }
    }
}
