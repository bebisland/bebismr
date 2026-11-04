using System.Runtime.InteropServices;

internal static class Input
{
    public static void Start()
    {
        var devices = OpenDevices();

        if (devices.Length == 0)
        {
            Console.Error.WriteLine("/dev/input is not authorized. Run with privileges.");
            Environment.Exit(1);
        }

        foreach (var device in devices)
        {
            var name = Path.GetFileName(device.Name);
            new Thread(() => Read(device)) { IsBackground = true, Name = name }.Start();
        }
    }

    private static FileStream[] OpenDevices()
    {
        var files = Directory.GetFiles("/dev/input", "event*");

        Array.Sort(files, StringComparer.Ordinal);

        var devices = new List<FileStream>(files.Length);

        try
        {
            devices.AddRange
                (files.Select(path => new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)));

            return [.. devices];
        }
        catch
        {
            return [];
        }
    }

    private static void Read(FileStream device)
    {
        var buffer = new byte[64 * 24];

        while (!Program.Quit)
        {
            int bytesRead;

            try
            {
                bytesRead = device.Read(buffer, 0, buffer.Length);
            }
            catch
            {
                return;
            }

            if (bytesRead <= 0)
                return;

            var length = bytesRead / 24 * 24;

            foreach (ref readonly var ev in MemoryMarshal.Cast<byte, InputEvent>(buffer.AsSpan(0, length)))
            {
                if (ev.Type != 1 || ev.Value == 2)
                    continue;

                var code = ev.Code;
                if (code is >= 256 and (< 272 or > 279))
                    continue;

                var down = ev.Value == 1;
                Mixer.OnKey(code, down);

                if (down)
                    Interlocked.Increment(ref Mixer.Impulse);
            }
        }
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct InputEvent
{
    public long   Sec;
    public long   Usec;
    public ushort Type;
    public ushort Code;
    public int    Value;
}