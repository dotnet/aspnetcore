using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;

namespace Microsoft.Diagnostics.Tools.Dump
{
    public static partial class Dumper
    {
        private static class Linux
        {
            internal static async Task CollectDumpAsync(Process process, string fileName)
            {
                // We don't work on WSL :(
                var ostype = await File.ReadAllTextAsync("/proc/sys/kernel/osrelease");
                if(ostype.Contains("Microsoft"))
                {
                    throw new PlatformNotSupportedException("Cannot collect memory dumps from Windows Subsystem for Linux.");
                }

                // First step is to find the .NET runtime. To do this we look for coreclr.so
                var coreclr = process.Modules.Cast<ProcessModule>().FirstOrDefault(m => string.Equals(m.ModuleName, "libcoreclr.so"));
                if(coreclr == null)
                {
                    throw new NotSupportedException("Unable to locate .NET runtime associated with this process!");
                }

                // Find createdump next to that file
                var runtimeDirectory = Path.GetDirectoryName(coreclr.FileName);
                var createDumpPath = Path.Combine(runtimeDirectory, "createdump");
                if(!File.Exists(createDumpPath))
                {
                    throw new NotSupportedException($"Unable to locate 'createdump' tool in '{runtimeDirectory}'");
                }

                // Create the dump
                var exitCode = await CreateDumpAsync(createDumpPath, fileName, process.Id);
                if(exitCode != 0)
                {
                    throw new Exception($"createdump exited with non-zero exit code: {exitCode}");
                }
            }

            private static Task<int> CreateDumpAsync(string exePath, string fileName, int processId)
            {
                var tcs = new TaskCompletionSource<int>();
                var createdump = new Process()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = exePath,
                        Arguments = $"-f {fileName} {processId}",
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        RedirectStandardInput = true,
                    },
                    EnableRaisingEvents = true,
                };
                createdump.Exited += (s, a) => tcs.TrySetResult(createdump.ExitCode);
                createdump.Start();
                return tcs.Task;
            }
        }
    }
}
