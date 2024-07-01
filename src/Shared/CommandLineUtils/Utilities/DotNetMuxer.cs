// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// System.AppContext.GetData is not available in these frameworks
#nullable enable

#if !NETFRAMEWORK

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Microsoft.Extensions.CommandLineUtils;

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
    public static string? MuxerPath { get; }

    /// <summary>
    /// Finds the full filepath to the .NET Core muxer,
    /// or returns a string containing the default name of the .NET Core muxer ('dotnet').
    /// </summary>
    /// <returns>The path or a string named 'dotnet'.</returns>
    public static string MuxerPathOrDefault()
        => MuxerPath ?? MuxerName;

    private static string? TryFindMuxerPath()
    {
        // If not running on Helix, use a custom .NET host, if specified.
        // This allows test projects to use a .NET host with the custom-built
        // ASP.NET Core shared framework.
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("helix")))
        {
            var dotNetHostOverride = typeof(DotNetMuxer).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .SingleOrDefault(a => a.Key == "DotNetHostOverride")?.Value;
            if (dotNetHostOverride is not null)
            {
                return dotNetHostOverride;
            }
        }

        var expectedFileName = MuxerName;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            expectedFileName += ".exe";
        }

        // If the currently running process is dotnet(.exe), return that path
        var mainModuleFullPath = Process.GetCurrentProcess().MainModule?.FileName;
        var mainModuleFileName = Path.GetFileName(mainModuleFullPath);
        if (string.Equals(expectedFileName, mainModuleFileName, StringComparison.OrdinalIgnoreCase))
        {
            return mainModuleFullPath;
        }

        // The currently running process may not be dotnet(.exe). For example,
        // it might be "testhost(.exe)" when running tests.
        // In this case, we can get the location where the CLR is installed,
        // and find dotnet(.exe) relative to that path.
        var runtimeDirectory = RuntimeEnvironment.GetRuntimeDirectory();
        var candidateDotNetExePath = Path.Combine(runtimeDirectory, "..", "..", "..", expectedFileName);
        if (File.Exists(candidateDotNetExePath))
        {
            var normalizedPath = Path.GetFullPath(candidateDotNetExePath);
            return normalizedPath;
        }

        return null;
    }
}
#endif
