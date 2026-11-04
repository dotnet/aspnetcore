// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Microsoft.AspNetCore.Components.AI.Mcp.Tests;

public class McpAppClientOptionsExtensionsTests
{
    [Fact]
    public void EnablesHtmlAppsBeforeConnecting()
    {
        var options = new McpClientOptions();

        options.EnableMcpApps();

        var extension = Assert.IsType<JsonObject>(options.Capabilities!.Extensions!["io.modelcontextprotocol/ui"]);
        Assert.Equal("text/html;profile=mcp-app", extension["mimeTypes"]![0]!.GetValue<string>());
    }

    [Fact]
    public void PreservesOtherCapabilitiesAndDoesNotAdvertiseUnsupportedUiTypes()
    {
        var otherExtension = new JsonObject { ["configured"] = true };
        var capabilities = new ClientCapabilities
        {
            Extensions = new Dictionary<string, object>
            {
                ["example/other"] = otherExtension,
                ["io.modelcontextprotocol/ui"] = new JsonObject { ["mimeTypes"] = new JsonArray("unsupported/example") },
            },
        };
        var options = new McpClientOptions { Capabilities = capabilities };

        options.EnableMcpApps();
        options.EnableMcpApps();

        Assert.Same(capabilities, options.Capabilities);
        Assert.Same(otherExtension, capabilities.Extensions["example/other"]);
        var extension = Assert.IsType<JsonObject>(capabilities.Extensions["io.modelcontextprotocol/ui"]);
        Assert.Single(extension["mimeTypes"]!.AsArray());
        Assert.Equal("text/html;profile=mcp-app", extension["mimeTypes"]![0]!.GetValue<string>());
        Assert.Equal(2, capabilities.Extensions.Count);
    }

    [Fact]
    public void RejectsNullOptions()
    {
        Assert.Throws<ArgumentNullException>("options", () => McpAppClientOptionsExtensions.EnableMcpApps(null!));
    }
}
