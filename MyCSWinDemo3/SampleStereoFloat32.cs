namespace Baksteen.Waves;

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 0)]
public struct SampleStereoFloat32
{
    public float Left;
    public float Right;
}
