using System;
using System.Diagnostics;
using System.IO;

namespace Templates.Test.Helpers
{
    internal class AddFirewallExclusion : IDisposable
    {
        private bool _disposedValue = false;
        private readonly string _exclusionPath;

        public AddFirewallExclusion(string exclusionPath)
        {
            if (!File.Exists(exclusionPath))
            {
                throw new FileNotFoundException($"File {exclusionPath} was not found.");
            }

            _exclusionPath = exclusionPath;
            var startInfo = new ProcessStartInfo
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                FileName = "cmd.exe",
                Arguments = $"/c netsh advfirewall firewall add rule name=\"Allow {exclusionPath}\" dir=in action=allow program=\"{exclusionPath}\"",
                UseShellExecute = false,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden,
            };

            Process.Start(startInfo);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    var startInfo = new ProcessStartInfo
                    {
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                        FileName = "cmd.exe",
                        Arguments = $"/c netsh advfirewall firewall delete rule name=\"Allow {_exclusionPath}\"",
                        UseShellExecute = false,
                        Verb = "runas",
                        WindowStyle = ProcessWindowStyle.Hidden,
                    };

                    Process.Start(startInfo);
                }

                _disposedValue = true;
            }
        }
    }
}
