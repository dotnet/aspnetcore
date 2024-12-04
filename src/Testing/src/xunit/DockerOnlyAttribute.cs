// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.InternalTesting;

[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public sealed class DockerOnlyAttribute : Attribute, ITestCondition
{
    public string SkipReason { get; } = "This test can only run in a Docker container.";

    public bool IsMet
    {
        get
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
            return Enumerable.Reverse(lines).Any(l => l.EndsWith("name=openrc:/docker", StringComparison.Ordinal));
        }
    }
}
