// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.DataProtection.Internal
{
    internal static class DockerUtils
    {
        private static Lazy<bool> _isDocker = new Lazy<bool>(IsProcessRunningInDocker);

        public static bool IsDocker => _isDocker.Value;

        public static bool IsVolumeMountedFolder(DirectoryInfo directory)
        {
            if (!IsDocker)
            {
                return false;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // we currently don't have a good way to detect mounted file systems within Windows ctonainers
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

        private static bool IsProcessRunningInDocker()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // we currently don't have a good way to detect if running in a Windows container
                return false;
            }

            const string procFile = "/proc/1/cgroup";
            if (!File.Exists(procFile))
            {
                return false;
            }

            var lines = File.ReadAllLines(procFile);
            // typically the last line in the file is "1:name=openrc:/docker"
            return lines.Reverse().Any(l => l.EndsWith("name=openrc:/docker", StringComparison.Ordinal));
        }
    }
}
