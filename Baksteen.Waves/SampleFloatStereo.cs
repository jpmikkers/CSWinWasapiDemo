namespace Baksteen.Waves;

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 0)]
public struct SampleFloatStereo
{
    public float Left;
    public float Right;
}
