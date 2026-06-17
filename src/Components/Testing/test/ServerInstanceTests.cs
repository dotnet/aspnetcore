// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Testing.Infrastructure;

namespace Microsoft.AspNetCore.Components.Testing.Tests;

public class ServerInstanceTests
{
    [Fact]
    public void ComputeKey_AppNameOnly_ReturnsAppName()
    {
        // Arrange
        var options = new ServerStartOptions();

        // Act
        var key = ServerInstance.ComputeKey("MyApp", options);

        // Assert
        Assert.Equal("MyApp", key);
    }

    [Fact]
    public void ComputeKey_WithServiceOverride_IncludesTypeAndMethod()
    {
        // Arrange
        var options = new ServerStartOptions();
        options.ConfigureServices<ServerInstanceTests>("Configure");

        // Act
        var key = ServerInstance.ComputeKey("MyApp", options);

        // Assert
        Assert.StartsWith("MyApp|", key);
        Assert.Contains(typeof(ServerInstanceTests).AssemblyQualifiedName!, key);
        Assert.Contains(":Configure", key);
    }

    [Fact]
    public void ComputeKey_WithEnvVars_IncludesSortedKeyValues()
    {
        // Arrange
        var options = new ServerStartOptions();
        options.EnvironmentVariables["Z_VAR"] = "z";
        options.EnvironmentVariables["A_VAR"] = "a";

        // Act
        var key = ServerInstance.ComputeKey("MyApp", options);

        // Assert — env vars sorted by key
        var aIndex = key.IndexOf("A_VAR=a");
        var zIndex = key.IndexOf("Z_VAR=z");
        Assert.True(aIndex < zIndex, "Environment variables should be sorted by key");
    }

    [Fact]
    public void ComputeKey_SameInputs_ProducesSameKey()
    {
        // Arrange
        var options1 = new ServerStartOptions();
        options1.ConfigureServices<string>("Method");
        options1.EnvironmentVariables["KEY"] = "val";

        var options2 = new ServerStartOptions();
        options2.ConfigureServices<string>("Method");
        options2.EnvironmentVariables["KEY"] = "val";

        // Act
        var key1 = ServerInstance.ComputeKey("App", options1);
        var key2 = ServerInstance.ComputeKey("App", options2);

        // Assert
        Assert.Equal(key1, key2);
    }

    [Fact]
    public void ComputeKey_DifferentApps_ProducesDifferentKeys()
    {
        // Arrange
        var options = new ServerStartOptions();

        // Act
        var key1 = ServerInstance.ComputeKey("App1", options);
        var key2 = ServerInstance.ComputeKey("App2", options);

        // Assert
        Assert.NotEqual(key1, key2);
    }
}
