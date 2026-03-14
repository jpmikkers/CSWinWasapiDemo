namespace WaveOutDemo;

using System.Runtime.InteropServices;

#if NOOIT
public class Program
{
    private static readonly int sampleRate = 48000;
    private static double phase = 0.0;
    private static double slowPhase = 0.0;

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

public class KeyScanner
{
    public enum VirtualKey : ushort
    {
        // Mouse buttons
        LBUTTON = 0x01,
        RBUTTON = 0x02,
        MBUTTON = 0x04,

        // Control keys
        CANCEL = 0x03,
        BACK = 0x08, // Backspace
        TAB = 0x09,
        CLEAR = 0x0C,
        RETURN = 0x0D, // Enter
        SHIFT = 0x10,
        CONTROL = 0x11, // Ctrl
        MENU = 0x12, // Alt
        PAUSE = 0x13,
        CAPITAL = 0x14, // Caps Lock

        // Arrow keys
        LEFT = 0x25,
        UP = 0x26,
        RIGHT = 0x27,
        DOWN = 0x28,

        // Function keys
        F1 = 0x70,
        F2 = 0x71,
        F3 = 0x72,
        F4 = 0x73,
        F5 = 0x74,
        F6 = 0x75,
        F7 = 0x76,
        F8 = 0x77,
        F9 = 0x78,
        F10 = 0x79,
        F11 = 0x7A,
        F12 = 0x7B,

        // Miscellaneous keys
        ESCAPE = 0x1B,
        SPACE = 0x20,
        PAGE_UP = 0x21,
        PAGE_DOWN = 0x22,
        END = 0x23,
        HOME = 0x24,
        INSERT = 0x2D,
        DELETE = 0x2E,

        // Number keys
        KEY_0 = 0x30,
        KEY_1 = 0x31,
        KEY_2 = 0x32,
        KEY_3 = 0x33,
        KEY_4 = 0x34,
        KEY_5 = 0x35,
        KEY_6 = 0x36,
        KEY_7 = 0x37,
        KEY_8 = 0x38,
        KEY_9 = 0x39,

        // Alphabet keys
        KEY_A = 0x41,
        KEY_B = 0x42,
        KEY_C = 0x43,
        KEY_D = 0x44,
        KEY_E = 0x45,
        KEY_F = 0x46,
        KEY_G = 0x47,
        KEY_H = 0x48,
        KEY_I = 0x49,
        KEY_J = 0x4A,
        KEY_K = 0x4B,
        KEY_L = 0x4C,
        KEY_M = 0x4D,
        KEY_N = 0x4E,
        KEY_O = 0x4F,
        KEY_P = 0x50,
        KEY_Q = 0x51,
        KEY_R = 0x52,
        KEY_S = 0x53,
        KEY_T = 0x54,
        KEY_U = 0x55,
        KEY_V = 0x56,
        KEY_W = 0x57,
        KEY_X = 0x58,
        KEY_Y = 0x59,
        KEY_Z = 0x5A,

        // Numpad keys
        NUMPAD0 = 0x60,
        NUMPAD1 = 0x61,
        NUMPAD2 = 0x62,
        NUMPAD3 = 0x63,
        NUMPAD4 = 0x64,
        NUMPAD5 = 0x65,
        NUMPAD6 = 0x66,
        NUMPAD7 = 0x67,
        NUMPAD8 = 0x68,
        NUMPAD9 = 0x69,
        MULTIPLY = 0x6A,
        ADD = 0x6B,
        SEPARATOR = 0x6C,
        SUBTRACT = 0x6D,
        DECIMAL = 0x6E,
        DIVIDE = 0x6F,

        OemSemicolon = 186,
        Oemplus = 187,  //The OEM plus key on any country/region keyboard.
        Oemcomma = 188, //The OEM comma key on any country/region keyboard.
        OemMinus = 189, // The OEM minus key on any country/region keyboard.
        OemPeriod = 190, // The OEM period key on any country/region keyboard.                    { KeyScanner.VirtualKey.DECIMAL, 60 }, // C4
        OemQuestion = 191, // The OEM question mark key on any country/region keyboard.

    }

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    readonly bool[] keys = new bool[256]; // Array to store key states

    public record KeyEvent
    {
        public VirtualKey Key
        {
            get; set;
        }

        public bool IsDown
        {
            get; set;
        }
    }

    public List<KeyEvent> Scan()
    {
        var result = new List<KeyEvent>();

        //System.Windows.Forms.Keys key = System.Windows.Forms.Keys.None;

        for (int key = 0; key < 256; key++)
        {
            var keyState = GetAsyncKeyState(key);

            // Check if the most significant bit is set (key down)
            if (!keys[key] && (keyState & 0x8000) != 0)
            {
                //Console.WriteLine($"Key Down: {(VirtualKey)key}");
                keys[key] = true;
                result.Add(new KeyEvent { Key = (VirtualKey)key, IsDown = true });
            }
            // Check if the least significant bit is set (key up event recorded)
            else if (keys[key] && keyState == 0)
            {
                //Console.WriteLine($"Key Up: {(VirtualKey)key}");
                keys[key] = false;
                result.Add(new KeyEvent { Key = (VirtualKey)key, IsDown = false });
            }
        }

        return result;
    }
}
