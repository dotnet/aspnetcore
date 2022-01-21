// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

public class CompatibilitySwitchTest
{
    [Fact]
    public void Constructor_WithName_IsValueSetIsFalse()
    {
        // Arrange & Act
        var @switch = new CompatibilitySwitch<bool>("TestProperty");

        // Assert
        Assert.False(@switch.Value);
        Assert.False(@switch.IsValueSet);
    }

    [Fact]
    public void Constructor_WithNameAndInitialValue_IsValueSetIsFalse()
    {
        // Arrange & Act
        var @switch = new CompatibilitySwitch<bool>("TestProperty", initialValue: true);

        // Assert
        Assert.True(@switch.Value);
        Assert.False(@switch.IsValueSet);
    }

    [Fact]
    public void ValueNonInterface_SettingValue_SetsIsValueSetToTrue()
    {
        // Arrange
        var @switch = new CompatibilitySwitch<bool>("TestProperty");

        // Act
        @switch.Value = false; // You don't need to actually change the value, just calling the setting works

        // Assert
        Assert.False(@switch.Value);
        Assert.True(@switch.IsValueSet);
    }

    [Fact]
    public void ValueInterface_SettingValue_SetsIsValueSetToTrue()
    {
        // Arrange
        var @switch = new CompatibilitySwitch<bool>("TestProperty");

        // Act
        ((ICompatibilitySwitch)@switch).Value = true;

        // Assert
        Assert.True(@switch.Value);
        Assert.True(@switch.IsValueSet);
    }
}
