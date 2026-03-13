namespace CSWinWasapiDemo;

using Baksteen.Waves;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Media.Audio;
using Windows.Win32.Media.KernelStreaming;
using Windows.Win32.Media.Multimedia;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell.PropertiesSystem;

public class AudioOut
{
    private readonly MtaTaskScheduler _scheduler;
    private readonly TaskFactory _taskFactory;
    private readonly AudioDeviceId _audioDeviceId;
    private readonly AudioConfig _audioConfig;
    private readonly ShareMode _shareMode;

    private ComScope<IMMDevice>? _device;
    private ComScope<IAudioClient3>? _audioClient;
    private ComScope<IAudioRenderClient>? _renderClient;
    private Task? _renderTask;
    private ManualResetEvent? _audioEvent;
    private CancellationTokenSource? _cts;

    public Action<Span<SampleShortMono>>? Mono16Render { get; set; }
    public Action<Span<SampleShortStereo>>? Stereo16Render { get; set; }
    public Action<Span<SampleFloatMono>>? MonoFloat32Render { get; set; }
    public Action<Span<SampleFloatStereo>>? StereoFloat32Render { get; set; }

    public enum ShareMode
    {
        Shared,
        Exclusive,
    }

    public enum SampleFormat
    {
        FmtShort,
        Fmt24BitsIn32Bits,
        FmtFloat,
    }

    public record AudioConfig
    {
        public uint Frequency { get; set; }
        public ushort Channels { get; set; }
        public SampleFormat Format { get; set; }
    }

    private static WAVEFORMATEXTENSIBLE CreateWaveFormatExtensible(AudioConfig audioConfig)
    {
        WAVEFORMATEXTENSIBLE format;

        switch (audioConfig.Format)
        {
            //case SampleFormat.FmtShort:
            //    format = new WAVEFORMATEXTENSIBLE
            //    {
            //        Format = new WAVEFORMATEX
            //        {
            //            cbSize = 0,
            //            nChannels = channels,
            //            nSamplesPerSec = samplefrequency,
            //            wFormatTag = (ushort)PInvoke.WAVE_FORMAT_PCM,
            //        }
            //    };
            //    format.Samples.wValidBitsPerSample = sizeof(short) * 8;
            //    format.Format.wBitsPerSample = sizeof(short) * 8;
            //    break;

            case SampleFormat.FmtShort:
                format = new WAVEFORMATEXTENSIBLE
                {
                    Format = new WAVEFORMATEX
                    {
                        cbSize = (ushort)(Marshal.SizeOf<WAVEFORMATEXTENSIBLE>() - Marshal.SizeOf<WAVEFORMATEX>()),
                        nChannels = audioConfig.Channels,
                        nSamplesPerSec = audioConfig.Frequency,
                        wFormatTag = (ushort)PInvoke.WAVE_FORMAT_EXTENSIBLE,
                    },
                    SubFormat = typeof(KSDATAFORMAT_SUBTYPE_PCM).GUID,
                };
                format.Samples.wValidBitsPerSample = sizeof(short) * 8;
                format.Format.wBitsPerSample = sizeof(short) * 8;
                break;

            case SampleFormat.Fmt24BitsIn32Bits:
                format = new WAVEFORMATEXTENSIBLE
                {
                    Format = new WAVEFORMATEX
                    {
                        cbSize = (ushort)(Marshal.SizeOf<WAVEFORMATEXTENSIBLE>() - Marshal.SizeOf<WAVEFORMATEX>()),
                        nChannels = audioConfig.Channels,
                        nSamplesPerSec = audioConfig.Frequency,
                        wFormatTag = (ushort)PInvoke.WAVE_FORMAT_EXTENSIBLE,
                    },
                    SubFormat = typeof(KSDATAFORMAT_SUBTYPE_PCM).GUID,
                };
                format.Samples.wValidBitsPerSample = 24;
                format.Format.wBitsPerSample = 32;
                break;

            case SampleFormat.FmtFloat:
                format = new WAVEFORMATEXTENSIBLE
                {
                    Format = new WAVEFORMATEX
                    {
                        cbSize = (ushort)(Marshal.SizeOf<WAVEFORMATEXTENSIBLE>() - Marshal.SizeOf<WAVEFORMATEX>()),
                        nChannels = audioConfig.Channels,
                        nSamplesPerSec = audioConfig.Frequency,
                        wFormatTag = (ushort)PInvoke.WAVE_FORMAT_EXTENSIBLE,
                    },
                    SubFormat = typeof(KSDATAFORMAT_SUBTYPE_IEEE_FLOAT).GUID,
                };
                format.Samples.wValidBitsPerSample = sizeof(float) * 8;   // 32?
                format.Format.wBitsPerSample = sizeof(float) * 8;
                break;

            default:
                throw new NotSupportedException();
        }

        format.Format.nBlockAlign = (ushort)((format.Format.nChannels * format.Format.wBitsPerSample) / 8);
        format.Format.nAvgBytesPerSec = format.Format.nSamplesPerSec * format.Format.nBlockAlign;

        format.dwChannelMask = format.Format.nChannels switch
        {
            1 => PInvoke.SPEAKER_FRONT_LEFT,
            2 => PInvoke.SPEAKER_FRONT_LEFT | PInvoke.SPEAKER_FRONT_RIGHT,
            _ => throw new NotImplementedException()
        };

        return format;
    }

    public AudioOut(AudioDeviceId audioDeviceId, AudioConfig audioConfig, ShareMode shareMode)
    {
        _audioDeviceId = audioDeviceId;
        _audioConfig = audioConfig;
        _shareMode = shareMode;

        _scheduler = new MtaTaskScheduler(2);

        _taskFactory = new TaskFactory(
            CancellationToken.None,
            TaskCreationOptions.DenyChildAttach,
            TaskContinuationOptions.ExecuteSynchronously,
            _scheduler);
    }

    private static AUDCLNT_SHAREMODE ToNative(ShareMode shareMode)
    {
        return shareMode switch
        {
            ShareMode.Shared => AUDCLNT_SHAREMODE.AUDCLNT_SHAREMODE_SHARED,
            ShareMode.Exclusive => AUDCLNT_SHAREMODE.AUDCLNT_SHAREMODE_EXCLUSIVE,
            _ => throw new NotImplementedException()
        };
    }

    public static AudioDeviceId? DefaultDeviceId()
    {
        // Create the device enumerator
        //var enumerator = (IMMDeviceEnumerator)Activator.CreateInstance(Type.GetTypeFromCLSID());

        using var scoped = new ComScope<IMMDeviceEnumerator>((IMMDeviceEnumerator)new MMDeviceEnumerator());
        scoped.Value.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out var defaultDevice);
        if (defaultDevice != null)
        {
            defaultDevice.GetId(out var strid);
            return new AudioDeviceId { NativeId = strid.ToString() };
        }
        return null;
    }

    private static ComScope<IMMDevice>? GetDevice(AudioDeviceId audioDeviceId)
    {
        using var scopedDeviceEnumerator = ComScope<IMMDeviceEnumerator>.Create((IMMDeviceEnumerator)new MMDeviceEnumerator());
        scopedDeviceEnumerator.Value.GetDevice(audioDeviceId.NativeId, out var device);

        if (device != null)
        {
            return ComScope<IMMDevice>.Create(device);
        }
        return null;
    }

    public static bool ProbeFormat(AudioDeviceId audioDeviceId, AudioConfig audioConfig, ShareMode shareMode)
    {
        using var device = GetDevice(audioDeviceId);

        if (device is not null)
        {
            device.Value.Activate(typeof(IAudioClient3).GUID, CLSCTX.CLSCTX_ALL, null, out var audioClient);

            if (audioClient != null)
            {
                using var scopedAudioClient = ComScope<IAudioClient3>.Create((IAudioClient3)audioClient);
                var format = CreateWaveFormatExtensible(audioConfig);
                HRESULT hresult = scopedAudioClient.Value.MyIsFormatSupported(ToNative(shareMode), format, out var closestMatch);
                return hresult == 0;
            }
        }
        return false;
    }

    public static List<AudioDeviceInfo> ProbeDevices()
    {
        var result = new List<AudioDeviceInfo>();

        // Create the device enumerator
        //var enumerator = (IMMDeviceEnumerator)Activator.CreateInstance(Type.GetTypeFromCLSID());
        using var scopedenumerator = new ComScope<IMMDeviceEnumerator>((IMMDeviceEnumerator)new MMDeviceEnumerator());

        //enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out var defaultDevice);
        //defaultDevice.GetId(out var strid);

        scopedenumerator.Value.EnumAudioEndpoints(EDataFlow.eRender, DEVICE_STATE.DEVICE_STATE_ACTIVE, out var collection);

        if (collection != null)
        {
            using var scopedCollection = ComScope<IMMDeviceCollection>.Create(collection);

            scopedCollection.Value.GetCount(out var count);

            for (uint i = 0; i < count; i++)
            {
                scopedCollection.Value.Item(i, out var device);

                var di = new AudioDeviceInfo();

                // The returned string is allocated by the system.
                // The caller must not free it.
                // The pointer remains valid for the lifetime of the IMMDevice object.
                device.GetId(out var id);
                di.Id = new AudioDeviceId { NativeId = id.ToString() };

                device.OpenPropertyStore(STGM.STGM_READ, out var propertyStore);
                using var scopedPropertyStore = ComScope<IPropertyStore>.Create(propertyStore);

                //propertyStore.GetCount(out var propertyCount);
                //Console.WriteLine($"Device {i} has {propertyCount} properties");

                //for (int p = 0; p < propertyCount; p++)
                //{
                //    propertyStore.GetAt((uint)p, out var propkey);
                //    //Console.WriteLine($"{propkeylut.PropertyKeyName(propkey)}");
                //}

                di.Name = scopedPropertyStore.Value.TryRead(PInvoke.PKEY_Device_FriendlyName, string.Empty);
                di.InterfaceFriendlyName = scopedPropertyStore.Value.TryRead(PInvoke.PKEY_DeviceInterface_FriendlyName, string.Empty);
                di.Description = scopedPropertyStore.Value.TryRead(PInvoke.PKEY_Device_DeviceDesc, string.Empty);

                result.Add(di);
            }
        }
        return result;
    }

    private async Task InitTask()
    {
        _device = AudioOut.GetDevice(_audioDeviceId);

        if (_device == null)
        {
            throw new ArgumentOutOfRangeException("deviceid");
        }

        _device.Value.Activate(typeof(IAudioClient3).GUID, CLSCTX.CLSCTX_ALL, null, out var tAudioClient);
        _audioClient = ComScope<IAudioClient3>.Create((IAudioClient3)tAudioClient);

        var format = CreateWaveFormatExtensible(_audioConfig);
        //audioClient.Value.GetMixFormat(out var pwfx);

        // Now you can use wfxExt.Format, wfxExt.ChannelMask, wfxExt.SubFormat, etc.
        //audioClient.Initialize(AUDCLNT_SHAREMODE.AUDCLNT_SHAREMODE_SHARED, PInvoke.AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM | PInvoke.AUDCLNT_STREAMFLAGS_EVENTCALLBACK, TimeSpan.FromSeconds(0.1).Ticks, 0, wfx, Guid.NewGuid());
        //MyExt.Initialize(audioClient, AUDCLNT_SHAREMODE.AUDCLNT_SHAREMODE_SHARED, PInvoke.AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM | PInvoke.AUDCLNT_STREAMFLAGS_EVENTCALLBACK, TimeSpan.FromSeconds(0.1).Ticks, 0, wfxExt, Guid.NewGuid());
        MyExt.Initialize(
            _audioClient.Value,
            ToNative(_shareMode),
            PInvoke.AUDCLNT_STREAMFLAGS_EVENTCALLBACK,
            TimeSpan.FromSeconds(0.1).Ticks,
            0,
            format,
            Guid.NewGuid());

        _audioClient.Value.GetService(typeof(IAudioRenderClient).GUID, out var tRenderClient);
        _renderClient = ComScope<IAudioRenderClient>.Create((IAudioRenderClient)tRenderClient);

        //audioClient.SetEventHandle(CreateEvent(null, false, false, null));
        _audioEvent = new ManualResetEvent(false);
        //var ttt = new ManualResetEventSlim(false);

        _audioClient.Value.SetEventHandle((HANDLE)_audioEvent.SafeWaitHandle.DangerousGetHandle());
        _audioClient.Value.Start();

        _cts = new CancellationTokenSource();
        _renderTask = _taskFactory.StartNew(async () => RenderTask(_cts.Token));
    }

    private async Task RenderTask(CancellationToken ct)
    {
        if (_audioClient == null || _renderClient == null || _audioEvent == null) return;

        _audioClient.Value.GetBufferSize(out var numBufferFrames);
        _audioClient.Value.GetStreamLatency(out var streamLatency);

        while (!ct.IsCancellationRequested)
        {
            //Console.WriteLine($"Before: I am on thread {Thread.CurrentThread.ManagedThreadId}");
            //await WaitOneAsync(audioEvent, ct);
            //Console.WriteLine($"After: I am on thread {Thread.CurrentThread.ManagedThreadId}");

            _audioEvent.WaitOne();
            _audioEvent.Reset();

            bool done = false;
            int frameCount = 0;

            while (!done && !ct.IsCancellationRequested)
            {
                try
                {
                    _audioClient.Value.GetCurrentPadding(out var numPaddingFrames);

                    uint framesToWrite = Math.Min(100, numBufferFrames - numPaddingFrames);
                    //Console.WriteLine($"to write {framesToWrite}");

                    unsafe
                    {
                        if (framesToWrite > 0)
                        {
                            _renderClient.Value.GetBuffer(framesToWrite, out var pData);

                            try
                            {
                                switch (_audioConfig.Format)
                                {
                                    case SampleFormat.FmtShort when _audioConfig.Channels == 1:
                                        {
                                            var span = new Span<SampleShortMono>(pData, (int)framesToWrite);
                                            Mono16Render?.Invoke(span);
                                        }
                                        break;

                                    case SampleFormat.FmtShort when _audioConfig.Channels == 2:
                                        {
                                            var span = new Span<SampleShortStereo>(pData, (int)framesToWrite);
                                            Stereo16Render?.Invoke(span);
                                        }
                                        break;

                                    case SampleFormat.Fmt24BitsIn32Bits when _audioConfig.Channels == 1:
                                        // TODO
                                        break;

                                    case SampleFormat.Fmt24BitsIn32Bits when _audioConfig.Channels == 2:
                                        // TODO
                                        break;

                                    case SampleFormat.FmtFloat when _audioConfig.Channels == 1:
                                        {
                                            var span = new Span<SampleFloatMono>(pData, (int)framesToWrite);
                                            MonoFloat32Render?.Invoke(span);
                                        }
                                        break;

                                    case SampleFormat.FmtFloat when _audioConfig.Channels == 2:
                                        {
                                            var span = new Span<SampleFloatStereo>(pData, (int)framesToWrite);
                                            StereoFloat32Render?.Invoke(span);
                                        }
                                        break;

                                    default:
                                        break;
                                }
                            }
                            finally
                            {
                                _renderClient.Value.ReleaseBuffer(framesToWrite, 0);
                            }
                        }
                        else
                        {
                            done = true;
                        }

                        frameCount += (int)framesToWrite;
                    }
                }
                catch (Exception ex)
                {
                    done = true;
                }
            }
        }

        //audioClient.
        _audioClient.Value.Stop();
    }

    private async Task RunMTA(Func<Task> action)
    {
        try
        {
            await _taskFactory.StartNew(async () =>
            {
                await action().ConfigureAwait(false);
            }).Unwrap();
        }
        catch (COMException ex)
        {
            throw new Exception(ErrorLut.TranslateHRESULT((HRESULT)ex.ErrorCode), ex);
        }
    }

    public async Task Start()
    {
        await RunMTA(InitTask);
    }

    public async Task Stop()
    {
        await RunMTA(async () =>
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts = null;
            }

            if (_renderTask != null)
            {
                _audioEvent?.Set();                         // make sure the render thread doesn't get stuck on the event wait
                await _renderTask.ConfigureAwait(false);    // todo this might throw?
                _renderTask = null;
            }

            if (_audioClient != null)
            {
                _audioClient.Value.Stop();
                _audioClient.Dispose();
                _audioClient = null;
            }

            if (_renderClient != null)
            {
                _renderClient.Dispose();
                _renderClient = null;
            }

            if (_device != null)
            {
                _device.Dispose();
                _device = null;
            }
        });
    }
}
