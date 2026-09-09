using System.Media;

namespace OpenOsk.Services;

/// <summary>
/// A short synthesised key click. Generated in memory at startup so no audio file ships with the app
/// and nothing is read from disk when a key is pressed.
/// </summary>
internal sealed class ClickSound : IDisposable
{
    private readonly SoundPlayer _player;

    public ClickSound()
    {
        _player = new SoundPlayer(new MemoryStream(Synthesize()));
        _player.Load();
    }

    public bool Enabled { get; set; } = true;

    public void Play()
    {
        if (!Enabled)
        {
            return;
        }

        try
        {
            _player.Play();
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or TimeoutException)
        {
            // No audio device, or the device is busy. Sound is decoration; never let it interrupt typing.
        }
    }

    public void Dispose() => _player.Dispose();

    /// <summary>16-bit mono PCM WAV: 22 ms of a 1.6 kHz tone with a sharp exponential decay.</summary>
    internal static byte[] Synthesize()
    {
        const int sampleRate = 22050;
        const double seconds = 0.022;
        const double frequency = 1600;
        var count = (int)(sampleRate * seconds);
        var data = new byte[count * 2];
        for (var i = 0; i < count; i++)
        {
            var t = i / (double)sampleRate;
            var envelope = Math.Exp(-t * 220);
            var sample = Math.Sin(2 * Math.PI * frequency * t) * envelope * 0.45;
            var value = (short)Math.Clamp(sample * short.MaxValue, short.MinValue, short.MaxValue);
            data[i * 2] = (byte)(value & 0xFF);
            data[(i * 2) + 1] = (byte)((value >> 8) & 0xFF);
        }

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true);
        writer.Write("RIFF"u8);
        writer.Write(36 + data.Length);
        writer.Write("WAVE"u8);
        writer.Write("fmt "u8);
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(sampleRate);
        writer.Write(sampleRate * 2);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write("data"u8);
        writer.Write(data.Length);
        writer.Write(data);
        writer.Flush();
        return stream.ToArray();
    }
}
