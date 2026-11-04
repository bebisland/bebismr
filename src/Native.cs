using System.Runtime.InteropServices;

internal static unsafe partial class Native
{
    public static string Home = "/tmp";

    public static void DropPrivileges()
    {
        Home = Environment.GetEnvironmentVariable("HOME") ?? "/tmp";

        var sudoUid = Environment.GetEnvironmentVariable("SUDO_UID");

        if (sudoUid is null || !uint.TryParse(sudoUid, out var uid))
            return;

        var gid = uint.TryParse(Environment.GetEnvironmentVariable("SUDO_GID"), out var g) ? g : uid;

        var (cursorSize, cursorTheme) = InvokerCursor();

        if (setgroups(0, null) != 0 || setgid(gid) != 0 || setuid(uid) != 0)
            Environment.Exit(2);

        Home = HomeOf(Environment.GetEnvironmentVariable("SUDO_USER")) ?? Home;

        var runtimeDir = $"/run/user/{uid}";

        setenv("HOME",                     Home,                              1);
        setenv("XDG_RUNTIME_DIR",          runtimeDir,                        1);
        setenv("PULSE_SERVER",             $"unix:{runtimeDir}/pulse/native", 1); // PipeWire-pulse / PulseAudio
        setenv("DBUS_SESSION_BUS_ADDRESS", $"unix:path={runtimeDir}/bus",     1);

        if (cursorSize is not null)
            setenv("XCURSOR_SIZE", cursorSize, 1);

        if (cursorTheme is not null)
            setenv("XCURSOR_THEME", cursorTheme, 1);

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

    private static (string? Size, string? Theme) InvokerCursor()
    {
        var size  = Environment.GetEnvironmentVariable("XCURSOR_SIZE");
        var theme = Environment.GetEnvironmentVariable("XCURSOR_THEME");
        var pid   = getppid();

        if (size is not null && theme is not null)
            return (size, theme);

        for (var depth = 0; depth < 4 && pid > 1; depth++)
        {
            try
            {
                foreach (var entry in File.ReadAllText($"/proc/{pid}/environ").Split('\0'))
                    if (size is null && entry.StartsWith("XCURSOR_SIZE=", StringComparison.Ordinal))
                        size = entry["XCURSOR_SIZE=".Length..];
                    else if (theme is null && entry.StartsWith("XCURSOR_THEME=", StringComparison.Ordinal))
                        theme = entry["XCURSOR_THEME=".Length..];
            }
            catch
            {
                // not readable
            }

            if (size is not null && theme is not null)
                break;

            pid = ParentOf(pid);
        }

        return (size, theme);
    }

    private static int ParentOf(int pid)
    {
        try
        {
            var stat   = File.ReadAllText($"/proc/{pid}/stat");
            var fields = stat[(stat.LastIndexOf(')') + 2)..].Split(' ');

            return int.TryParse(fields[1], out var parent) ? parent : 0;
        }
        catch
        {
            return 0;
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

    [LibraryImport("libc.so.6")]
    private static partial int getppid();

    [LibraryImport("libc.so.6", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    private static partial void setenv(string name, string value, int overwrite);
}