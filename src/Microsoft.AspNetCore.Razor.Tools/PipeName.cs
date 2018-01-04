// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.AspNetCore.Razor.TagHelperTool
{
    internal static class PipeName
    {
        // We want each pipe to unique and predictable based on the inputs of:
        // - user (security)
        // - elevation status (security)
        // - path of tool on disk (version)
        // 
        // This allows us to meet the security and version compat requirements just by selecting a pipe name.
        //
        // https://github.com/dotnet/corefx/issues/25427 will actually enforce the security, but we still
        // want these guarantees when we try to connect so we can expect it to succeed.
        //
        // This is similar to (and based on) the code used by Roslyn in VBCSCompiler:
        // https://github.com/dotnet/roslyn/blob/c273b6a9f19570a344c274ae89185b3a2b64d93d/src/Compilers/Shared/BuildServerConnection.cs#L528
        public static string ComputeDefault()
        {
            // Include a prefix so we can't conflict with VBCSCompiler if we somehow end up in the same directory.
            // That would be a pretty wacky bug to try and unravel.
            var baseName = ComputeBaseName("Razor:" + AppDomain.CurrentDomain.BaseDirectory);

            // Prefix with username and elevation
            bool isAdmin = false;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
#if WINDOWS_HACK_LOL
                var currentIdentity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(currentIdentity);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
#endif
            }

            var userName = Environment.UserName;
            if (userName == null)
            {
                return null;
            }

            return $"{userName}.{isAdmin}.{baseName}";
        }
        
        private static string ComputeBaseName(string baseDirectory)
        {
            // Normalize away trailing slashes. File APIs are not consistent about including it, so it's
            // best to normalize and avoid ending up with two servers running accidentally.
            baseDirectory = baseDirectory.TrimEnd(Path.DirectorySeparatorChar);

            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(baseDirectory));
                return Convert.ToBase64String(bytes)
                    .Replace("/", "_")
                    .Replace("=", string.Empty);
            }
        }
    }
}
