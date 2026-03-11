namespace CSWinWasapiDemo;

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

    private static WAVEFORMATEXTENSIBLE CreateWaveFormatExtensible(uint samplefrequency, ushort channels, SampleFormat sampleFormat)
    {
        WAVEFORMATEXTENSIBLE format;

        switch (sampleFormat)
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
                        nChannels = channels,
                        nSamplesPerSec = samplefrequency,
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
                        nChannels = channels,
                        nSamplesPerSec = samplefrequency,
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
                        nChannels = channels,
                        nSamplesPerSec = samplefrequency,
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


    public AudioOut()
    {

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

    public static bool ProbeFormat(AudioDeviceId audioDeviceId, ShareMode shareMode, uint sampleRate, ushort numChannels, SampleFormat sampleFormat)
    {
        using var device = GetDevice(audioDeviceId);

        if (device is not null)
        {
            device.Value.Activate(typeof(IAudioClient3).GUID, CLSCTX.CLSCTX_ALL, null, out var audioClient);

            if (audioClient != null)
            {
                using var scopedAudioClient = ComScope<IAudioClient3>.Create((IAudioClient3)audioClient);
                var format = CreateWaveFormatExtensible(sampleRate, numChannels, sampleFormat);
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
}
