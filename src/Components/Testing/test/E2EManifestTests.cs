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
        // Arrange
        var json = """
            {
                "apps": {
                    "MyApp": {
                        "projectPath": "/path/to/MyApp.csproj",
                        "publicUrl": "https://localhost:5001",
                        "environmentVariables": {
                            "ASPNETCORE_ENVIRONMENT": "Development"
                        }
                    }
                }
            }
            """;

        // Act
        var manifest = JsonSerializer.Deserialize<E2EManifest>(json);

        // Assert
        Assert.NotNull(manifest);
        Assert.Single(manifest!.Apps);
        Assert.True(manifest.Apps.ContainsKey("MyApp"));

        var app = manifest.Apps["MyApp"];
        Assert.Equal("/path/to/MyApp.csproj", app.ProjectPath);
        Assert.Equal("https://localhost:5001", app.PublicUrl);
        Assert.Equal("Development", app.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"]);
    }

    [Fact]
    public void Deserialize_WithPublishedApp_ReturnsPublishedDetails()
    {
        // Arrange
        var json = """
            {
                "apps": {
                    "PublishedApp": {
                        "published": {
                            "executable": "PublishedApp.exe",
                            "args": "",
                            "workingDirectory": "PublishedApp"
                        },
                        "environmentVariables": {}
                    }
                }
            }
            """;

        // Act
        var manifest = JsonSerializer.Deserialize<E2EManifest>(json);

        // Assert
        Assert.NotNull(manifest);
        var app = manifest!.Apps["PublishedApp"];
        Assert.NotNull(app.Published);
        Assert.Equal("PublishedApp.exe", app.Published!.Executable);
        Assert.Equal("PublishedApp", app.Published.WorkingDirectory);
    }

    [Fact]
    public void Deserialize_EmptyApps_ReturnsEmptyDictionary()
    {
        // Arrange
        var json = """{ "apps": {} }""";

        // Act
        var manifest = JsonSerializer.Deserialize<E2EManifest>(json);

        // Assert
        Assert.NotNull(manifest);
        Assert.Empty(manifest!.Apps);
    }

    [Fact]
    public void GetApp_ExistingKey_ReturnsEntry()
    {
        // Arrange
        var manifest = new E2EManifest();
        manifest.Apps["TestApp"] = new E2EAppEntry { ProjectPath = "/test" };

        // Act
        var result = manifest.GetApp("TestApp");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("/test", result!.ProjectPath);
    }

    [Fact]
    public void GetApp_MissingKey_ReturnsNull()
    {
        // Arrange
        var manifest = new E2EManifest();

        // Act
        var result = manifest.GetApp("NonExistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Load_MissingManifest_ThrowsFileNotFoundException()
    {
        // Arrange
        var assemblyName = "NonExistentAssembly_" + Guid.NewGuid().ToString("N");

        // Act & Assert
        var ex = Assert.Throws<FileNotFoundException>(() => E2EManifest.Load(assemblyName));
        Assert.Contains("E2E manifest not found", ex.Message);
        Assert.Contains("Microsoft.AspNetCore.Components.Testing.targets", ex.Message);
    }

    [Fact]
    public void Deserialize_MultipleApps_AllPresent()
    {
        // Arrange
        var json = """
            {
                "apps": {
                    "App1": { "projectPath": "/app1", "environmentVariables": {} },
                    "App2": { "projectPath": "/app2", "environmentVariables": {} },
                    "App3": { "projectPath": "/app3", "environmentVariables": {} }
                }
            }
            """;

        // Act
        var manifest = JsonSerializer.Deserialize<E2EManifest>(json);

        // Assert
        Assert.NotNull(manifest);
        Assert.Equal(3, manifest!.Apps.Count);
        Assert.NotNull(manifest.GetApp("App1"));
        Assert.NotNull(manifest.GetApp("App2"));
        Assert.NotNull(manifest.GetApp("App3"));
    }
}
