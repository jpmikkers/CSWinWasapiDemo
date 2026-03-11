using Windows.Win32.Foundation;
using Windows.Win32.System.Com.StructuredStorage;
using Windows.Win32.System.Variant;

internal static unsafe class MyPropVariantReader
{
    public static object? Read(in PROPVARIANT pv)
    {
        var vt = pv.Anonymous.Anonymous.vt;

        switch (vt)
        {
            case VARENUM.VT_EMPTY:
            case VARENUM.VT_NULL:
                return null;

            case VARENUM.VT_BSTR:
            case VARENUM.VT_LPWSTR:
                // CsWin32 exposes the union; field name may be pwszVal or pszVal depending on generator.
                // Try common locations via pointer reads.
                {
                    // pwszVal is a wchar_t*; marshal to managed string
                    PWSTR p = pv.Anonymous.Anonymous.Anonymous.pwszVal;
                    return p.ToString();
                    //if(p == IntPtr.Zero) return null;
                    //return Marshal.PtrToStringUni(p);
                }

            case VARENUM.VT_BOOL:
                {
                    // VARIANT_BOOL is short: -1 == true, 0 == false
                    short vb = pv.Anonymous.Anonymous.Anonymous.boolVal;
                    return vb != 0;
                }

            case VARENUM.VT_I1:
                return pv.Anonymous.Anonymous.Anonymous.bVal;

            case VARENUM.VT_UI1:
                return (sbyte)pv.Anonymous.Anonymous.Anonymous.bVal;

            case VARENUM.VT_I2:
                return pv.Anonymous.Anonymous.Anonymous.iVal;

            case VARENUM.VT_UI2:
                return pv.Anonymous.Anonymous.Anonymous.uiVal;

            case VARENUM.VT_I4:
                return pv.Anonymous.Anonymous.Anonymous.lVal;

            case VARENUM.VT_UI4:
                return pv.Anonymous.Anonymous.Anonymous.ulVal;

            case VARENUM.VT_I8:
                return pv.Anonymous.Anonymous.Anonymous.hVal; // or llVal depending on generator

            case VARENUM.VT_UI8:
                return pv.Anonymous.Anonymous.Anonymous.uhVal;

            case VARENUM.VT_DECIMAL:
                return pv.Anonymous.decVal;

            case VARENUM.VT_FILETIME:
                {
                    var ft = pv.Anonymous.Anonymous.Anonymous.filetime;
                    long fileTime = ((long)ft.dwHighDateTime << 32) | (uint)ft.dwLowDateTime;
                    return DateTime.FromFileTimeUtc(fileTime);
                }

            default:
                // Handle VT_VECTOR of strings (common)
                //if((vt & VARENUM.VT_VECTOR) != 0 && (vt & VARENUM.VT_TYPEMASK) == VARENUM.VT_LPWSTR)
                //{
                //    // vector of LPWSTR: the union usually exposes calpwstr or cElems + pElems pointer
                //    // CsWin32 may expose a pointer and a count; adapt if your generated struct differs.
                //    uint count = pv.Anonymous.Anonymous.cElems;
                //    IntPtr pElems = pv.Anonymous.Anonymous.pElems;
                //    if(pElems == IntPtr.Zero || count == 0) return Array.Empty<string>();

                //    var result = new string[count];
                //    for(uint i = 0; i < count; i++)
                //    {
                //        IntPtr pStrPtr = Marshal.ReadIntPtr(pElems, (int)(i * IntPtr.Size));
                //        result[i] = Marshal.PtrToStringUni(pStrPtr) ?? string.Empty;
                //    }
                //    return result;
                //}

                // Fallback: return raw vt so caller can handle unknown types
                return new { UnknownVt = (int)vt };
        }
    }
}