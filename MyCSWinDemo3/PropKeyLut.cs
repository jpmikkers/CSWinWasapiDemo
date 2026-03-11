namespace CSWinWasapiDemo;

using System.Reflection;
using Windows.Win32;
using Windows.Win32.Foundation;

internal class PropKeyLut
{
    private record MYPROPERTYKEY
    {
        public Guid fmtid { get; set; }
        public uint pid { get; set; }

    }

    private Dictionary<MYPROPERTYKEY, string> _known = [];

    //public PROPERTYKEY FindPropertyKey(string name)
    //{

    //}

    public string PropertyKeyName(PROPERTYKEY key)
    {
        if (!_known.TryGetValue(new MYPROPERTYKEY { fmtid = key.fmtid, pid = key.pid }, out var result))
        {
            result = $"{key.fmtid} {key.pid}";
        }
        return result;
    }

    public PropKeyLut()
    {
        var pinvtype = typeof(PInvoke);
        foreach (var p in pinvtype.GetFields(
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.Static).Where(x => x.FieldType == typeof(PROPERTYKEY)))
        {
            if (p.GetValue(null) is PROPERTYKEY key)
            {
                Console.WriteLine($"{p.Name} {key.fmtid} {key.pid} ");
                if (!_known.TryAdd(new MYPROPERTYKEY { fmtid = key.fmtid, pid = key.pid }, p.Name))
                {
                    Console.WriteLine($"***************** {p.Name}  duped?");
                }
            }
        }
    }
}
