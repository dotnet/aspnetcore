// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Testing.Infrastructure;

namespace Microsoft.AspNetCore.Components.Testing.Tests;

public class ServerStartOptionsTests
{
    [Fact]
    public void DefaultTimeout_Is60Seconds()
    {
        // Arrange & Act
        var options = new ServerStartOptions();

        // Assert
        Assert.Equal(60_000, options.ReadinessTimeoutMs);
    }

    [Fact]
    public void ConfigureServices_Generic_CapturesTypeAndMethod()
    {
        // Arrange
        var options = new ServerStartOptions();

        // Act
        options.ConfigureServices<ServerStartOptionsTests>("MyMethod");

        // Assert
        Assert.Equal(typeof(ServerStartOptionsTests).AssemblyQualifiedName, options.ServiceOverrideTypeName);
        Assert.Equal("MyMethod", options.ServiceOverrideMethodName);
    }

    [Fact]
    public void ConfigureServices_NonGeneric_CapturesTypeAndMethod()
    {
        // Arrange
        var options = new ServerStartOptions();
        var type = typeof(string);

        // Act
        options.ConfigureServices(type, "Configure");

        // Assert
        Assert.Equal(type.AssemblyQualifiedName, options.ServiceOverrideTypeName);
        Assert.Equal("Configure", options.ServiceOverrideMethodName);
    }

    [Fact]
    public void ConfigureServices_CalledTwice_OverwritesPrevious()
    {
        // Arrange
        var options = new ServerStartOptions();

        // Act
        options.ConfigureServices<string>("First");
        options.ConfigureServices<int>("Second");

        // Assert
        Assert.Equal(typeof(int).AssemblyQualifiedName, options.ServiceOverrideTypeName);
        Assert.Equal("Second", options.ServiceOverrideMethodName);
    }

    [Fact]
    public void EnvironmentVariables_IsEmptyByDefault()
    {
        // Arrange & Act
        var options = new ServerStartOptions();

        // Assert
        Assert.Empty(options.EnvironmentVariables);
    }

    [Fact]
    public void EnvironmentVariables_CanAddMultiple()
    {
        // Arrange
        var options = new ServerStartOptions();

        // Act
        options.EnvironmentVariables["KEY1"] = "value1";
        options.EnvironmentVariables["KEY2"] = "value2";

        // Assert
        Assert.Equal(2, options.EnvironmentVariables.Count);
        Assert.Equal("value1", options.EnvironmentVariables["KEY1"]);
        Assert.Equal("value2", options.EnvironmentVariables["KEY2"]);
    }

    [Fact]
    public void ServiceOverride_IsNullByDefault()
    {
        // Arrange & Act
        var options = new ServerStartOptions();

        // Assert
        Assert.Null(options.ServiceOverrideTypeName);
        Assert.Null(options.ServiceOverrideMethodName);
    }

    [Fact]
    public void ReadinessTimeoutMs_CanBeCustomized()
    {
        // Arrange
        var options = new ServerStartOptions();

        // Act
        options.ReadinessTimeoutMs = 120_000;

        // Assert
        Assert.Equal(120_000, options.ReadinessTimeoutMs);
    }
}
