namespace WaveOutDemo;
#if NOOIT
public class Program
{

#if NEVER
    private static void MyMono8Filler(Memory<SampleMono8> memory)
    {
        var span = memory.Span;

        for (int i = 0; i < span.Length; i++)
        {
            sbyte sample = (sbyte)(Math.Sin(phase) * 127);
            double vibrato = Math.Sin(slowPhase) * 10.0;

            phase += (440.0 + vibrato) * Math.Tau / sampleRate;
            slowPhase += 5.0 * Math.Tau / sampleRate;

            span[i].Mono = sample;
        }
    }

    private static void MyMono16Filler(Memory<SampleMono16> memory)
    {
        var span = memory.Span;

        for (int i = 0; i < span.Length; i++)
        {
            short sample = (short)(Math.Sin(phase) * 32767);
            double vibrato = Math.Sin(slowPhase) * 10.0;

            phase += (440.0 + vibrato) * Math.Tau / sampleRate;
            slowPhase += 5.0 * Math.Tau / sampleRate;

            span[i].Mono = sample;
        }
    }

    private static void MyStereo16Filler(Memory<SampleStereo16> memory)
    {
        var span = memory.Span;

        for (int i = 0; i < span.Length; i++)
        {
            short sample = (short)(Math.Sin(phase) * 32767);
            double vibrato = Math.Sin(slowPhase) * 10.0;

            phase += (440.0 + vibrato) * Math.Tau / sampleRate;
            slowPhase += 5.0 * Math.Tau / sampleRate;

            span[i].Left = sample;
            span[i].Right = sample;
        }
    }
#endif

    private static readonly int sampleRate = 48000;
    private static double phase = 0.0;
    private static double slowPhase = 0.0;
    private static int note = -1;
    //private static string[] notes = { "C-4", "   ", "D-4", "E-4", "F-4", "G-4", "A-4", "B-4", "C-5" };
    private static string[] notes = { 
        "---", "G-4", "A-4", "B-4", "D-5", "C-5", "C-5", "E-5", "D-5",
        "D-5", "G-5", "F#5", "G-5", "D-5", "B-4", "G-4", "A-4", "B-4",
        "C-5", "D-5", "E-5", "D-5", "C-5", "B-4", "A-4", "B-4", "G-4",
        "F#4", "G-4", "A-4", "D-4", "F#4", "A-4", "---", "---", "---",
    };

    private static int noteIndex = 0;
    private const int ticksPerNote = 10; // 1/4 note at 48kHz
    private static int ticks = 0;

    private static void MyStereoFloat32Filler(Memory<SampleStereoFloat32> memory)
    {
        var span = memory.Span;
        
        var newNote = Notes.NoteToMidiNote(notes[noteIndex]);
        note = newNote switch
        {
            -1 => -1,
            -2 => note,
            _ => newNote
        };

        ticks++;
        if(ticks>=ticksPerNote)
        {
            ticks = 0;
            noteIndex++;
            if (noteIndex >= notes.Length)
                noteIndex = 0;
        }

        if (note == -1)
        {
            for (int i = 0; i < span.Length; i++)
            {
                span[i].Left = 0.0f;
                span[i].Right = 0.0f;
            }
            return;
        }
        else
        {
            double noteFrequency = Notes.MidiNoteToFrequency(note);

            for (int i = 0; i < span.Length; i++)
            {
                float sample = (float)(0.20 * (Math.Sin(phase * 0.5) + Math.Sin(phase) + Math.Sin(phase * 2.0) + Math.Sin(phase * 3.0) + Math.Sin(phase * 4.0)));
                double vibrato = Math.Sin(slowPhase) * 1.0;

                phase += (noteFrequency + vibrato) * Math.Tau / sampleRate;
                slowPhase += 4.0 * Math.Tau / sampleRate;

                span[i].Left = sample;
                span[i].Right = sample;
            }
        }
    }

    static async Task Main()
    {
        Console.WriteLine($"{Notes.NoteToMidiNote("A-4")} {Notes.MidiNoteToNote(0)}");

        var wavePlayer = new WavePlayer(
            48000,
            WavePlayer.SampleFormat.Float32,
            WavePlayer.SampleChannels.Stereo,
            TimeSpan.FromMilliseconds(50),
            4);

        wavePlayer.StereoFloat32Render = MyStereoFloat32Filler;
        //wavePlayer.Stereo16Render = MyStereo16Filler;
        //wavePlayer.Mono8Render = MyMono8Filler;

        await wavePlayer.Main();
    }
}
#endif

public class KeyPiano
{
    //private KeyScanner keyScanner = new KeyScanner();

    private int note = -1;

    private Dictionary<KeyScanner.VirtualKey, int> keyMap = new Dictionary<KeyScanner.VirtualKey, int>
    {
        { KeyScanner.VirtualKey.KEY_Q, 60 }, // C4
        { KeyScanner.VirtualKey.KEY_2, 61 }, // C#4
        { KeyScanner.VirtualKey.KEY_W, 62 }, // D4
        { KeyScanner.VirtualKey.KEY_3, 63 }, // D#4
        { KeyScanner.VirtualKey.KEY_E, 64 }, // E4
        { KeyScanner.VirtualKey.KEY_R, 65 }, // F4
        { KeyScanner.VirtualKey.KEY_5, 66 }, // F#4
        { KeyScanner.VirtualKey.KEY_T, 67 }, // G4
        { KeyScanner.VirtualKey.KEY_6, 68 }, // G#4
        { KeyScanner.VirtualKey.KEY_Y, 69 }, // A4
        { KeyScanner.VirtualKey.KEY_7, 70 }, // A#4
        { KeyScanner.VirtualKey.KEY_U, 71 }, // B4
        { KeyScanner.VirtualKey.KEY_I, 72 }, // C5
        { KeyScanner.VirtualKey.KEY_9, 73 }, // C#5
        { KeyScanner.VirtualKey.KEY_O, 74 }, // D5
        { KeyScanner.VirtualKey.KEY_0, 75 }, // D#5
        { KeyScanner.VirtualKey.KEY_P, 76 }, // E5

        { KeyScanner.VirtualKey.KEY_Z, 48 }, // C3
        { KeyScanner.VirtualKey.KEY_S, 49 }, // C#3
        { KeyScanner.VirtualKey.KEY_X, 50 }, // D3
        { KeyScanner.VirtualKey.KEY_D, 51 }, // D#3
        { KeyScanner.VirtualKey.KEY_C, 52 }, // E3
        { KeyScanner.VirtualKey.KEY_V, 53 }, // F3
        { KeyScanner.VirtualKey.KEY_G, 54 }, // F#3
        { KeyScanner.VirtualKey.KEY_B, 55 }, // G3
        { KeyScanner.VirtualKey.KEY_H, 56 }, // G#3
        { KeyScanner.VirtualKey.KEY_N, 57 }, // A3
        { KeyScanner.VirtualKey.KEY_J, 58 }, // A#3
        { KeyScanner.VirtualKey.KEY_M, 59 }, // B3
        { KeyScanner.VirtualKey.Oemcomma, 60 }, // C4
        { KeyScanner.VirtualKey.KEY_L, 61 }, // C#4
        { KeyScanner.VirtualKey.OemPeriod, 62 }, // D4
        { KeyScanner.VirtualKey.OemSemicolon, 63 }, // D#4
        { KeyScanner.VirtualKey.OemQuestion, 64 }, // E4

        //{ KeyScanner.VirtualKey.KEY_DOT, 73 }, // C#4
        //{ KeyScanner.VirtualKey.KEY_O, 74 }, // D4
        //{ KeyScanner.VirtualKey.KEY_0, 75 }, // D#4
        //{ KeyScanner.VirtualKey.KEY_P, 76 }  // E4
    };

    public List<(int note, bool isOn)> ToNoteEvents(List<KeyScanner.KeyEvent> keyEvents)
    {
        List<(int node, bool isOn)> notes = new List<(int node, bool isOn)>();

        foreach (var x in keyEvents)
        {
            ; if (keyMap.TryGetValue(x.Key, out int noteValue))
            {
                note = noteValue;

                if (x.IsDown)
                {
                    notes.Add((note, true));
                }
                else
                {
                    notes.Add((note, false));
                }
            }
        }
        return notes;
    }
}
