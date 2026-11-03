using System.Runtime.InteropServices;

internal static unsafe partial class Native
{
    private static string _home = "/tmp";

    public static void DropPrivileges()
    {
        _home = Environment.GetEnvironmentVariable("HOME") ?? "/tmp";

        var sudoUid = Environment.GetEnvironmentVariable("SUDO_UID");

        if (sudoUid is null || !uint.TryParse(sudoUid, out var uid))
            return;

        var gid = uint.TryParse(Environment.GetEnvironmentVariable("SUDO_GID"), out var g) ? g : uid;

        if (setgroups(0, null) != 0 || setgid(gid) != 0 || setuid(uid) != 0)
            Environment.Exit(2);

        _home = HomeOf(Environment.GetEnvironmentVariable("SUDO_USER")) ?? _home;

        var runtimeDir = $"/run/user/{uid}";

        setenv("HOME",                     _home,                             1);
        setenv("XDG_RUNTIME_DIR",          runtimeDir,                        1);
        setenv("PULSE_SERVER",             $"unix:{runtimeDir}/pulse/native", 1); // PipeWire-pulse / PulseAudio
        setenv("DBUS_SESSION_BUS_ADDRESS", $"unix:path={runtimeDir}/bus",     1);

        if (Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") is not null)
            return;

        try
        {
            foreach (var file in Directory.GetFiles(runtimeDir, "wayland-*"))
            {
                if (file.EndsWith(".lock", StringComparison.Ordinal))
                    continue;

                setenv("WAYLAND_DISPLAY",  Path.GetFileName(file), 1);
                setenv("XDG_SESSION_TYPE", "wayland",              1);
                break;
            }
        }
        catch
        {
            // ignored
        }
    }

    private static string? HomeOf(string? user)
    {
        if (user == null)
            return null;

        var home = Environment.GetEnvironmentVariable("HOME") ?? "/tmp";

        try
        {
            foreach (var line in File.ReadLines("/etc/passwd"))
            {
                var parts = line.Split(':');

                if (parts.Length > 5 && parts[0] == user)
                    return parts[5];
            }

            return home;
        }
        catch
        {
            return home;
        }
    }

    // ReSharper disable InconsistentNaming
    [LibraryImport("libc.so.6", SetLastError = true)]
    private static partial int setgroups(nuint size, uint* list);

    [LibraryImport("libc.so.6", SetLastError = true)]
    private static partial int setgid(uint gid);

    [LibraryImport("libc.so.6", SetLastError = true)]
    private static partial int setuid(uint uid);

    [LibraryImport("libc.so.6", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    private static partial void setenv(string name, string value, int overwrite);
}