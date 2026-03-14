namespace Baksteen.Waves;

using System;
using System.Runtime.InteropServices;

public class ComScope<T> : IDisposable where T : class
{
    public T Value { get; }

    public static ComScope<T> Create(T value)
    {
        return new ComScope<T>(value);
    }

    public ComScope(T value)
    {
        Value = value;
    }

    public void Dispose()
    {
        if (Value is not null)
        {
            Marshal.ReleaseComObject(Value);
            //unsafe
            //{
            //    // CsWin32 COM interfaces expose Release() directly
            //    ((IUnknown*)Unsafe.As<T, void>(ref Unsafe.AsRef(Value)))->Release();
            //}
        }
    }
}