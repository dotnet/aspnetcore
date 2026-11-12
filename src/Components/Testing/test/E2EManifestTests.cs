// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;

namespace Microsoft.AspNetCore.Components.Testing.Tests;

public class E2EManifestTests
{
    [Fact]
    public void Deserialize_ValidJson_ReturnsManifest()
    {
        var json = """
            {
                "apps": {
                    "MyApp": {
                        "executable": "dotnet",
                        "arguments": "run --no-launch-profile",
                        "workingDirectory": "/path/to/MyApp",
                        "publicUrl": "https://localhost:5001",
                        "environmentVariables": {
                            "ASPNETCORE_ENVIRONMENT": "Development"
                        }
                    }
                }
            }
            """;

        var manifest = JsonSerializer.Deserialize<E2EManifest>(json);

        Assert.NotNull(manifest);
        Assert.Single(manifest!.Apps);
        Assert.True(manifest.Apps.ContainsKey("MyApp"));

        var app = manifest.Apps["MyApp"];
        Assert.Equal("dotnet", app.Executable);
        Assert.Equal("run --no-launch-profile", app.Arguments);
        Assert.Equal("/path/to/MyApp", app.WorkingDirectory);
        Assert.Equal("https://localhost:5001", app.PublicUrl);
        Assert.Equal("Development", app.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"]);
        Assert.Equal(E2EAppEntry.StartupHookHarnessMode, app.HarnessMode);
    }

    [Fact]
    public void Deserialize_WithPublishedApp_ReturnsPublishedDetails()
    {
        var json = """
            {
                "apps": {
                    "PublishedApp": {
                        "executable": "PublishedApp.exe",
                        "arguments": "",
                        "workingDirectory": "e2e-apps/PublishedApp",
                        "environmentVariables": {}
                    }
                }
            }
            """;

        var manifest = JsonSerializer.Deserialize<E2EManifest>(json);

        Assert.NotNull(manifest);
        var app = manifest!.Apps["PublishedApp"];
        Assert.Equal("PublishedApp.exe", app.Executable);
        Assert.Equal("", app.Arguments);
        Assert.Equal("e2e-apps/PublishedApp", app.WorkingDirectory);
    }

    [Fact]
    public void Deserialize_EmptyApps_ReturnsEmptyDictionary()
    {
        var json = """{ "apps": {} }""";

        var manifest = JsonSerializer.Deserialize<E2EManifest>(json);

        Assert.NotNull(manifest);
        Assert.Empty(manifest!.Apps);
    }

    [Fact]
    public void GetApp_ExistingKey_ReturnsEntry()
    {
        var manifest = new E2EManifest();
        manifest.Apps["TestApp"] = new E2EAppEntry { Executable = "dotnet", Arguments = "run --no-launch-profile", WorkingDirectory = "/test" };

        var result = manifest.GetApp("TestApp");

        Assert.NotNull(result);
        Assert.Equal("dotnet", result!.Executable);
    }

    [Fact]
    public void GetApp_MissingKey_ReturnsNull()
    {
        var manifest = new E2EManifest();

        var result = manifest.GetApp("NonExistent");

        Assert.Null(result);
    }

    [Fact]
    public void Load_MissingManifest_ThrowsFileNotFoundException()
    {
        var assemblyName = "NonExistentAssembly_" + Guid.NewGuid().ToString("N");

        var ex = Assert.Throws<FileNotFoundException>(() => E2EManifest.Load(assemblyName));
        Assert.Contains("E2E manifest not found", ex.Message);
        Assert.Contains("Microsoft.AspNetCore.Components.Testing.targets", ex.Message);
    }

    [Fact]
    public void Deserialize_AllMode_HasBothBuildAndPublishedEntries()
    {
        var json = """
            {
                "apps": {
                    "MyApp": {
                        "executable": "dotnet",
                        "arguments": "run --no-launch-profile",
                        "workingDirectory": "e2e-apps/MyApp",
                        "environmentVariables": {}
                    },
                    "publish/MyApp": {
                        "executable": "MyApp.exe",
                        "arguments": "",
                        "workingDirectory": "e2e-apps/publish/MyApp",
                        "environmentVariables": {}
                    }
                }
            }
            """;

        var manifest = JsonSerializer.Deserialize<E2EManifest>(json);

        Assert.NotNull(manifest);
        Assert.Equal(2, manifest!.Apps.Count);

        var buildEntry = manifest.GetApp("MyApp");
        Assert.NotNull(buildEntry);
        Assert.Equal("dotnet", buildEntry!.Executable);
        Assert.Equal("run --no-launch-profile", buildEntry.Arguments);
        Assert.Equal("e2e-apps/MyApp", buildEntry.WorkingDirectory);

        var publishedEntry = manifest.GetApp("publish/MyApp");
        Assert.NotNull(publishedEntry);
        Assert.Equal("MyApp.exe", publishedEntry!.Executable);
        Assert.Equal("", publishedEntry.Arguments);
        Assert.Equal("e2e-apps/publish/MyApp", publishedEntry.WorkingDirectory);
    }

    [Fact]
    public void Deserialize_MultipleApps_AllPresent()
    {
        var json = """
            {
                "apps": {
                    "App1": { "executable": "dotnet", "arguments": "run --no-launch-profile", "workingDirectory": "/app1", "environmentVariables": {} },
                    "App2": { "executable": "dotnet", "arguments": "run --no-launch-profile", "workingDirectory": "/app2", "environmentVariables": {} },
                    "App3": { "executable": "dotnet", "arguments": "run --no-launch-profile", "workingDirectory": "/app3", "environmentVariables": {} }
                }
            }
            """;

        var manifest = JsonSerializer.Deserialize<E2EManifest>(json);

        Assert.NotNull(manifest);
        Assert.Equal(3, manifest!.Apps.Count);
        Assert.NotNull(manifest.GetApp("App1"));
        Assert.NotNull(manifest.GetApp("App2"));
        Assert.NotNull(manifest.GetApp("App3"));
    }

    [Fact]
    public void Deserialize_CompiledHarness_PreservesBehaviorMode()
    {
        var json = """
            {
                "apps": {
                    "NativeApp": {
                        "executable": "NativeApp",
                        "arguments": "",
                        "workingDirectory": "e2e-apps/NativeApp",
                        "environmentVariables": {},
                        "harnessMode": "compiled"
                    }
                }
            }
            """;

        var manifest = JsonSerializer.Deserialize<E2EManifest>(json);

        var app = Assert.Single(manifest!.Apps).Value;
        Assert.Equal(E2EAppEntry.CompiledHarnessMode, app.HarnessMode);
        Assert.Equal("NativeApp", app.Executable);
    }

    [Fact]
    public void Validate_UnknownHarnessMode_Throws()
    {
        var manifest = new E2EManifest();
        manifest.Apps["App"] = new E2EAppEntry
        {
            Executable = "App",
            HarnessMode = "unknown",
        };

        var exception = Assert.Throws<InvalidOperationException>(manifest.Validate);

        Assert.Contains("unsupported harnessMode 'unknown'", exception.Message);
        Assert.Contains("startupHook", exception.Message);
        Assert.Contains("compiled", exception.Message);
    }

    [Fact]
    public void Validate_CompiledHarnessUsingDotnet_Throws()
    {
        var manifest = new E2EManifest();
        manifest.Apps["App"] = new E2EAppEntry
        {
            Executable = "dotnet",
            Arguments = "App.dll",
            HarnessMode = E2EAppEntry.CompiledHarnessMode,
        };

        var exception = Assert.Throws<InvalidOperationException>(manifest.Validate);

        Assert.Contains("does not point to a native executable", exception.Message);
    }
}
