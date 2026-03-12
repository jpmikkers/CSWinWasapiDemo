namespace Baksteen.Waves;

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 0)]
public struct SampleStereo8
{
    public sbyte Left;
    public sbyte Right;
}
