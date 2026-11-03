using static Raylib_cs.Raylib;

internal static unsafe class Mixer
{
    private const int Rate = 48000;

    private const           float    RainVolume = 0.35f;
    private static readonly float[]? Rain;
    private static          int      _rainPos;

    static Mixer()
    {
        Rain = LoadWave48K("assets/rain.wav");
    }

    public static void Render(float* o, int frames)
    {
        var data  = Rain!;
        var count = data.Length / 2;
        var pos   = _rainPos;

        for (var i = 0; i < frames; i++)
        {
            if (pos == count)
                pos = 0;

            o[i * 2] += data[pos * 2]                          * RainVolume;
            o[i                  * 2 + 1] += data[pos * 2 + 1] * RainVolume;
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
}