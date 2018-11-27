// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    public class NullableCompatibilitySwitchTest
    {
        [Fact]
        public void Constructor_WithName_IsValueSetIsFalse()
        {
            // Arrange & Act
            var @switch = new NullableCompatibilitySwitch<bool>("TestProperty");

            // Assert
            Assert.Null(@switch.Value);
            Assert.False(@switch.IsValueSet);
        }

        [Fact]
        public void ValueNonInterface_SettingValueToNull_SetsIsValueSetToTrue()
        {
            // Arrange
            var @switch = new NullableCompatibilitySwitch<bool>("TestProperty");

            // Act
            @switch.Value = null;

            // Assert
            Assert.Null(@switch.Value);
            Assert.True(@switch.IsValueSet);
        }

        [Fact]
        public void ValueNonInterface_SettingValue_SetsIsValueSetToTrue()
        {
            // Arrange
            var @switch = new NullableCompatibilitySwitch<bool>("TestProperty");

            // Act
            @switch.Value = false;

            // Assert
            Assert.False(@switch.Value);
            Assert.True(@switch.IsValueSet);
        }

        [Fact]
        public void ValueInterface_SettingValueToNull_SetsIsValueSetToTrue()
        {
            // Arrange
            var @switch = new NullableCompatibilitySwitch<bool>("TestProperty");

            // Act
            ((ICompatibilitySwitch)@switch).Value = null;

            // Assert
            Assert.Null(@switch.Value);
            Assert.True(@switch.IsValueSet);
        }

        [Fact]
        public void ValueInterface_SettingValue_SetsIsValueSetToTrue()
        {
            // Arrange
            var @switch = new NullableCompatibilitySwitch<bool>("TestProperty");

            // Act
            ((ICompatibilitySwitch)@switch).Value = true;

            // Assert
            Assert.True(@switch.Value);
            Assert.True(@switch.IsValueSet);
        }
    }
}
