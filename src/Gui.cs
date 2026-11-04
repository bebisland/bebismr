using System.Numerics;
using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;
using static ImGuiNET.ImGui;
using static Raylib_cs.ConfigFlags;
using static Raylib_cs.Raylib;

internal static unsafe class Gui
{
    private static readonly string[] SettingNames = ["Klavye", "Mouse", "Yagmur"];
    private static readonly string[] SettingIconFiles = ["assets/keyboard.png", "assets/mouse.png", "assets/rain.png"];
    private static readonly Texture2D[] SettingIconTextures = new Texture2D[SettingIconFiles.Length];

    private static readonly Vector4 Gold  = new(230 / 255f, 178 / 255f, 41 / 255f, 1f);
    private static readonly Vector4 Fill  = new(176 / 255f, 136 / 255f, 31 / 255f, 1f);
    private static readonly Vector4 Track = new(58  / 255f, 45  / 255f, 10 / 255f, 1f);

    public static void Start()
    {
        SetConfigFlags(TransparentWindow | VSyncHint);
        InitWindow(360, 460, "Bebismr");
        SetExitKey(KeyboardKey.Null);
        rlImGui.Setup();

        GetIO().NativePtr->IniFilename = null;

        var bebisTex    = LoadTexture("assets/bebis.png");
        var settingsTex = LoadTexture("assets/settings.png");

        for (var i = 0; i < SettingIconFiles.Length; i++)
            SettingIconTextures[i] = LoadTexture(SettingIconFiles[i]);

        var showSettings = false;

        while (!WindowShouldClose())
        {
            BeginDrawing();
            ClearBackground(new Color(10, 10, 0, 240));

            var center = GetScreenCenter();

            var scale  = 250f            / MathF.Max(bebisTex.Width, bebisTex.Height);
            var width  = bebisTex.Width  * scale;
            var height = bebisTex.Height * scale;

            DrawTexturePro
            (
                bebisTex,
                new Rectangle(0,        0,        bebisTex.Width, bebisTex.Height),
                new Rectangle(center.X, center.Y, width,          height),
                new Vector2(width * .5f, height * .5f),
                0,
                Color.White
            );

            rlImGui.Begin();

            if (!Raylib.IsWindowFocused())
            {
                showSettings = false;
            }
            else
            {
                SetNextWindowPos(new Vector2(GetScreenWidth() - 58, 10), ImGuiCond.Always);
                SetNextWindowBgAlpha(0);

                const ImGuiWindowFlags buttonFlags = ImGuiWindowFlags.NoDecoration
                                                   | ImGuiWindowFlags.NoMove
                                                   | ImGuiWindowFlags.NoBackground
                                                   | ImGuiWindowFlags.NoSavedSettings
                                                   | ImGuiWindowFlags.AlwaysAutoResize;

                Begin("settings-button", buttonFlags);

                PushStyleColor(ImGuiCol.Button,        Vector4.Zero);
                PushStyleColor(ImGuiCol.ButtonHovered, Vector4.Zero);
                PushStyleColor(ImGuiCol.ButtonActive,  Vector4.Zero);

                if (rlImGui.ImageButtonSize("settings", settingsTex, new Vector2(28, 28)))
                    showSettings = !showSettings;

                PopStyleColor(3);

                End();

                if (showSettings)
                {
                    SetNextWindowPos(new Vector2(16,                     54), ImGuiCond.Always);
                    SetNextWindowSize(new Vector2(GetScreenWidth() - 32, 0), ImGuiCond.Always);
                    SetNextWindowBgAlpha(.92f);

                    const ImGuiWindowFlags panelFlags = ImGuiWindowFlags.NoResize
                                                      | ImGuiWindowFlags.NoMove
                                                      | ImGuiWindowFlags.NoCollapse
                                                      | ImGuiWindowFlags.NoTitleBar
                                                      | ImGuiWindowFlags.NoSavedSettings;

                    Begin("Ses", panelFlags);
                    PushItemWidth(-1);

                    for (var i = 0; i < SettingNames.Length; i++)
                    {
                        rlImGui.ImageSize(SettingIconTextures[i], 18, 18);
                        SameLine();

                        var ratio = i switch
                        {
                            0 => Mixer.KeyVolume,
                            1 => Mixer.ClickVolume,
                            _ => Mixer.RainVolume
                        };

                        if (VolumeSlider(i, ratio, out var value))
                            switch (i)
                            {
                                case 0:
                                    Mixer.KeyVolume = value;
                                    break;
                                case 1:
                                    Mixer.ClickVolume = value;
                                    break;
                                default:
                                    Mixer.RainVolume = value;
                                    break;
                            }
                    }

                    PopItemWidth();
                    End();
                }
            }

            rlImGui.End();

            EndDrawing();
        }

        UnloadTexture(bebisTex);

        rlImGui.Shutdown();
        CloseWindow();
    }

    private static bool VolumeSlider(int i, float ratio, out float value)
    {
        const float height       = 20f;
        const float trackHeight  = 16f;
        const float handleRadius = 10f;
        const float trackRadius  = trackHeight * .5f;

        var origin = GetCursorScreenPos();
        var width  = CalcItemWidth();
        var span   = MathF.Max(1f, width - handleRadius * 2f);
        var midY   = origin.Y + height      * .5f;
        var top    = midY     - trackHeight * .5f;
        var bottom = midY     + trackHeight * .5f;

        value = ratio;

        InvisibleButton($"##{SettingNames[i]}", new Vector2(width, height));

        if (IsItemActive() && IsMouseDown(ImGuiMouseButton.Left))
        {
            var mouseX = GetMousePos().X;
            var t      = Math.Clamp((mouseX - origin.X - handleRadius) / span, 0f, 1f);
            value = MathF.Round(t * 100f) / 100f;
        }

        var draw   = GetWindowDrawList();
        var center = new Vector2(origin.X + handleRadius + span * value, midY);

        draw.AddRectFilled
            (origin with { Y = top }, new Vector2(origin.X + width, bottom), GetColorU32(Track), trackRadius);

        var fillWidth = MathF.Min(width, handleRadius * 2f + span * value);

        if (fillWidth > .5f)
            draw.AddRectFilled
            (
                origin with { Y = top },
                new Vector2(origin.X + fillWidth, bottom),
                GetColorU32(Fill),
                trackRadius,
                ImDrawFlags.RoundCornersAll
            );

        draw.AddCircleFilled(center, handleRadius, GetColorU32(Gold), 32);

        var text = $"{(int)MathF.Round(value * 100f)}";
        var size = CalcTextSize(text);
        var pos  = new Vector2(origin.X + (width - size.X) * .5f, midY - size.Y * .5f);

        draw.AddText(pos, GetColorU32(ImGuiCol.Text), text);

        return MathF.Abs(value - ratio) > .0001f;
    }
}