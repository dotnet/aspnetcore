using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebSockets.ConformanceTest.Autobahn
{
    public class Executable
    {
        private static readonly string _exeSuffix = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : string.Empty;

        private readonly string _path;

        protected Executable(string path)
        {
            _path = path;
        }

        public static string Locate(string name)
        {
            foreach (var dir in Environment.GetEnvironmentVariable("PATH").Split(Path.PathSeparator))
            {
                var candidate = Path.Combine(dir, name + _exeSuffix);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            return null;
        }

        public async Task<int> ExecAsync(string args, CancellationToken cancellationToken)
        {
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = _path,
                    Arguments = args,
                    UseShellExecute = false,
                },
                EnableRaisingEvents = true
            };
            var tcs = new TaskCompletionSource<int>();

            using (cancellationToken.Register(() => Cancel(process, tcs)))
            {
                process.Exited += (_, __) => tcs.TrySetResult(process.ExitCode);

                process.Start();

                return await tcs.Task;
            }
        }

        private static void Cancel(Process process, TaskCompletionSource<int> tcs)
        {
            if (process != null && !process.HasExited)
            {
                process.Kill();
            }
            tcs.TrySetCanceled();
        }
    }
}
