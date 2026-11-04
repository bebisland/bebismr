using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Raylib_cs;
using static Raylib_cs.Raylib;

internal static unsafe class Program
{
    public static bool Quit;

    private static void Main(string[] args)
    {
        var headless = args.Contains("-d");

        Input.Start();
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
            Gui.Start();
        }

        Quit = true;

        UnloadAudioStream(audioStream);
        CloseAudioDevice();
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void Callback(void* data, uint frames)
    {
        Mixer.Render((float*)data, (int)frames);
    }
}