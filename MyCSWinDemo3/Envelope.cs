
namespace WaveOutDemo;

public class Envelope
{
    public double SampleRate
    {
        get; set;
    } = 44100.0;

    public TimeSpan AttackTime
    {
        get; set;
    } = TimeSpan.FromMilliseconds(50);

    public TimeSpan DecayTime
    {
        get; set;
    } = TimeSpan.FromMilliseconds(50);

    public double SustainLevel
    {
        get; set;
    } = 0.5;

    public TimeSpan ReleaseTime
    {
        get; set;
    } = TimeSpan.FromMilliseconds(500);

    private int sampleCounter = 0;

    private bool isNoteOn = false;
    private EnvelopeState envelopeState = EnvelopeState.Off;

    private double attackStartLevel = 0.0;
    private double releaseStartLevel = 0.0;

    public enum EnvelopeState
    {
        Off,
        Attack,
        Decay,
        Sustain,
        Release
    }

    public EnvelopeState State => envelopeState;

    public void NoteOn()
    {
        isNoteOn = true;
        envelopeState = EnvelopeState.Attack;
        sampleCounter = 0;
    }

    public void NoteOff()
    {
        isNoteOn = false;
        envelopeState = EnvelopeState.Release;
        sampleCounter = 0;
    }

    public void NoteKill()
    {
        envelopeState = EnvelopeState.Off;
        attackStartLevel = 0.0;
        releaseStartLevel = 0.0;
        sampleCounter = 0;
    }

    // see https://dsp.stackexchange.com/a/94617/83305
    private double Shaper(double shape, double x)
    {
        //return 1.0 - Math.Pow(1 - x,2.0);
        if (Math.Abs(shape) < 0.001)
        {
            return x;
        }
        else
        {
            return (Math.Pow(2.0, 16.0 * shape * x) - 1.0) / (Math.Pow(2.0, 16.0 * shape) - 1.0);
        }
    }

    private double ShapedLevel(double relTime, double startLevel, double endLevel, double shapePower)
    {
        relTime = Math.Clamp(relTime, 0.0, 1.0);
        return Double.Lerp(startLevel, endLevel, Shaper(-0.3, relTime));
    }

    public double Sample()
    {
        double relativeTime;
        double result;

        switch (envelopeState)
        {
            case EnvelopeState.Off:
            default:
                result = 0.0;
                break;

            case EnvelopeState.Attack:
                relativeTime = (double)sampleCounter / (SampleRate * AttackTime.TotalSeconds);
                result = ShapedLevel(relativeTime, attackStartLevel, 1.0, 0.33);
                releaseStartLevel = result;
                if (relativeTime >= 1.0)
                {
                    envelopeState = EnvelopeState.Decay;
                    sampleCounter = 0;
                }
                break;

            case EnvelopeState.Decay:
                relativeTime = (double)sampleCounter / (SampleRate * DecayTime.TotalSeconds);
                result = ShapedLevel(relativeTime, 1.0, SustainLevel, 0.33);
                releaseStartLevel = result;
                attackStartLevel = result;
                if (relativeTime >= 1.0)
                {
                    envelopeState = EnvelopeState.Sustain;
                    sampleCounter = 0;
                }
                break;

            case EnvelopeState.Sustain:
                result = SustainLevel;
                releaseStartLevel = result;
                attackStartLevel = result;
                break;

            case EnvelopeState.Release:
                relativeTime = (double)sampleCounter / (SampleRate * ReleaseTime.TotalSeconds);
                result = ShapedLevel(relativeTime, releaseStartLevel, 0, 0.33);
                attackStartLevel = result;
                if (relativeTime >= 1.0)
                {
                    envelopeState = EnvelopeState.Off;
                    sampleCounter = 0;
                }
                break;
        }

        sampleCounter++;
        //System.Diagnostics.Trace.WriteLine($"Sample: {result}");
        return result;
    }
}
