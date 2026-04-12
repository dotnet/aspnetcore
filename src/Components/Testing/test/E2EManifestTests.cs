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
                        "arguments": "run --no-launch-profile --project /path/to/MyApp.csproj",
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
        Assert.Equal("run --no-launch-profile --project /path/to/MyApp.csproj", app.Arguments);
        Assert.Equal("https://localhost:5001", app.PublicUrl);
        Assert.Equal("Development", app.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"]);
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
                        "workingDirectory": "PublishedApp",
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
        Assert.Equal("PublishedApp", app.WorkingDirectory);
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
        manifest.Apps["TestApp"] = new E2EAppEntry { Executable = "dotnet", Arguments = "run --project /test" };

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
    public void Deserialize_MultipleApps_AllPresent()
    {
        var json = """
            {
                "apps": {
                    "App1": { "executable": "dotnet", "arguments": "run --project /app1", "environmentVariables": {} },
                    "App2": { "executable": "dotnet", "arguments": "run --project /app2", "environmentVariables": {} },
                    "App3": { "executable": "dotnet", "arguments": "run --project /app3", "environmentVariables": {} }
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
}
