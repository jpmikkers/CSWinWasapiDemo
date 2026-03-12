using CSWinWasapiDemo;


public static class Program
{
    //[MTAThread]
    [STAThread]
    public static async Task Main(string[] args)
    {
        var probedDevices = AudioOut.ProbeDevices();
        var defaultDeviceId = AudioOut.DefaultDeviceId();

        if (defaultDeviceId == null)
        {
            throw new Exception("no default audio device found");
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
                chunk[s].Left = 0f;
                chunk[s].Right = 0.01f * MathF.Sin(phase);
                phase += advance;
                if (phase > MathF.Tau) { phase -= MathF.Tau; }
            }
        };

        await player.Start();
        Console.ReadLine();
        await player.Stop();
#if NEVER
        [DllImport("ole32.dll")]
        private static extern int CoCreateFreeThreadedMarshaler(
          IntPtr pUnkOuter,
          [MarshalAs(UnmanagedType.IUnknown)] out object ppUnkMarshaler);
        object freeThreadedMarshaler;
        // Aggregate the free-threaded marshaler
        CoCreateFreeThreadedMarshaler(IntPtr.Zero, out freeThreadedMarshaler);

        var propkeylut = new PropKeyLut();

        //cts.Cancel();
        //await playerTask;

        // see https://github.com/naudio/NAudio/blob/master/NAudio.Wasapi/CoreAudioApi/AudioClient.cs

        // Initialize COM
        unsafe
        {

            // Create the device enumerator
            //var enumerator = (IMMDeviceEnumerator)Activator.CreateInstance(Type.GetTypeFromCLSID());
            IMMDeviceEnumerator enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();

            enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out var defaultDevice);
            defaultDevice.GetId(out var strid);

            //enumerator.EnumAudioEndpoints(EDataFlow.eRender, DEVICE_STATE.DEVICE_STATE_ACTIVE, out var collection);
            enumerator.EnumAudioEndpoints(EDataFlow.eRender, DEVICE_STATE.DEVICE_STATE_ACTIVE, out var collection);

            if (collection != null)
            {
                PWSTR id = new PWSTR();

                collection.GetCount(out var count);
                for (uint i = 0; i < count; i++)
                {
                    collection.Item(i, out var device);
                    device.GetId(out id);
                    device.OpenPropertyStore(STGM.STGM_READ, out var propertyStore);
                    propertyStore.GetCount(out var propertyCount);
                    Console.WriteLine($"Device {i} has {propertyCount} properties");

                    for (int p = 0; p < propertyCount; p++)
                    {
                        propertyStore.GetAt((uint)p, out var propkey);
                        Console.WriteLine($"{propkeylut.PropertyKeyName(propkey)}");
                    }

                    propertyStore.GetValue(PInvoke.PKEY_Device_FriendlyName, out var deviceFriendlyName);
                    Console.WriteLine($"Device {MyPropVariantReader.Read(deviceFriendlyName)}");
                    PInvoke.PropVariantClear(ref deviceFriendlyName);

                    propertyStore.GetValue(PInvoke.PKEY_DeviceInterface_FriendlyName, out var deviceInterfaceFriendlyName);
                    Console.WriteLine($"DeviceItf {MyPropVariantReader.Read(deviceInterfaceFriendlyName)}");
                    PInvoke.PropVariantClear(ref deviceInterfaceFriendlyName);
                    //PInvoke.Propv

                    //PKEY_Device_FriendlyName

                    //UI_Shell_PropertiesSystem_IPropertyStore_Extensions.
                    //propertyStore..GetValue(tem, out var friendlyName);
                    Console.WriteLine($"Device {i}: {id}");

                    if (i == 2)
                    {
                        device.Activate(typeof(Windows.Win32.Media.Audio.IAudioClient3).GUID, CLSCTX.CLSCTX_ALL, null, out var audioClient2);
                        var audioClient = (IAudioClient3)audioClient2;

                        audioClient.GetMixFormat(out var pwfx);

                        try
                        {
                            // Read the base WAVEFORMATEX header
                            var wfx = Marshal.PtrToStructure<WAVEFORMATEX>((nint)pwfx);

                            if ((wfx.wFormatTag & PInvoke.WAVE_FORMAT_EXTENSIBLE) == PInvoke.WAVE_FORMAT_EXTENSIBLE)
                            {
                                // Marshal as WAVEFORMATEXTENSIBLE
                                var wfxExt = Marshal.PtrToStructure<WAVEFORMATEXTENSIBLE>((nint)pwfx);

                                // Now you can use wfxExt.Format, wfxExt.ChannelMask, wfxExt.SubFormat, etc.
                                //audioClient.Initialize(AUDCLNT_SHAREMODE.AUDCLNT_SHAREMODE_SHARED, PInvoke.AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM | PInvoke.AUDCLNT_STREAMFLAGS_EVENTCALLBACK, TimeSpan.FromSeconds(0.1).Ticks, 0, wfx, Guid.NewGuid());
                                //MyExt.Initialize(audioClient, AUDCLNT_SHAREMODE.AUDCLNT_SHAREMODE_SHARED, PInvoke.AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM | PInvoke.AUDCLNT_STREAMFLAGS_EVENTCALLBACK, TimeSpan.FromSeconds(0.1).Ticks, 0, wfxExt, Guid.NewGuid());
                                MyExt.Initialize(audioClient, AUDCLNT_SHAREMODE.AUDCLNT_SHAREMODE_SHARED, PInvoke.AUDCLNT_STREAMFLAGS_EVENTCALLBACK, TimeSpan.FromSeconds(0.1).Ticks, 0, wfxExt, Guid.NewGuid());

                                audioClient.GetService(typeof(IAudioRenderClient).GUID, out var objRenderClient);
                                var renderClient = (IAudioRenderClient)objRenderClient;

                                //audioClient.SetEventHandle(CreateEvent(null, false, false, null));
                                var audioEvent = new ManualResetEvent(false);
                                audioClient.SetEventHandle((HANDLE)audioEvent.SafeWaitHandle.DangerousGetHandle());

                                audioClient.Start();

                                audioClient.GetBufferSize(out var numBufferFrames);
                                audioClient.GetStreamLatency(out var streamLatency);

                                float phase = 0;
                                float advance = (240 * MathF.Tau / wfx.nSamplesPerSec);

                                while (true)
                                {
                                    audioEvent.WaitOne();
                                    audioEvent.Reset();

                                    bool done = false;
                                    int frameCount = 0;

                                    while (!done)
                                    {
                                        try
                                        {
                                            audioClient.GetCurrentPadding(out var numPaddingFrames);

                                            uint framesToWrite = Math.Min(100, numBufferFrames - numPaddingFrames);
                                            //Console.WriteLine($"to write {framesToWrite}");

                                            if (framesToWrite > 0)
                                            {
                                                renderClient.GetBuffer(framesToWrite, out var pData);

                                                var span = new Span<float>(pData, (int)framesToWrite * 2);

                                                for (int s = 0; s < span.Length; s += 2)
                                                {
                                                    span[s] = 0.01f * MathF.Sin(phase);
                                                    span[s + 1] = 0.01f * MathF.Sin(phase);
                                                    phase += advance;
                                                    if (phase > MathF.Tau) { phase -= MathF.Tau; }
                                                }

                                                renderClient.ReleaseBuffer(framesToWrite, 0);
                                            }
                                            else
                                            {
                                                done = true;
                                            }

                                            frameCount += (int)framesToWrite;
                                        }
                                        catch (Exception ex)
                                        {
                                            done = true;
                                        }
                                    }

                                    //Console.WriteLine($"I did {frameCount} frames {numBufferFrames}");
                                    //audioClient.GetB
                                }

                                //audioClient.
                                audioClient.Stop();
                            }
                            else
                            {
                                // Use the basic WAVEFORMATEX
                                // wfx.nSamplesPerSec, wfx.wBitsPerSample, etc.
                            }
                        }
                        finally
                        {
                            // Windows allocated this memory → you must free it
                            Marshal.FreeCoTaskMem((nint)pwfx);
                        }                        //mixFormat.Value;


                        if (ComWrappers.TryGetComInstance(audioClient2, out var wrapper))
                        {
                            Console.WriteLine("I got a wrapper");
                        }

                        var refcount = Marshal.ReleaseComObject(audioClient);
                    }
                    //var xxx = Marshal.GetIUnknownForObject(audioClient);
                    //((ComObject)audioClient)

                    //ComObject.
                    //PInvoke

                    //if(audioClient is System.Runtime.InteropServices.Marshalling.ComObject co)
                    //{
                    //    // Managed COM wrapper
                    //    ComObject.Release(co);
                    // }
                    //((System.Runtime.InteropServices.Marshalling.ComObject)audioClient);
                    //((IUnknown)audioClient2).Release();

#if NEVER
                    var isd = id.ToString();
                    var guid = typeof(Windows.Win32.Media.Audio.IAudioClient2).GUID;
                    var handler = new MyActivationHandler();
                    //var hhandler = GCHandle.Alloc(handler, GCHandleType.Pinned);

                    var hresult = PInvoke.ActivateAudioInterfaceAsync(
                        id.ToString(),
                        guid,
                        null,
                        handler,
                        out var activationOperatoin);
#endif
                }

                //Windows.Win32.Media.Audio.IAudioClient3
                //Media_Audio_IAudioClient3_Extensions.



                //    guid,
                //    null,

                //    Windows.Win32.Media.Audio.AUDIO_INTERFACE_ACTIVATION_PARAMS.Create(),
                //    null,
                //    new ActivateAudioInterfaceCompletionHandler((result) =>
                //    {
                //        Console.WriteLine($"ActivateAudioInterfaceAsync completed with result: {result}");
                //    }),
                //    out var asyncOperation);
                //Windows.Win32.Media.Audio.PInvoke.ActivateAudioInterfaceAsync(...)
                //        ActivateAudioInterfaceAsync
                //IActivateAudioInterfaceCompletionHandler
                //IActivateAudioInterfaceAsyncOperation

                //Media_Audio_IActivateAudioInterfaceAsyncOperation_Extensions.
            }

            //IMMDeviceCollection collection = new MMDeviceCollection();

            //enumerator..GetDefaultAudioEndpoint(EDataFlow.eCapture, ERole.eConsole, out var device);

            //device.GetId()

        }

        while (true)
        {
            // See https://aka.ms/new-console-template for more information
            Console.WriteLine("Hello, World!");
            Thread.Sleep(1000);
        }
#endif
    }
}
