namespace WaveOutDemo;

using Baksteen.Waves;
using System;
using System.Collections.Concurrent;

public class Organ
{
    private class Channel
    {
        private readonly Envelope _envelope;

        private readonly double _sampleRate;
        private readonly double _radiansPerSample;
        private double _lphase = 0.0;
        private double _lslowPhase = 0.0;
        private double _rphase = 0.0;
        private double _rslowPhase = 0.0;
        private bool _isPlaying;

        public int Note
        {
            get; private set;
        }

        private double frequency;

        public void KeyOn(int note)
        {
            if (note < 0)
                return;
            this.Note = note;
            frequency = Notes.MidiNoteToFrequency(note);
            _lphase = Random.Shared.NextDouble() * Math.Tau;
            _lslowPhase = 0.0;
            _rphase = Random.Shared.NextDouble() * Math.Tau;
            _rslowPhase = 0.0;
            _isPlaying = true;
            _envelope.NoteOn();
        }

        public void KeyOff()
        {
            _isPlaying = false;
            _envelope.NoteOff();
            //envelope.NoteKill();
        }

        public Channel(double sampleRate)
        {
            this._sampleRate = sampleRate;
            _radiansPerSample = Math.Tau / _sampleRate;
            this._envelope = new Envelope()
            {
                SampleRate = sampleRate,
                AttackTime = TimeSpan.FromMilliseconds(10),
                DecayTime = TimeSpan.FromMilliseconds(10),
                SustainLevel = 0.6,
                ReleaseTime = TimeSpan.FromMilliseconds(250)
            };
        }

        public SampleFloatStereo Render()
        {
            if (_envelope.State != Envelope.EnvelopeState.Off)
            {
                var volume = _envelope.Sample();

                float lsample = (float)(volume * 0.25 * (Math.Sin(_lphase) + Math.Sin(_lphase * 2.0) + Math.Sin(_lphase * 3.0) + Math.Sin(_lphase * 4.0)));
                double lvibrato = Math.Sin(_lslowPhase) * 1.0;
                _lphase = (_lphase + ((frequency * 1.002) + lvibrato) * _radiansPerSample) % Math.Tau;
                _lslowPhase = (_lslowPhase + (4.0 + 0.001) * _radiansPerSample) % Math.Tau;

                float rsample = (float)(volume * 0.25 * (Math.Sin(_rphase) + Math.Sin(_rphase * 2.0) + Math.Sin(_rphase * 3.0) + Math.Sin(_rphase * 4.0)));
                double rvibrato = Math.Sin(_rslowPhase) * 1.0;
                _rphase = (_rphase + ((frequency / 1.002) + rvibrato) * _radiansPerSample) % Math.Tau;
                _rslowPhase = (_rslowPhase + (4.0 - 0.001) * _radiansPerSample) % Math.Tau;
                return new SampleFloatStereo { Left = lsample, Right = rsample };
            }
            else
            {
                return new SampleFloatStereo();
            }
        }
    }

    private readonly int _sampleRate;
    private readonly int _maxPolyphony;
    private readonly Queue<Channel> _freeChannels = new();
    private readonly HashSet<Channel> _usedChannels = new();
    private ConcurrentQueue<(int note, bool isOn)> _noteEvents = [];

    public Organ(int sampleRate, int maxPolyphony)
    {
        this._sampleRate = sampleRate;
        this._maxPolyphony = maxPolyphony;
        for (int i = 0; i < this._maxPolyphony; i++)
        {
            _freeChannels.Enqueue(new Channel(sampleRate));
        }
    }

    private void KeyOff(int note)
    {
        foreach (var channel in _usedChannels.Where(c => c.Note == note).ToList())
        {
            channel.KeyOff();
            _usedChannels.Remove(channel);
            _freeChannels.Enqueue(channel);
        }
    }

    public void Render(Span<SampleFloatStereo> span)
    {
        while (_noteEvents.TryDequeue(out var n))
        {
            if (n.isOn)
            {
                KeyOff(n.note);
                if (_freeChannels.TryDequeue(out var channel))
                {
                    _usedChannels.Add(channel);
                    channel.KeyOn(n.note);
                }
            }
            else
            {
                KeyOff(n.note);
            }
        }

        for (var i = 0; i < span.Length; i++)
        {
            var left = 0f;
            var right = 0f;

            foreach (var channel in _usedChannels.Concat(_freeChannels))
            {
                var tmp = channel.Render();
                left += tmp.Left;
                right += tmp.Right;
            }

            span[i] = new SampleFloatStereo { Left = left / _maxPolyphony, Right = right / _maxPolyphony };
        }
    }

    public void QueueNoteEvent(int note, bool isOn)
    {
        _noteEvents.Enqueue((note, isOn));
    }
}
