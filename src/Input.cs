internal static class Input
{
    public static FileStream[] OpenDevices()
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
}