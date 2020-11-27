// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Xunit;

namespace Microsoft.AspNetCore.Testing
{
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
    public class MaximumOSVersionTest
    {
        [ConditionalFact]
        [MaximumOSVersion(OperatingSystems.Windows, WindowsVersions.Win7)]
        public void RunTest_Win7DoesRunOnWin7()
        {
            Assert.True(
                OperatingSystem.IsWindows() &&
                Environment.OSVersion.Version.ToString().StartsWith("6.1", StringComparison.Ordinal),
                "Test should only be running on Win7 or Win2008R2.");
        }

        [ConditionalTheory]
        [MaximumOSVersion(OperatingSystems.Windows, WindowsVersions.Win7)]
        [InlineData(1)]
        public void RunTheory_Win7DoesRunOnWin7(int arg)
        {
            Assert.True(
                OperatingSystem.IsWindows() &&
                Environment.OSVersion.Version.ToString().StartsWith("6.1", StringComparison.Ordinal),
                "Test should only be running on Win7 or Win2008R2.");
        }

        [ConditionalFact]
        [MaximumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10_RS4)]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public void RunTest_Win10_RS4()
        {
            Assert.True(OperatingSystem.IsWindows());
            var versionKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            Assert.NotNull(versionKey);
            var currentVersion = (string)versionKey.GetValue("CurrentBuildNumber");
            Assert.NotNull(currentVersion);
            Assert.True(17134 >= int.Parse(currentVersion, CultureInfo.InvariantCulture));
        }

        [ConditionalFact]
        [MaximumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10_19H2)]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public void RunTest_Win10_19H2()
        {
            Assert.True(OperatingSystem.IsWindows());
            var versionKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            Assert.NotNull(versionKey);
            var currentVersion = (string)versionKey.GetValue("CurrentBuildNumber");
            Assert.NotNull(currentVersion);
            Assert.True(18363 >= int.Parse(currentVersion, CultureInfo.InvariantCulture));
        }
    }

    [MaximumOSVersion(OperatingSystems.Windows, WindowsVersions.Win7)]
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
    public class OSMaxVersionClassTest
    {
        [ConditionalFact]
        public void TestSkipClass_Win7DoesRunOnWin7()
        {
            Assert.True(
                OperatingSystem.IsWindows() &&
                Environment.OSVersion.Version.ToString().StartsWith("6.1", StringComparison.Ordinal),
                "Test should only be running on Win7 or Win2008R2.");
        }
    }

    // Let this one run cross plat just to check the constructor logic.
    [MaximumOSVersion(OperatingSystems.Windows, WindowsVersions.Win7)]
    public class OSMaxVersionCrossPlatTest
    {
        [ConditionalFact]
        public void TestSkipClass_Win7DoesRunOnWin7()
        {
            if (OperatingSystem.IsWindows())
            {
                Assert.True(Environment.OSVersion.Version.ToString().StartsWith("6.1", StringComparison.Ordinal),
                    "Test should only be running on Win7 or Win2008R2.");
            }
        }
    }
}
