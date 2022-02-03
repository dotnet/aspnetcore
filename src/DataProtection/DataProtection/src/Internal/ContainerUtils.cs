// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.DataProtection.Internal;

internal static class ContainerUtils
{
    private static readonly Lazy<bool> _isContainer = new Lazy<bool>(IsProcessRunningInContainer);
    private const string RunningInContainerVariableName = "DOTNET_RUNNING_IN_CONTAINER";
    private const string DeprecatedRunningInContainerVariableName = "DOTNET_RUNNING_IN_CONTAINERS";

    public static bool IsContainer => _isContainer.Value;

    public static bool IsVolumeMountedFolder(DirectoryInfo directory)
    {
        if (!IsContainer)
        {
            return false;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // we currently don't have a good way to detect mounted file systems within Windows containers
            return false;
        }

        const string mountsFile = "/proc/self/mounts";
        if (!File.Exists(mountsFile))
        {
            return false;
        }

        var lines = File.ReadAllLines(mountsFile);
        return IsDirectoryMounted(directory, lines);
    }

    // internal for testing. Don't use directly
    internal static bool IsDirectoryMounted(DirectoryInfo directory, IEnumerable<string> fstab)
    {
        // Expected file format: http://man7.org/linux/man-pages/man5/fstab.5.html
        foreach (var line in fstab)
        {
            if (line == null || line.Length == 0 || line[0] == '#')
            {
                // skip empty and commented-out lines
                continue;
            }

            var fields = line.Split(new[] { '\t', ' ' });

            if (fields.Length < 2 // line had too few fields
                || fields[1].Length <= 1 // fs_file empty or is the root directory '/'
                || fields[1][0] != '/') // fs_file was not a file path
            {
                continue;
            }

            // check if directory is a subdirectory of this location
            var fs_file = new DirectoryInfo(fields[1].TrimEnd(Path.DirectorySeparatorChar)).FullName;
            var dir = directory;
            while (dir != null)
            {
                // filesystems on Linux are case sensitive
                if (fs_file.Equals(dir.FullName.TrimEnd(Path.DirectorySeparatorChar), StringComparison.Ordinal))
                {
                    return true;
                }

                dir = dir.Parent;
            }
        }

        return false;
    }

    private static bool IsProcessRunningInContainer()
    {
        // Official .NET Core images (Windows and Linux) set this. So trust it if it's there.
        // We check both DOTNET_RUNNING_IN_CONTAINER (the current name) and DOTNET_RUNNING_IN_CONTAINERS (a deprecated name used in some images).
        if (GetBooleanEnvVar(RunningInContainerVariableName) || GetBooleanEnvVar(DeprecatedRunningInContainerVariableName))
        {
            return true;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // we currently don't have a good way to detect if running in a Windows container
            return false;
        }

        // Try to detect docker using the cgroups process 1 is in.
        const string procFile = "/proc/1/cgroup";
        if (!File.Exists(procFile))
        {
            return false;
        }

        var lines = File.ReadAllLines(procFile);
        // typically the last line in the file is "1:name=openrc:/docker"
        return lines.Reverse().Any(l => l.EndsWith("name=openrc:/docker", StringComparison.Ordinal));
    }

    private static bool GetBooleanEnvVar(string envVarName)
    {
        var value = Environment.GetEnvironmentVariable(envVarName);
        return string.Equals(value, "1", StringComparison.Ordinal) ||
            string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }
}
