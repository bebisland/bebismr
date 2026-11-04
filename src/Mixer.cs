using static Raylib_cs.Raylib;

internal static unsafe class Mixer
{
    private const           int      Bank = 288;
    private const           int      Rate = 48000;
    public static           int      Impulse;
    private static readonly float[]? Rain;
    private static          int      _rainPos;
    private static readonly Sound[]  Sounds = new Sound[32];

    static Mixer()
    {
        LoadPacks("assets/packs");
        Rain = LoadWave48K("assets/rain.ogg");
    }

    public static void OnKey(ushort code, bool down)
    {
        if (!down || code >= Bank || !Pack.ResolvedDefinitions.TryGetValue(code, out var variants))
            return;

        var mouse = code >= 272;
        var vol   = mouse ? Config.Instance.Volume.Mouse / 100f * 5 : Config.Instance.Volume.Keyboard / 100f * 2;

        if (vol < .005f)
            return;

        var data = variants.Length == 1 ? variants[0] : variants[Random.Shared.Next(variants.Length)];
        Play(data, vol);
    }

    private static void Play(float[] data, float gain)
    {
        var index = -1;

        for (var i = 0; i < Sounds.Length; i++)
            if (Sounds[i].Definitions == null)
            {
                index = i;
                break;
            }

        Sounds[index] = new Sound { Definitions = data, Pos = 0, L = gain, R = gain };
    }

    public static void Render(float* o, int frames)
    {
        // Sounds
        new Span<float>(o, frames * 2).Clear();

        for (var s = 0; s < Sounds.Length; s++)
        {
            ref var sound = ref Sounds[s];

            if (sound.Definitions == null)
                continue;

            for (var i = 0; i < frames; i++)
            {
                var p = (int)sound.Pos;

                if (p >= sound.Definitions.Length - 1)
                {
                    sound.Definitions = null;
                    break;
                }

                var fraction = sound.Pos                             - p;
                var sample   = sound.Definitions[p] * (1 - fraction) + sound.Definitions[p + 1] * fraction;
                o[i                                 * 2] += sample * sound.L;
                o[i * 2 + 1]                             += sample * sound.R;
                sound.Pos                                += 1;
            }
        }

        // Rain
        var data  = Rain!;
        var count = data.Length / 2;
        var pos   = _rainPos;

        for (var i = 0; i < frames; i++)
        {
            if (pos == count)
                pos = 0;

            var volume = Config.Instance.Volume.Rain / 100f;

            o[i * 2] += data[pos * 2]                          * volume;
            o[i                  * 2 + 1] += data[pos * 2 + 1] * volume;
            pos++;
        }

        _rainPos = pos == count ? 0 : pos;
    }

    private static float[]? LoadWave48K(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException(path);

        var wave = LoadWave(path);

        if (wave.FrameCount == 0 || wave.Data == null || wave.Channels == 0 || wave.SampleRate == 0)
        {
            UnloadWave(wave);
            return null;
        }

        var samples = LoadWaveSamples(wave);

        if (samples == null)
        {
            UnloadWave(wave);
            return null;
        }

        try
        {
            var sourceFrames = (int)wave.FrameCount;
            var channels     = (int)wave.Channels;
            var sourceRate   = (int)wave.SampleRate;
            var outputFrames = Math.Max(1, (int)((long)sourceFrames * Rate / sourceRate));
            var output       = new float[outputFrames * 2];
            var step         = (double)sourceRate / Rate;

            for (var i = 0; i < outputFrames; i++)
            {
                var position = i * step;
                var p        = Math.Min((int)position, sourceFrames - 1);
                var q        = Math.Min(p                           + 1, sourceFrames - 1);
                var fraction = (float)(position - p);
                var left0    = samples[p * channels];
                var left1    = samples[q * channels];
                var right0   = channels > 1 ? samples[p * channels + 1] : left0;
                var right1   = channels > 1 ? samples[q * channels + 1] : left1;

                output[i * 2] = left0 + (left1 - left0)                          * fraction;
                output[i * 2                   + 1] = right0 + (right1 - right0) * fraction;
            }

            return output;
        }
        finally
        {
            UnloadWaveSamples(samples);
            UnloadWave(wave);
        }
    }

    private static void LoadPacks(string root)
    {
        if (!Directory.Exists(root))
            return;

        Pack? keyboard = null, mouse = null;
        var   dirs     = Directory.GetDirectories(root);

        Array.Sort(dirs, StringComparer.Ordinal);

        foreach (var dir in dirs)
        {
            var config = Path.Combine(dir, "config.json");

            if (!File.Exists(config))
                continue;

            var pack = Pack.Read(config, dir);

            if (pack == null)
                continue;

            if (pack.Mouse)
                mouse ??= pack;
            else
                keyboard ??= pack;
        }

        if (keyboard != null)
            LoadKeyboard(keyboard);

        if (mouse != null)
            LoadMouse(mouse);
    }

    private static void LoadKeyboard(Pack pack)
    {
        var audio = LoadWave48K(Path.Combine(pack.Dir, pack.AudioFile));

        if (audio == null)
            throw new NotSupportedException(pack.AudioFile);

        var generic = Variants(audio, pack.Definitions, "key") ?? Variants(audio, pack.Definitions, "Key");

        foreach (var (code, name) in Pack.KeyNames)
        {
            var variants = Variants(audio, pack.Definitions, name);

            if (variants != null)
                Pack.ResolvedDefinitions[code] = variants;
        }

        if (generic == null)
            return;

        for (ushort code = 0; code < 272; code++)
            Pack.ResolvedDefinitions.TryAdd(code, generic);
    }

    private static void LoadMouse(Pack pack)
    {
        var audio = LoadWave48K(Path.Combine(pack.Dir, pack.AudioFile));

        if (audio == null)
            throw new NotSupportedException(pack.AudioFile);

        foreach (var (code, name) in Pack.MouseNames)
        {
            var variants = Variants(audio, pack.Definitions, name);

            if (variants != null)
                Pack.ResolvedDefinitions[code] = variants;
        }
    }

    private static float[][]? Variants(float[]                                              audio,
                                       Dictionary<string, List<(double Start, double End)>> definitions,
                                       string                                               name)
    {
        if (!definitions.TryGetValue(name, out var timing))
            return null;

        var frames = audio.Length / 2;
        var list   = new List<float[]>(timing.Count);

        foreach (var (start, end) in timing)
        {
            var from = Math.Clamp((int)Math.Round(start * Rate / 1000.0), 0, frames - 1);
            var to   = Math.Clamp((int)Math.Round(end   * Rate / 1000.0), from      + 1, frames);
            var data = new float[(to                                                - from) * 2];

            Array.Copy(audio, from * 2, data, 0, data.Length);
            list.Add(data);
        }

        return list.Count == 0 ? null : [.. list];
    }
}