// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.AspNetCore.Razor.Tools
{
    internal static class PipeName
    {
        // We want each pipe to unique and predictable based on the inputs of:
        // - user (security)
        // - path of tool on disk (version)
        // 
        // This allows us to meet the security and version compat requirements just by selecting a pipe name.
        //
        // This is similar to (and based on) the code used by Roslyn in VBCSCompiler:
        // https://github.com/dotnet/roslyn/blob/c273b6a9f19570a344c274ae89185b3a2b64d93d/src/Compilers/Shared/BuildServerConnection.cs#L528
        public static string ComputeDefault(string toolDirectory = null)
        {
            if (string.IsNullOrEmpty(toolDirectory))
            {
                // This can be null in cases where we don't have a way of knowing the tool assembly path like when someone manually
                // invokes the cli tool without passing in a pipe name as argument.
                toolDirectory = AppDomain.CurrentDomain.BaseDirectory;
            }

            // Include a prefix so we can't conflict with VBCSCompiler if we somehow end up in the same directory.
            // That would be a pretty wacky bug to try and unravel.
            var baseName = ComputeBaseName("Razor:" + toolDirectory);

            // Prefix with username
            var userName = Environment.UserName;
            if (userName == null)
            {
                return null;
            }

            return $"{userName}.{baseName}";
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
                    .Substring(0, 25) // We only have ~50 total characters on Mac, so strip that down
                    .Replace("/", "_")
                    .Replace("=", string.Empty);
            }
        }
    }
}
