namespace Baksteen.Waves;

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 0)]
public struct SampleShortStereo
{
    public short Left;
    public short Right;
}
