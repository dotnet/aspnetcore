// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.Playwright;

namespace Microsoft.AspNetCore.Components.Testing.Tests;

public class PlaywrightExtensionsTests
{
    [Fact]
    public void WithServerRouting_SetsXTestBackendHeader()
    {
        // Arrange
        var options = new BrowserNewContextOptions();
        var server = CreateMockServerInstance();

        // Act
        var result = options.WithServerRouting(server);

        // Assert
        Assert.Same(options, result);
        Assert.NotNull(options.ExtraHTTPHeaders);
        var headers = options.ExtraHTTPHeaders!.ToDictionary(x => x.Key, x => x.Value);
        Assert.Equal(server.Id, headers["X-Test-Backend"]);
    }

    [Fact]
    public void WithServerRouting_PreservesExistingHeaders()
    {
        var options = new BrowserNewContextOptions
        {
            ExtraHTTPHeaders = new Dictionary<string, string>
            {
                ["Authorization"] = "Bearer token123"
            }
        };
        var server = CreateMockServerInstance();

        options.WithServerRouting(server);

        var headers = options.ExtraHTTPHeaders!.ToDictionary(x => x.Key, x => x.Value);
        Assert.Equal("Bearer token123", headers["Authorization"]);
        Assert.Equal(server.Id, headers["X-Test-Backend"]);
    }

    [Fact]
    public void WithServerRouting_ReturnsChainableInstance()
    {
        // Arrange
        var options = new BrowserNewContextOptions();
        var server = CreateMockServerInstance();

        // Act
        var result = options.WithServerRouting(server);

        // Assert — fluent API returns the same instance
        Assert.Same(options, result);
    }

    [Fact]
    public void WithArtifacts_WithoutEnvVar_DoesNotSetRecordVideoDir()
    {
        // Arrange
        var options = new BrowserNewContextOptions();

        // Act — PLAYWRIGHT_RECORD_VIDEO is not set in test env
        var result = options.WithArtifacts("/some/dir");

        // Assert
        Assert.Same(options, result);
        Assert.Null(options.RecordVideoDir);
    }

    [Fact]
    public void WithArtifacts_NullDir_DoesNotSetRecordVideoDir()
    {
        // Arrange
        var options = new BrowserNewContextOptions();

        // Act
        var result = options.WithArtifacts(null);

        // Assert
        Assert.Same(options, result);
        Assert.Null(options.RecordVideoDir);
    }

    static ServerInstance CreateMockServerInstance()
    {
        // InternalsVisibleTo grants access to the internal constructor.
        // Id is auto-generated (Guid); we use the real Id in assertions.
        return new ServerInstance("TestApp", "key", "http://localhost:5000", null);
    }
}
