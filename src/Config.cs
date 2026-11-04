using Tomlyn;
using static Raylib_cs.Raylib;

public class Config
{
    public static readonly Config Instance;

    static Config()
    {
        if (File.Exists(FilePath))
        {
            var tomlContent = File.ReadAllText(FilePath);
            Instance = TomlSerializer.Deserialize(tomlContent, TomlContext.Default.Config)!;
        }

        if (Instance == null)
        {
            Instance = new Config();
            Save();
        }
    }

    private static string FilePath => Path.Combine(Native.Home, ".config", "bebisland", "bebismr", "config.toml");

    public VolumeConfig Volume { get; set; } = new();
    public WindowConfig Window { get; set; } = new();
    public RenderConfig Render { get; set; } = new();

    public static void Save()
    {
        Instance.Window.SizeX = GetScreenWidth();
        Instance.Window.SizeY = GetScreenHeight();

        var tomlContent = TomlSerializer.Serialize(Instance, TomlContext.Default.Config);
        File.WriteAllText(FilePath, tomlContent);
    }

    public class VolumeConfig
    {
        public int Keyboard { get; set; } = 35;
        public int Mouse    { get; set; } = 50;
        public int Rain     { get; set; } = 20;
    }

    public class WindowConfig
    {
        public int SizeX { get; set; } = 360;
        public int SizeY { get; set; } = 460;
    }

    public class RenderConfig
    {
        public int    Fps        { get; set; } = 60;
        public string Background { get; set; } = "10, 10, 0, 240";
    }
}