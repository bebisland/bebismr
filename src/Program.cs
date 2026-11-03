using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Raylib_cs;
using static Raylib_cs.Raylib;

internal static unsafe class Program
{
    private static void Main()
    {
        SetTraceLogLevel(TraceLogLevel.Error);

        InitAudioDevice();
        SetAudioStreamBufferSizeDefault(512);
        var audioStream = LoadAudioStream(48000, 32, 2);
        SetAudioStreamCallback(audioStream, &Callback);
        PlayAudioStream(audioStream);

        SetConfigFlags(ConfigFlags.TransparentWindow);
        InitWindow(360, 460, "Bebismr");
        SetExitKey(KeyboardKey.Null);
        SetTargetFPS(30);

        var bebisTex = LoadTexture("assets/bebis.png");

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

            EndDrawing();
        }

        UnloadAudioStream(audioStream);
        CloseAudioDevice();

        UnloadTexture(bebisTex);

        CloseWindow();
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void Callback(void* data, uint frames)
    {
        Mixer.Render((float*)data, (int)frames);
    }
}