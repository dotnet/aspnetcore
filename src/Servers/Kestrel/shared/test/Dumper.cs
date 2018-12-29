using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Dump
{
    public static partial class Dumper
    {
        public static Task CollectDumpAsync(Process process, string fileName)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Windows.CollectDumpAsync(process, fileName);
            }
            else if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return Linux.CollectDumpAsync(process, fileName);
            }
            else
            {
                // OSX ????
                return Task.CompletedTask;
                // throw new PlatformNotSupportedException("Can't collect a memory dump on this platform.");
            }
        }
    }
}
