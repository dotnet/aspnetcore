// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Testing.xunit
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public class OSSkipConditionAttribute : Attribute, ITestCondition
    {
        private readonly OperatingSystems _excludedOperatingSystem;
        private readonly IEnumerable<string> _excludedVersions;
        private readonly OperatingSystems _osPlatform;
        private readonly string _osVersion;

        public OSSkipConditionAttribute(OperatingSystems operatingSystem, params string[] versions) :
            this(
                operatingSystem,
                GetCurrentOS(),
                GetCurrentOSVersion(),
                versions)
        {
        }

        // to enable unit testing
        internal OSSkipConditionAttribute(
            OperatingSystems operatingSystem, OperatingSystems osPlatform, string osVersion, params string[] versions)
        {
            _excludedOperatingSystem = operatingSystem;
            _excludedVersions = versions ?? Enumerable.Empty<string>();
            _osPlatform = osPlatform;
            _osVersion = osVersion;
        }

        public bool IsMet
        {
            get
            {
                var currentOSInfo = new OSInfo()
                {
                    OperatingSystem = _osPlatform,
                    Version = _osVersion,
                };

                var skip = (_excludedOperatingSystem & currentOSInfo.OperatingSystem) == currentOSInfo.OperatingSystem;
                if (_excludedVersions.Any())
                {
                    skip = skip
                        && _excludedVersions.Any(ex => _osVersion.StartsWith(ex, StringComparison.OrdinalIgnoreCase));
                }

                // Since a test would be excuted only if 'IsMet' is true, return false if we want to skip
                return !skip;
            }
        }

        public string SkipReason { get; set; } = "Test cannot run on this operating system.";

        static private OperatingSystems GetCurrentOS()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return OperatingSystems.Windows;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return OperatingSystems.Linux;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return OperatingSystems.MacOSX;
            }
            throw new PlatformNotSupportedException();
        }

        static private string GetCurrentOSVersion()
        {
            // currently not used on other OS's
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Environment.OSVersion.Version.ToString();
            }
            else
            {
                return string.Empty;
            }
        }

        private class OSInfo
        {
            public OperatingSystems OperatingSystem { get; set; }

            public string Version { get; set; }
        }
    }
}
