using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Media.Audio;
using Windows.Win32.System.Com;
//[GeneratedComClass]
[ComVisible(true)]
public partial class MyActivationHandler : IActivateAudioInterfaceCompletionHandler, IAgileObject
{
    public MyActivationHandler()
    {
    }

    void IActivateAudioInterfaceCompletionHandler.ActivateCompleted(IActivateAudioInterfaceAsyncOperation activateOperation)
    {
        // First get the activation results, and see if anything bad happened then
        activateOperation.GetActivateResult(out var hr, out object unk);
        if (hr != 0)
        {
            Console.WriteLine("ActivateCompleted failed");
            Marshal.ThrowExceptionForHR(hr, new IntPtr(-1));
            //tcs.TrySetException(Marshal.GetExceptionForHR(hr, new IntPtr(-1)));
            return;
        }
        Console.WriteLine("ActivateCompleted called");
    }

}
