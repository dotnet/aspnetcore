// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.AspNetCore.Testing
{
    public class OSSkipConditionTest
    {
        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        public void TestSkipLinux()
        {
            Assert.False(
                OperatingSystem.IsLinux(),
                "Test should not be running on Linux");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public void TestSkipMacOSX()
        {
            Assert.False(
                OperatingSystem.IsMacOS(),
                "Test should not be running on MacOSX.");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows)]
        public void TestSkipWindows()
        {
            Assert.False(
                OperatingSystem.IsWindows(),
                "Test should not be running on Windows.");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        public void TestSkipLinuxAndMacOSX()
        {
            Assert.False(
                OperatingSystem.IsLinux(),
                "Test should not be running on Linux.");
            Assert.False(
                OperatingSystem.IsMacOS(),
                "Test should not be running on MacOSX.");
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [InlineData(1)]
        public void TestTheorySkipLinux(int arg)
        {
            Assert.False(
                OperatingSystem.IsLinux(),
                "Test should not be running on Linux");
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData(1)]
        public void TestTheorySkipMacOS(int arg)
        {
            Assert.False(
                OperatingSystem.IsMacOS(),
                "Test should not be running on MacOSX.");
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Windows)]
        [InlineData(1)]
        public void TestTheorySkipWindows(int arg)
        {
            Assert.False(
                OperatingSystem.IsWindows(),
                "Test should not be running on Windows.");
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
        [InlineData(1)]
        public void TestTheorySkipLinuxAndMacOSX(int arg)
        {
            Assert.False(
                OperatingSystem.IsLinux(),
                "Test should not be running on Linux.");
            Assert.False(
                OperatingSystem.IsMacOS(),
                "Test should not be running on MacOSX.");
        }
    }

    [OSSkipCondition(OperatingSystems.Windows)]
    public class OSSkipConditionClassTest
    {
        [ConditionalFact]
        public void TestSkipClassWindows()
        {
            Assert.False(
                OperatingSystem.IsWindows(),
                "Test should not be running on Windows.");
        }
    }
}
