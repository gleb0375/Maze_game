using Microsoft.Extensions.Configuration;
using System.Globalization;
using Serilog;

namespace semestral_work.Config
{

    /// <summary>
    /// Poskytuje centralizovaný přístup ke konfiguraci aplikace načtené z appsettings.json.
    /// Obsahuje metody pro čtení cest k souborům, shaderům, texturám a dalších parametrů jako výška kamery, rychlost pohybu apod.
    /// </summary>
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

        public static string GetCeilingTexturePath() =>
          Configuration["TextureConfig:CeillingTexturePath"] ?? throw new Exception("Ceiling texture path is not configured.");

        public static float GetCameraHeight()
        {
            string heightStr = Configuration["Camera:Height"]
                ?? throw new Exception("Camera:Height is not configured in appsettings.json.");

            if (!float.TryParse(heightStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float height))
            {
                throw new Exception("Invalid Camera:Height value in appsettings.json: " + heightStr);
            }

            Log.Information("Camera height read: {Height}", height);
            return height;
        }

        public static float GetLightHeight()
        {
            string lightHeightStr = Configuration["Camera:LightHeight"]
                ?? throw new Exception("Camera:LightHeight is not configured in appsettings.json.");

            if (!float.TryParse(lightHeightStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float lightHeight))
            {
                throw new Exception("Invalid Camera:LightHeight value in appsettings.json: " + lightHeightStr);
            }

            Log.Information("Light height read: {LightHeight}", lightHeight);
            return lightHeight;
        }

        public static float GetAngleOfDepression()
        {
            string angleOfDepressionStr = Configuration["Camera:AngleOfDepression"]
                ?? throw new Exception("Camera:AngleOfDepression is not configured in appsettings.json.");

            if (!float.TryParse(angleOfDepressionStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float angleOfDepression))
            {
                throw new Exception("Invalid Camera:AngleOfDepression value in appsettings.json: " + angleOfDepressionStr);
            }

            Log.Information("Angle of depression read: {Angle}", angleOfDepression);
            return angleOfDepression;
        }

        public static float GetMouseSensivity()
        {
            string mouseSensivityStr = Configuration["Mouse:Sensivity"]
                ?? throw new Exception("Mouse:Sensivity is not configured in appsettings.json.");

            if (!float.TryParse(mouseSensivityStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float mouseSensivity))
            {
                throw new Exception("Invalid Mouse:Sensivity value in appsettings.json: " + mouseSensivityStr);
            }

            Log.Information("Mouse sensitivity read: {Sensitivity}", mouseSensivity);
            return mouseSensivity;
        }

        public static float GetMovementSpeed()
        {
            string movementSpeedStr = Configuration["Movement:Speed"]
                ?? throw new Exception("Movement:Speed is not configured in appsettings.json.");

            if (!float.TryParse(movementSpeedStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float movementSpeed))
            {
                throw new Exception("Invalid Movement:Speed value in appsettings.json: " + movementSpeedStr);
            }

            Log.Information("Movement speed read: {Speed}", movementSpeed);
            return movementSpeed;
        }

        public static float GetLightCutoffDeg()
        {
            string cutoffStr = Configuration["Light:CutoffDeg"]
                ?? throw new Exception("Light:CutoffDeg is not configured in appsettings.json.");

            if (!float.TryParse(cutoffStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float cutoff))
            {
                throw new Exception("Invalid Light:CutoffDeg value in appsettings.json: " + cutoffStr);
            }

            Log.Information("Light cutoff read: {CutoffDeg}", cutoff);
            return cutoff;
        }

        public static float GetLightRange()
        {
            string rangeStr = Configuration["Light:Range"]
                ?? throw new Exception("Light:Range is not configured in appsettings.json.");

            if (!float.TryParse(rangeStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float range))
            {
                throw new Exception("Invalid Light:Range value in appsettings.json: " + rangeStr);
            }

            Log.Information("Light range read: {Range}", range);
            return range;
        }

        public static string GetMiniMapVertexShaderPath()
        {
            string? path = Configuration["MiniMapConfig:VertexShaderPath"];
            if (string.IsNullOrWhiteSpace(path))
                throw new Exception("MiniMapConfig:MiniMapVertexShaderPath is not configured in appsettings.json.");

            Log.Information("MiniMap vertex shader path: {Path}", path);
            return path;
        }

        public static string GetMiniMapFragmentShaderPath()
        {
            string? path = Configuration["MiniMapConfig:FragmentShaderPath"];
            if (string.IsNullOrWhiteSpace(path))
                throw new Exception("MiniMapConfig:MiniMapFragmentShaderPath is not configured in appsettings.json.");

            Log.Information("MiniMap fragment shader path: {Path}", path);
            return path;
        }

        public static int GetMiniMapSizeInPixels()
        {
            string? sizeStr = Configuration["MiniMapConfig:SizeInPx"];
            if (!int.TryParse(sizeStr, out int size))
                throw new Exception("Invalid MiniMapConfig:SizeInPx value in appsettings.json: " + sizeStr);

            Log.Information("MiniMap size in pixels: {Size}", size);
            return size;
        }

        public static float GetMiniMapViewRadius()
        {
            string? radiusStr = Configuration["MiniMapConfig:ViewRadius"];
            if (!float.TryParse(radiusStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float radius))
                throw new Exception("Invalid MiniMapConfig:ViewRadius value in appsettings.json: " + radiusStr);

            Log.Information("MiniMap view radius: {Radius}", radius);
            return radius;
        }

        public static float GetMiniMapArrowSize()
        {
            string? arrowSizeStr = Configuration["MiniMapConfig:ArrowSize"];
            if (!float.TryParse(arrowSizeStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float arrowSize))
                throw new Exception("Invalid MiniMapConfig:ArrowSize value in appsettings.json: " + arrowSizeStr);

            Log.Information("MiniMap arrow size: {ArrowSize}", arrowSize);
            return arrowSize;
        }

        public static string GetAppleModelPath()
        {
            string? path = Configuration["CollectableItems:applePath"];
            if (string.IsNullOrWhiteSpace(path))
                throw new Exception("CollectableItems:applePath is not configured in appsettings.json.");

            Log.Information("Apple model path: {Path}", path);
            return path;
        }

        public static string GetAppleVertexShaderPath()
        {
            string? path = Configuration["CollectableItems:AppleShaderVertex"];
            if (string.IsNullOrWhiteSpace(path))
                throw new Exception("CollectableItems:AppleShaderVertex is not configured in appsettings.json.");

            Log.Information("Apple vertex shader path: {Path}", path);
            return path;
        }

        public static string GetAppleFragmentShaderPath()
        {
            string? path = Configuration["CollectableItems:AppleShaderFragment"];
            if (string.IsNullOrWhiteSpace(path))
                throw new Exception("CollectableItems:AppleShaderFragment is not configured in appsettings.json.");

            Log.Information("Apple fragment shader path: {Path}", path);
            return path;
        }

        public static string GetCarModelPath()
        {
            string? path = Configuration["Cars:PorschePath"];
            if (string.IsNullOrWhiteSpace(path))
                throw new Exception("Cars:PorschePath missing.");

            Log.Information("Car model path: {Path}", path);
            return path;
        }

        public static string GetCarVertexShaderPath()
        {
            string? path = Configuration["Cars:PorscheShaderVertex"];
            if (string.IsNullOrWhiteSpace(path))
                throw new Exception("Cars:PorsheShaderVertex is not configured in appsettings.json.");

            Log.Information("Car vertex shader path: {Path}", path);
            return path;
        }

        public static string GetCarFragmentShaderPath()
        {
            string? path = Configuration["Cars:PorscheShaderFragment"];
            if (string.IsNullOrWhiteSpace(path))
                throw new Exception("Cars:PorsheShaderFragment is not configured in appsettings.json.");

            Log.Information("Car fragment shader path: {Path}", path);
            return path;
        }
    }
}
