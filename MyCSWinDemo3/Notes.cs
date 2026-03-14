namespace WaveOutDemo;

internal static class Notes
{

    /// <summary>
    /// Convert a MIDI note number to a frequency in Hz.
    /// </summary>
    /// <param name="noteNumber">69 is A4, or 440Hz</param>
    /// <returns></returns>
    public static double MidiNoteToFrequency(int noteNumber)
    {
        return 440.0 * Math.Pow(2.0, (noteNumber - 69) / 12.0);
    }

    public static int NoteToMidiNote(string noteName)
    {
        // C4 is 60 in MIDI note numbers
        // D4 is 62 in MIDI note numbers
        // E4 is 64 in MIDI note numbers
        // F4 is 65 in MIDI note numbers
        // G4 is 67 in MIDI note numbers
        // A4 is 69 in MIDI note numbers
        // B4 is 71 in MIDI note numbers

        if (noteName[0] is '-')
        {
            return -1; // note off/rest
        }

        if (noteName[0] is ' ')
        {
            return -2; // keep playing the last note
        }

        var note = noteName[0] switch
        {
            '-' => -1,
            ' ' => -1,
            'C' => 0,
            'D' => 2,
            'E' => 4,
            'F' => 5,
            'G' => 7,
            'A' => 9,
            'B' => 11,
            _ => throw new ArgumentException("Invalid note name")
        };

        var sharpflat = noteName[1] switch
        {
            '#' => 1,
            'b' => -1,
            _ => 0
        };

        var octave = (noteName[2] - '4');

        return note + sharpflat + octave * 12 + 60; // C4 is 60 in MIDI note numbers
    }

    public static string MidiNoteToNote(int midiNote)
    {
        var note = midiNote % 12;
        var octave = (midiNote / 12) - 1;

        var notePart = note switch
        {
            0 => "C-",
            1 => "C#",
            2 => "D-",
            3 => "D#",
            4 => "E-",
            5 => "F-",
            6 => "F#",
            7 => "G-",
            8 => "G#",
            9 => "A-",
            10 => "A#",
            11 => "B-",
            _ => throw new ArgumentException("Invalid MIDI note number")
        };

        return $"{notePart}{(char)('0' + octave)}";
    }
}