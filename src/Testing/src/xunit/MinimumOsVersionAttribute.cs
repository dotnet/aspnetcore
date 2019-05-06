// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Microsoft.AspNetCore.Testing.xunit
{
    /// <summary>
    /// Skips a test if the OS is the given type (Windows) and the OS version is less than specified.
    /// E.g. Specifying Window 10.0 skips on Win 8, but not on Linux. Combine with OSSkipConditionAttribute as needed.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public class MinimumOSVersionAttribute : Attribute, ITestCondition
    {
        private readonly OperatingSystems _excludedOperatingSystem;
        private readonly Version _minVersion;
        private readonly OperatingSystems _osPlatform;
        private readonly Version _osVersion;

        public MinimumOSVersionAttribute(OperatingSystems operatingSystem, string minVersion) :
            this(
                operatingSystem,
                GetCurrentOS(),
                GetCurrentOSVersion(),
                Version.Parse(minVersion))
        {
        }

        // to enable unit testing
        internal MinimumOSVersionAttribute(
            OperatingSystems operatingSystem, OperatingSystems osPlatform, Version osVersion, Version minVersion)
        {
            if (operatingSystem != OperatingSystems.Windows)
            {
                throw new NotImplementedException("Min version support is only implemented for Windows.");
            }
            _excludedOperatingSystem = operatingSystem;
            _minVersion = minVersion;
            _osPlatform = osPlatform;
            _osVersion = osVersion;

            SkipReason = $"This test requires {_excludedOperatingSystem} {_minVersion} or later.";
        }

        public bool IsMet
        {
            get
            {
                // Do not skip other OS's, Use OSSkipConditionAttribute or a separate MinimumOSVersionAttribute for that.
                if (_osPlatform != _excludedOperatingSystem)
                {
                    return true;
                }

                return _osVersion >= _minVersion;
            }
        }

        public string SkipReason { get; set; }

        private static OperatingSystems GetCurrentOS()
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

        private static Version GetCurrentOSVersion()
        {
            // currently not used on other OS's
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Win10+
                var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                var major = key.GetValue("CurrentMajorVersionNumber") as int?;
                var minor = key.GetValue("CurrentMinorVersionNumber") as int?;

                if (major.HasValue && minor.HasValue)
                {
                    return new Version(major.Value, minor.Value);
                }

                // CurrentVersion doesn't work past Win8.1
                var current = key.GetValue("CurrentVersion") as string;
                if (!string.IsNullOrEmpty(current) && Version.TryParse(current, out var currentVersion))
                {
                    return currentVersion;
                }

                // Environment.OSVersion doesn't work past Win8.
                return Environment.OSVersion.Version;
            }
            else
            {
                return new Version();
            }
        }
    }
}
