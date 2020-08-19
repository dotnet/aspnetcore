// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.AspNetCore.Testing
{
    public class OSSkipConditionAttributeTest
    {
        [Fact]
        public void Skips_WhenOperatingSystemMatches()
        {
            // Act
            var osSkipAttribute = new OSSkipConditionAttribute(
                OperatingSystems.Windows,
                OperatingSystems.Windows);

            // Assert
            Assert.False(osSkipAttribute.IsMet);
        }

        [Fact]
        public void DoesNotSkip_WhenOperatingSystemDoesNotMatch()
        {
            // Act
            var osSkipAttribute = new OSSkipConditionAttribute(
                OperatingSystems.Linux,
                OperatingSystems.Windows);

            // Assert
            Assert.True(osSkipAttribute.IsMet);
        }

        [Fact]
        public void Skips_BothMacOSXAndLinux()
        {
            // Act
            var osSkipAttributeLinux = new OSSkipConditionAttribute(OperatingSystems.Linux | OperatingSystems.MacOSX, OperatingSystems.Linux);
            var osSkipAttributeMacOSX = new OSSkipConditionAttribute(OperatingSystems.Linux | OperatingSystems.MacOSX, OperatingSystems.MacOSX);

            // Assert
            Assert.False(osSkipAttributeLinux.IsMet);
            Assert.False(osSkipAttributeMacOSX.IsMet);
        }

        [Fact]
        public void Skips_BothMacOSXAndWindows()
        {
            // Act
            var osSkipAttribute = new OSSkipConditionAttribute(OperatingSystems.Windows | OperatingSystems.MacOSX, OperatingSystems.Windows);
            var osSkipAttributeMacOSX = new OSSkipConditionAttribute(OperatingSystems.Windows | OperatingSystems.MacOSX, OperatingSystems.MacOSX);

            // Assert
            Assert.False(osSkipAttribute.IsMet);
            Assert.False(osSkipAttributeMacOSX.IsMet);
        }

        [Fact]
        public void Skips_BothWindowsAndLinux()
        {
            // Act
            var osSkipAttribute = new OSSkipConditionAttribute(OperatingSystems.Linux | OperatingSystems.Windows, OperatingSystems.Windows);
            var osSkipAttributeLinux = new OSSkipConditionAttribute(OperatingSystems.Linux | OperatingSystems.Windows, OperatingSystems.Linux);

            // Assert
            Assert.False(osSkipAttribute.IsMet);
            Assert.False(osSkipAttributeLinux.IsMet);
        }
    }
}
