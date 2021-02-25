// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Testing
{
    /// <summary>
    /// Skip test if running on Alpine Linux (which uses musl instead of glibc)
    /// </summary>
    public class SkipOnAlpineAttribute : Attribute, ITestCondition
    {
        public SkipOnAlpineAttribute(string issueUrl = "")
        {
            IssueUrl = issueUrl;
        }

        public string IssueUrl { get; }

        public bool IsMet { get; } = !IsAlpine;

        public string SkipReason => "Test cannot run on Alpine Linux.";

        // This logic is borrowed from https://github.com/dotnet/runtime/blob/6a5a78bec9a6e14b4aa52cd5ac558f6cf5c6a211/src/libraries/Common/tests/TestUtilities/System/PlatformDetection.Unix.cs
        private static bool IsAlpine { get; } =
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && File.Exists("/etc/os-release") &&
            File.ReadAllLines("/etc/os-release").Any(line =>
                line.StartsWith("ID=", StringComparison.Ordinal) && line.Substring(3).Trim('"', '\'') == "alpine");
    }
}
