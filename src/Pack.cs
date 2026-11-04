using System.Text.Json;

internal sealed class Pack
{
    public static readonly Dictionary<ushort, string> KeyNames = new()
    {
        [1]   = "Escape",
        [2]   = "Digit1",
        [3]   = "Digit2",
        [4]   = "Digit3",
        [5]   = "Digit4",
        [6]   = "Digit5",
        [7]   = "Digit6",
        [8]   = "Digit7",
        [9]   = "Digit8",
        [10]  = "Digit9",
        [11]  = "Digit0",
        [12]  = "Minus",
        [13]  = "Equal",
        [14]  = "Backspace",
        [15]  = "Tab",
        [16]  = "KeyQ",
        [17]  = "KeyW",
        [18]  = "KeyE",
        [19]  = "KeyR",
        [20]  = "KeyT",
        [21]  = "KeyY",
        [22]  = "KeyU",
        [23]  = "KeyI",
        [24]  = "KeyO",
        [25]  = "KeyP",
        [26]  = "BracketLeft",
        [27]  = "BracketRight",
        [28]  = "Enter",
        [29]  = "ControlLeft",
        [30]  = "KeyA",
        [31]  = "KeyS",
        [32]  = "KeyD",
        [33]  = "KeyF",
        [34]  = "KeyG",
        [35]  = "KeyH",
        [36]  = "KeyJ",
        [37]  = "KeyK",
        [38]  = "KeyL",
        [39]  = "Semicolon",
        [40]  = "Quote",
        [41]  = "Backquote",
        [42]  = "ShiftLeft",
        [43]  = "Backslash",
        [44]  = "KeyZ",
        [45]  = "KeyX",
        [46]  = "KeyC",
        [47]  = "KeyV",
        [48]  = "KeyB",
        [49]  = "KeyN",
        [50]  = "KeyM",
        [51]  = "Comma",
        [52]  = "Period",
        [53]  = "Slash",
        [54]  = "ShiftRight",
        [55]  = "NumpadMultiply",
        [56]  = "AltLeft",
        [57]  = "Space",
        [58]  = "CapsLock",
        [59]  = "F1",
        [60]  = "F2",
        [61]  = "F3",
        [62]  = "F4",
        [63]  = "F5",
        [64]  = "F6",
        [65]  = "F7",
        [66]  = "F8",
        [67]  = "F9",
        [68]  = "F10",
        [69]  = "NumLock",
        [70]  = "ScrollLock",
        [71]  = "Numpad7",
        [72]  = "Numpad8",
        [73]  = "Numpad9",
        [74]  = "NumpadSubtract",
        [75]  = "Numpad4",
        [76]  = "Numpad5",
        [77]  = "Numpad6",
        [78]  = "NumpadAdd",
        [79]  = "Numpad1",
        [80]  = "Numpad2",
        [81]  = "Numpad3",
        [82]  = "Numpad0",
        [83]  = "NumpadDecimal",
        [86]  = "IntlBackslash",
        [87]  = "F11",
        [88]  = "F12",
        [96]  = "NumpadEnter",
        [97]  = "ControlRight",
        [98]  = "NumpadDivide",
        [99]  = "PrintScreen",
        [100] = "AltRight",
        [102] = "Home",
        [103] = "ArrowUp",
        [104] = "PageUp",
        [105] = "ArrowLeft",
        [106] = "ArrowRight",
        [107] = "End",
        [108] = "ArrowDown",
        [109] = "PageDown",
        [110] = "Insert",
        [111] = "Delete",
        [113] = "Mute",
        [114] = "VolumeDown",
        [115] = "VolumeUp",
        [119] = "Pause",
        [125] = "MetaLeft",
        [126] = "MetaRight",
        [127] = "ContextMenu"
    };

    public static readonly Dictionary<ushort, string> MouseNames = new()
    {
        [272] = "MouseLeft",
        [273] = "MouseRight",
        [274] = "MouseMiddle",
        [275] = "Mouse4",
        [276] = "Mouse5"
    };

    public static readonly Dictionary<ushort, float[][]> ResolvedDefinitions = new();

    public required string                                               AudioFile;
    public required Dictionary<string, List<(double Start, double End)>> Definitions;
    public required string                                               Dir;
    public required bool                                                 Mouse;

    public static Pack? Read(string configPath, string dir)
    {
        try
        {
            using var doc  = JsonDocument.Parse(File.ReadAllText(configPath));
            var       root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
                return null;

            if (!root.TryGetProperty("audio_file", out var audio) || audio.ValueKind != JsonValueKind.String)
                return null;

            var audioFile = audio.GetString();

            if (string.IsNullOrWhiteSpace(audioFile))
                return null;

            if (!root.TryGetProperty("definitions", out var definitions)
             || definitions.ValueKind != JsonValueKind.Object)
                return null;

            var defs = new Dictionary<string, List<(double Start, double End)>>(StringComparer.Ordinal);

            foreach (var property in definitions.EnumerateObject())
            {
                if (!property.Value.TryGetProperty("timing", out var timing) || timing.ValueKind != JsonValueKind.Array)
                    continue;

                var segments = new List<(double, double)>();

                foreach (var entry in timing.EnumerateArray().Where(entry => entry.ValueKind == JsonValueKind.Array))
                {
                    double start = 0, end = 0;
                    var    index = 0;

                    foreach (var value in entry.EnumerateArray())
                    {
                        if (!value.TryGetDouble(out var number))
                            continue;

                        if (index == 0)
                        {
                            start = number;
                        }
                        else if (index == 1)
                        {
                            end = number;
                            break;
                        }

                        index++;
                    }

                    if (end > start)
                        segments.Add((start, end));
                }

                if (segments.Count > 0)
                    defs[property.Name] = segments;
            }

            if (defs.Count == 0)
                return null;

            var isMouse = defs.ContainsKey("MouseLeft")
                       || defs.ContainsKey("MouseRight")
                       || defs.ContainsKey("MouseMiddle");

            if (!root.TryGetProperty("soundpack_type", out var type) || type.ValueKind != JsonValueKind.String)
                return new Pack { Dir = dir, AudioFile = audioFile, Definitions = defs, Mouse = isMouse };

            if (string.Equals(type.GetString(), "Mouse", StringComparison.OrdinalIgnoreCase))
                isMouse = true;
            else if (string.Equals(type.GetString(), "Keyboard", StringComparison.OrdinalIgnoreCase))
                isMouse = false;

            return new Pack { Dir = dir, AudioFile = audioFile, Definitions = defs, Mouse = isMouse };
        }
        catch (Exception e)
        {
            throw new NotSupportedException($"{configPath}: {e.Message}");
        }
    }
}