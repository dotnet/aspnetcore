using System;
using System.Diagnostics;
using System.IO;
using System.Text;

#nullable enable

namespace Microsoft.AspNetCore.Certificates.Generation
{
    internal static class ProcessHelper
    {
        public static string GetCommandLine(ProcessStartInfo psi)
            => $"{psi.FileName} {string.Join(" ", psi.ArgumentList)}";

        public static bool HasProgram(string program)
        {
            string path;
            string? pathEnvVar = Environment.GetEnvironmentVariable("PATH");
            if (pathEnvVar != null)
            {
                string[] pathItems = pathEnvVar.Split(':', StringSplitOptions.RemoveEmptyEntries);
                foreach (var pathItem in pathItems)
                {
                    path = Path.Combine(pathItem, program);
                    if (File.Exists(path))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}