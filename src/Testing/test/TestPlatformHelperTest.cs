// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Testing
{
    public class TestPlatformHelperTest
    {
        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [OSSkipCondition(OperatingSystems.Windows)]
        public void IsLinux_TrueOnLinux()
        {
            Assert.True(TestPlatformHelper.IsLinux);
            Assert.False(TestPlatformHelper.IsMac);
            Assert.False(TestPlatformHelper.IsWindows);
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.Windows)]
        public void IsMac_TrueOnMac()
        {
            Assert.False(TestPlatformHelper.IsLinux);
            Assert.True(TestPlatformHelper.IsMac);
            Assert.False(TestPlatformHelper.IsWindows);
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public void IsWindows_TrueOnWindows()
        {
            Assert.False(TestPlatformHelper.IsLinux);
            Assert.False(TestPlatformHelper.IsMac);
            Assert.True(TestPlatformHelper.IsWindows);
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.CLR | RuntimeFrameworks.CoreCLR | RuntimeFrameworks.None)]
        public void IsMono_TrueOnMono()
        {
            Assert.True(TestPlatformHelper.IsMono);
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public void IsMono_FalseElsewhere()
        {
            Assert.False(TestPlatformHelper.IsMono);
        }
    }
}
