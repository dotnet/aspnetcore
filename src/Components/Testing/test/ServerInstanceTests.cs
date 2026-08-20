// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

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

    [Fact]
    public void WriteStartupFailureArtifacts_WritesStartupStdoutAndStderrFiles()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            nameof(ServerInstanceTests),
            Guid.NewGuid().ToString("N"));
        var instance = new ServerInstance("TestApp", "key", "http://localhost", onDisposed: null);

        try
        {
            var paths = instance.WriteStartupFailureArtifacts(
                directory,
                new InvalidOperationException("Intentional startup failure"));

            Assert.Equal(3, paths.Count);
            Assert.Contains(paths, path => path.EndsWith(".startup.log", StringComparison.Ordinal));
            Assert.Contains(paths, path => path.EndsWith(".stdout.log", StringComparison.Ordinal));
            Assert.Contains(paths, path => path.EndsWith(".stderr.log", StringComparison.Ordinal));
            Assert.Contains("Intentional startup failure", File.ReadAllText(paths[0]));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public void BuildProcessEnvironment_StartupHookHarness_SetsManagedInjectionVariables()
    {
        var entry = new E2EAppEntry();

        var environment = ServerInstance.BuildProcessEnvironment(
            entry,
            new ServerStartOptions(),
            "http://localhost:5001",
            "C:/tests/TestAssembly.dll",
            "TestAssembly",
            "http://localhost:6001/_ready/token");

        Assert.Equal("http://localhost:5001", environment["ASPNETCORE_URLS"]);
        Assert.Equal("http://localhost:5001", environment["E2E_TEST_APP_URL"]);
        Assert.Equal("http://localhost:6001/_ready/token", environment["E2E_READY_URL"]);
        Assert.Equal("TestAssembly", environment["ASPNETCORE_HOSTINGSTARTUPASSEMBLIES"]);
        Assert.Equal("C:/tests/TestAssembly.dll", environment["DOTNET_STARTUP_HOOKS"]);
        Assert.Equal(
            Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            environment["TEST_PARENT_PID"]);
    }

    [Fact]
    public void BuildProcessEnvironment_CompiledHarness_SetsLifecycleVariablesAndOmitsManagedInjection()
    {
        var entry = new E2EAppEntry
        {
            HarnessMode = E2EAppEntry.CompiledHarnessMode,
            EnvironmentVariables =
            {
                ["DOTNET_STARTUP_HOOKS"] = "manifest-hook.dll",
                ["ASPNETCORE_HOSTINGSTARTUPASSEMBLIES"] = "Manifest.Assembly",
            },
        };
        var options = new ServerStartOptions();
        options.EnvironmentVariables["DOTNET_STARTUP_HOOKS"] = "option-hook.dll";

        var environment = ServerInstance.BuildProcessEnvironment(
            entry,
            options,
            "http://localhost:5002",
            "C:/tests/TestAssembly.dll",
            "TestAssembly",
            "http://localhost:6002/_ready/token");

        Assert.Equal("http://localhost:5002", environment["ASPNETCORE_URLS"]);
        Assert.Equal("http://localhost:5002", environment["E2E_TEST_APP_URL"]);
        Assert.Equal("http://localhost:6002/_ready/token", environment["E2E_READY_URL"]);
        Assert.Equal(
            Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            environment["TEST_PARENT_PID"]);
        Assert.DoesNotContain("ASPNETCORE_HOSTINGSTARTUPASSEMBLIES", environment);
        Assert.DoesNotContain("DOTNET_STARTUP_HOOKS", environment);
        Assert.DoesNotContain("E2E_TEST_SERVICES_TYPE", environment);
        Assert.DoesNotContain("E2E_TEST_SERVICES_METHOD", environment);
    }

    [Fact]
    public void BuildProcessEnvironment_CompiledHarnessWithConfigureServices_FailsClearly()
    {
        var entry = new E2EAppEntry { HarnessMode = E2EAppEntry.CompiledHarnessMode };
        var options = new ServerStartOptions();
        options.ConfigureServices<ServerInstanceTests>(nameof(Configure));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ServerInstance.BuildProcessEnvironment(
                entry,
                options,
                "http://localhost:5003",
                "C:/tests/TestAssembly.dll",
                "TestAssembly",
                "http://localhost:6003/_ready/token"));

        Assert.Contains("ConfigureServices cannot be used with a compiled E2E harness", exception.Message);
        Assert.Contains("Native AOT app", exception.Message);
    }

    [Fact]
    public void BuildProcessEnvironment_UnknownHarnessMode_FailsClearly()
    {
        var entry = new E2EAppEntry { HarnessMode = "unknown" };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ServerInstance.BuildProcessEnvironment(
                entry,
                new ServerStartOptions(),
                "http://localhost:5004",
                "C:/tests/TestAssembly.dll",
                "TestAssembly",
                "http://localhost:6004/_ready/token"));

        Assert.Contains("Unsupported E2E harness mode 'unknown'", exception.Message);
    }

    [Fact]
    public void ApplyProcessEnvironment_CompiledHarness_RemovesInheritedManagedInjection()
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo();
        startInfo.Environment["DOTNET_STARTUP_HOOKS"] = "inherited-hook.dll";
        startInfo.Environment["ASPNETCORE_HOSTINGSTARTUPASSEMBLIES"] = "Inherited.Assembly";
        var environment = new Dictionary<string, string>
        {
            ["ASPNETCORE_URLS"] = "http://localhost:5005",
            ["E2E_READY_URL"] = "http://localhost:6005/_ready/token",
        };

        ServerInstance.ApplyProcessEnvironment(startInfo, environment, isCompiledHarness: true);

        Assert.Equal("http://localhost:5005", startInfo.Environment["ASPNETCORE_URLS"]);
        Assert.Equal("http://localhost:6005/_ready/token", startInfo.Environment["E2E_READY_URL"]);
        Assert.DoesNotContain("DOTNET_STARTUP_HOOKS", startInfo.Environment);
        Assert.DoesNotContain("ASPNETCORE_HOSTINGSTARTUPASSEMBLIES", startInfo.Environment);
    }

    private static void Configure(IServiceCollection services)
    {
    }
}
