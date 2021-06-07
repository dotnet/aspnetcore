using System;
using System.IO;
using static System.Environment;

#nullable enable

namespace Microsoft.AspNetCore.Certificates.Generation
{
    internal static class Paths
    {
        public static string Home => Environment.GetFolderPath(SpecialFolder.MyDocuments);

        public static string GetUserTempFile(string suffix = ".tmp")
        {
            string directory = Paths.XdgRuntimeDir ?? Home ?? // Should be user folders.
                               Path.GetTempPath();            // Probably global on Linux.

            return Path.Combine(directory, Guid.NewGuid() + suffix);
        }

        private static string? XdgRuntimeDir => Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
    }
}