using CSWinWasapiDemo;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var probedDevices = AudioOut.ProbeDevices();
        var defaultDeviceId = AudioOut.DefaultDeviceId();

        if (defaultDeviceId == null)
        {
            throw new InvalidOperationException("no default audio device found");
        }

        var player = new AudioOut(
            defaultDeviceId,
            new AudioOut.AudioConfig
            {
                Frequency = 48000,
                Channels = 2,
                Format = AudioOut.SampleFormat.FmtFloat
            },
            AudioOut.ShareMode.Shared
        );

        float phase = 0;
        float advance = (240 * MathF.Tau / 48000);

        player.StereoFloat32Render = chunk =>
        {
            for (int s = 0; s < chunk.Length; s++)
            {
                chunk[s].Left = 0.1f * MathF.Sin(phase);
                chunk[s].Right = 0.1f * MathF.Sin(phase);
                phase = (phase + advance) % MathF.Tau;
            }
        };

        await player.Start();
        Console.ReadLine();
        await player.Stop();
    }
}
