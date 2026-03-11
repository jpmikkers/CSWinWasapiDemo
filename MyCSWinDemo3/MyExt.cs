namespace CSWinWasapiDemo;

using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Media.Audio;
using Windows.Win32.UI.Shell.PropertiesSystem;


internal static partial class MyExt
{
    /// <inheritdoc cref="winmdroot.Media.Audio.IAudioClient3.Initialize(winmdroot.Media.Audio.AUDCLNT_SHAREMODE, uint, long, long, winmdroot.Media.Audio.WAVEFORMATEX*, global::System.Guid*)"/>
    internal static unsafe void Initialize(this IAudioClient3 @this, AUDCLNT_SHAREMODE ShareMode, uint StreamFlags, long hnsBufferDuration, long hnsPeriodicity, in WAVEFORMATEXTENSIBLE pFormat, [Optional] global::System.Guid? AudioSessionGuid)
    {
        fixed (WAVEFORMATEXTENSIBLE* pFormatLocal = &pFormat)
        {
            global::System.Guid AudioSessionGuidLocal = AudioSessionGuid ?? default(global::System.Guid);
            @this.Initialize(ShareMode, StreamFlags, hnsBufferDuration, hnsPeriodicity, (WAVEFORMATEX*)pFormatLocal, AudioSessionGuid.HasValue ? &AudioSessionGuidLocal : null);
        }
    }

    internal static unsafe HRESULT MyIsFormatSupported(this IAudioClient3 @this, AUDCLNT_SHAREMODE ShareMode, in WAVEFORMATEXTENSIBLE pFormat, out WAVEFORMATEXTENSIBLE? closestMatch)
    {
        closestMatch = null;

        nint closestMatchPointer;
        fixed (WAVEFORMATEXTENSIBLE* pFormatLocal = &pFormat)
        {
            var result = @this.IsFormatSupported(ShareMode, (WAVEFORMATEX*)pFormatLocal, (WAVEFORMATEX**)&closestMatchPointer);

            if (closestMatchPointer != nint.Zero)
            {
                var wfx = Marshal.PtrToStructure<WAVEFORMATEX>(closestMatchPointer);

                if ((wfx.wFormatTag & PInvoke.WAVE_FORMAT_EXTENSIBLE) == PInvoke.WAVE_FORMAT_EXTENSIBLE)
                {
                    // Marshal as WAVEFORMATEXTENSIBLE
                    var wfxExt = Marshal.PtrToStructure<WAVEFORMATEXTENSIBLE>(closestMatchPointer);
                    closestMatch = wfxExt;
                }
                else
                {
                    closestMatch = new WAVEFORMATEXTENSIBLE
                    {
                        Format = wfx,
                    };
                }

                Marshal.FreeCoTaskMem(closestMatchPointer);
            }

            return result;
        }
    }

    internal static T TryRead<T>(this IPropertyStore store, PROPERTYKEY key, T fallback)
    {
        store.GetValue(key, out var pv);
        var tmpVal = MyPropVariantReader.Read(pv);
        var result = (tmpVal != null && tmpVal is T) ? (T)tmpVal : fallback;
        PInvoke.PropVariantClear(ref pv);
        return result;
    }
}
