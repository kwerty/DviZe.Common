using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Kwerty.DviZe.Win;

public static class Win32ExceptionExtensions
{
    extension(Win32Exception)
    {
        public static Win32Exception FromError(string functionName, int errorCode)
            => new(errorCode, $"A native function call to '{functionName}' failed with code 0x{errorCode:X8}.");

        public static Win32Exception FromLastError(string functionName)
            => FromError(functionName, Marshal.GetLastWin32Error());
    }
}