using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Raylib_cs;
using static Raylib_cs.Raylib;

internal static unsafe class Program
{
    private static int Main(string[] args)
    {
        var headless = args.Contains("-d");
        var devices  = Input.OpenDevices();

        if (devices.Length == 0)
        {
            Console.Error.WriteLine("/dev/input is not authorized. Run with privileges.");
            return 1;
        }

        Native.DropPrivileges();

        SetTraceLogLevel(TraceLogLevel.Error);

        InitAudioDevice();
        SetAudioStreamBufferSizeDefault(512);
        var audioStream = LoadAudioStream(48000, 32, 2);
        SetAudioStreamCallback(audioStream, &Callback);
        PlayAudioStream(audioStream);

        if (headless)
        {
            var done = new ManualResetEventSlim();

            using var s1 = PosixSignalRegistration.Create
            (
                PosixSignal.SIGINT,
                c =>
                {
                    c.Cancel = true;
                    done.Set();
                }
            );

            using var s2 = PosixSignalRegistration.Create
            (
                PosixSignal.SIGTERM,
                c =>
                {
                    c.Cancel = true;
                    done.Set();
                }
            );

            done.Wait();
        }
        else
        {
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

            UnloadTexture(bebisTex);
        }

        UnloadAudioStream(audioStream);
        CloseAudioDevice();

        CloseWindow();

        return 0;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void Callback(void* data, uint frames)
    {
        Mixer.Render((float*)data, (int)frames);
    }
}