// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class BrowserOptionsTest
{
    [Fact]
    public void SubOptions_ArePreInitialized()
    {
        var options = new BrowserOptions();

        Assert.NotNull(options.InteractiveServer);
        Assert.NotNull(options.StaticServer);
        Assert.NotNull(options.InteractiveWebAssembly);
    }

    [Fact]
    public void Serialization_PreservesTopLevelWireNames()
    {
        var options = new BrowserOptions
        {
            LogLevel = LogLevel.Warning,
        };
        options.InteractiveServer.ReconnectionMaxRetries = 5;
        options.StaticServer.PreserveDom = true;
        options.InteractiveWebAssembly.EnvironmentName = "Staging";

        using var document = SerializeToDocument(options);
        var root = document.RootElement;

        Assert.True(root.TryGetProperty("logLevel", out _));
        Assert.True(root.TryGetProperty("webAssembly", out _));
        Assert.True(root.TryGetProperty("server", out _));
        Assert.True(root.TryGetProperty("ssr", out _));
    }

    [Fact]
    public void Serialization_InteractiveServer_UsesMillisecondsForReconnectionRetryInterval()
    {
        var options = new BrowserOptions();
        options.InteractiveServer.ReconnectionRetryInterval = TimeSpan.FromSeconds(1.5);

        using var document = SerializeToDocument(options);
        var server = document.RootElement.GetProperty("server");

        Assert.Equal(1500, server.GetProperty("reconnectionRetryIntervalMilliseconds").GetInt32());
    }

    [Fact]
    public void Serialization_StaticServer_UsesNegatedDisableDomPreservation()
    {
        var options = new BrowserOptions();
        options.StaticServer.PreserveDom = true;
        options.StaticServer.CircuitInactivityTimeout = TimeSpan.FromSeconds(2);

        using var document = SerializeToDocument(options);
        var ssr = document.RootElement.GetProperty("ssr");

        Assert.False(ssr.GetProperty("disableDomPreservation").GetBoolean());
        Assert.Equal(2000, ssr.GetProperty("circuitInactivityTimeoutMs").GetInt32());
    }

    [Fact]
    public void Serialization_InteractiveServer_Extensions_AreWrittenAsFlatKeys()
    {
        var options = new BrowserOptions();
        options.InteractiveServer.Extensions["autoPauseEnabled"] = JsonSerializer.SerializeToElement(true);

        using var document = SerializeToDocument(options);
        var server = document.RootElement.GetProperty("server");

        Assert.True(server.GetProperty("autoPauseEnabled").GetBoolean());
    }

    [Fact]
    public void GetBrowserOptions_ReturnsSameInstanceOnSubsequentCalls()
    {
        var context = new DefaultHttpContext();

        var first = BrowserOptions.GetBrowserOptions(context);
        var second = BrowserOptions.GetBrowserOptions(context);

        Assert.Same(first, second);
    }

    [Fact]
    public void GetBrowserOptions_SeedsFromEndpointMetadata()
    {
        var metadataOptions = new BrowserOptions();
        var context = new DefaultHttpContext();
        context.SetEndpoint(new Endpoint(
            requestDelegate: null,
            metadata: new EndpointMetadataCollection(metadataOptions),
            displayName: "Test"));

        var result = BrowserOptions.GetBrowserOptions(context);

        Assert.Same(metadataOptions, result);
    }

    [Fact]
    public void GetBrowserOptions_ThrowsForNullContext()
    {
        Assert.Throws<ArgumentNullException>(() => BrowserOptions.GetBrowserOptions(null));
    }

    [Fact]
    public void MergeInto_OnlyOverridesSetValues()
    {
        var target = new BrowserOptions
        {
            LogLevel = LogLevel.Warning,
        };
        target.InteractiveServer.ReconnectionMaxRetries = 3;
        target.InteractiveWebAssembly.EnvironmentName = "Production";

        var source = new BrowserOptions();
        source.InteractiveServer.ReconnectionMaxRetries = 10;

        ConfigureBrowser.MergeInto(target, source);

        Assert.Equal(LogLevel.Warning, target.LogLevel);
        Assert.Equal(10, target.InteractiveServer.ReconnectionMaxRetries);
        Assert.Equal("Production", target.InteractiveWebAssembly.EnvironmentName);
    }

    private static JsonDocument SerializeToDocument(BrowserOptions options)
    {
        var json = JsonSerializer.Serialize(options, BrowserOptionsJsonContext.Default.BrowserOptions);
        return JsonDocument.Parse(json);
    }
}
