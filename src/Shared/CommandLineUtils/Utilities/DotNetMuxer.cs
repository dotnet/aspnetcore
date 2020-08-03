// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// System.AppContext.GetData is not available in these frameworks
#if !NET451 && !NET452 && !NET46 && !NET461

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Extensions.CommandLineUtils
{
    /// <summary>
    /// Utilities for finding the "dotnet.exe" file from the currently running .NET Core application
    /// </summary>
    internal static class DotNetMuxer
    {
        private const string MuxerName = "dotnet";

        static DotNetMuxer()
        {
            MuxerPath = TryFindMuxerPath();
        }

        /// <summary>
        /// The full filepath to the .NET Core muxer.
        /// </summary>
        public static string MuxerPath { get; }

        /// <summary>
        /// Finds the full filepath to the .NET Core muxer,
        /// or returns a string containing the default name of the .NET Core muxer ('dotnet').
        /// </summary>
        /// <returns>The path or a string named 'dotnet'.</returns>
        public static string MuxerPathOrDefault()
            => MuxerPath ?? MuxerName;

        private static string TryFindMuxerPath()
        {
            var fileName = MuxerName;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                fileName += ".exe";
            }

            var mainModule = Process.GetCurrentProcess().MainModule;
            if (!string.IsNullOrEmpty(mainModule?.FileName)
                && Path.GetFileName(mainModule.FileName).Equals(fileName, StringComparison.OrdinalIgnoreCase))
            {
                return mainModule.FileName;
            }
            
            return null;
        }
    }
}
#endif
