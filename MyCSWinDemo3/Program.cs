namespace WaveOutDemo;

using Baksteen.Waves;

class Program
{
    static async Task Main(string[] args)
    {
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

        var organ = new Organ(48000, 8);
        player.StereoFloatRender = organ.Render;
        var keyScanner = new KeyScanner();
        var keyPiano = new KeyPiano();

        Console.WriteLine("play the organ!. Press escape to quit.");

        await player.Start();

        while (true)
        {
            var scanEvents = keyScanner.Scan();
            if (scanEvents.Any(x => x.Key == KeyScanner.VirtualKey.ESCAPE)) break;
            foreach (var noteEvent in keyPiano.ToNoteEvents(scanEvents))
            {
                organ.QueueNoteEvent(noteEvent.note, noteEvent.isOn);
            }
            await Task.Delay(10);
        }

        await player.Stop();
    }
}
