using System;
using static System.Environment;

#nullable enable

namespace Microsoft.AspNetCore.Certificates.Generation
{
    internal static class Paths
    {
        public static string? XdgRuntimeDir => Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
        public static string Home => Environment.GetFolderPath(SpecialFolder.MyDocuments, SpecialFolderOption.DoNotVerify);
    }
}