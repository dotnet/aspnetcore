// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Microsoft.AspNetCore.Components.AI.Mcp.Tests;

public class McpContentHostMetadataTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData("""{"visibility":["app"]}""", true)]
    [InlineData("""{"visibility":["model"]}""", false)]
    [InlineData("""{"visibility":[]}""", false)]
    public async Task AppVisibilityUsesStandardDefaultAndAudience(string? metadata, bool visible)
    {
        var tool = McpClientFixture.Tool("action", metadata ?? "{}");
        var server = new McpClientFixture("server")
        {
            ToolDefinitions = [McpClientFixture.Tool("open", """{"resourceUri":"ui://test/app"}"""), tool]
        };
        await using var client = await McpClient.CreateAsync(server);
        await using var host = new McpHostFixture(client, (await client.ListToolsAsync()).ToArray());

        var response = Assert.IsType<JsonRpcResponse>(await host.Host.DispatchAsync(new()
        {
            Id = new("list"), Method = RequestMethods.ToolsList, Params = new JsonObject()
        }, default));

        Assert.Equal(visible, response.Result!["tools"]!.AsArray().Any(item => item!["name"]!.GetValue<string>() == "action"));
        Assert.Equal(0, server.ToolCalls);
    }

    [Theory]
    [InlineData("""{"visibility":"app"}""")]
    [InlineData("""{"visibility":["unknown"]}""")]
    public async Task MalformedVisibilityReturnsExplicitProtocolError(string metadata)
    {
        var server = new McpClientFixture("server")
        {
            ToolDefinitions = [McpClientFixture.Tool("open", metadata)]
        };
        await using var client = await McpClient.CreateAsync(server);
        await using var host = new McpHostFixture(client, (await client.ListToolsAsync()).ToArray());

        var response = Assert.IsType<JsonRpcError>(await host.Host.DispatchAsync(new()
        {
            Id = new("list"), Method = RequestMethods.ToolsList, Params = new JsonObject()
        }, default));

        Assert.Equal(-32000, response.Error.Code);
        Assert.Equal("Tool UI visibility must contain only model or app.", response.Error.Message);
    }

    [Theory]
    [InlineData("/relative")]
    [InlineData("ui:opaque")]
    [InlineData("https://not-ui.example")]
    public async Task MalformedResourceUriIsNotSilentlyAccepted(string uri)
    {
        var tool = McpClientFixture.Tool("open", "{}");
        tool.Meta!["ui"] = new JsonObject { ["resourceUri"] = uri };
        var server = new McpClientFixture("server") { ToolDefinitions = [tool] };
        await using var client = await McpClient.CreateAsync(server);
        await using var host = new McpHostFixture(client, (await client.ListToolsAsync()).ToArray());

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => host.InitializeAsync());

        Assert.Equal("App tools must reference an absolute ui:// resource.", error.Message);
        Assert.Equal(0, server.ResourceReads);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ResourceUriSupportsLegacyMetadataAndPrefersNestedValue(bool nested)
    {
        var tool = McpClientFixture.Tool("open", nested ? """{"resourceUri":"ui://test/app"}""" : "{}");
        tool.Meta!["ui/resourceUri"] = nested ? "ui://legacy/app" : "ui://test/app";
        var server = new McpClientFixture("server") { ToolDefinitions = [tool] };
        await using var client = await McpClient.CreateAsync(server);
        await using var host = new McpHostFixture(client, (await client.ListToolsAsync()).ToArray());

        await host.InitializeAsync();

        Assert.Equal("<html><body>App</body></html>", host.Browser.Document.GetProperty("html").GetString());
        Assert.Equal(1, server.ResourceReads);
    }

    [Fact]
    public async Task DocumentSelectionSupportsAdditionalNonDocumentContents()
    {
        var server = Server("<p>selected</p>");
        server.ResourceResult!.Contents.Add(new TextResourceContents
        {
            Uri = "data://other/item", MimeType = "text/plain", Text = "other"
        });
        await using var client = await McpClient.CreateAsync(server);
        await using var host = new McpHostFixture(client, (await client.ListToolsAsync()).ToArray());

        await host.InitializeAsync();

        Assert.Equal("<p>selected</p>", host.Browser.Document.GetProperty("html").GetString());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, host.Browser.Document.GetProperty("csp").ValueKind);
        Assert.Equal(System.Text.Json.JsonValueKind.Null, host.Browser.Document.GetProperty("permissions").ValueKind);
    }

    [Fact]
    public async Task AmbiguousHtmlDocumentsFailExplicitly()
    {
        var server = Server("one");
        server.ResourceResult!.Contents.Add(new TextResourceContents
        {
            Uri = "ui://test/app", MimeType = McpClientFixture.HtmlMimeType, Text = "two"
        });
        await using var client = await McpClient.CreateAsync(server);
        await using var host = new McpHostFixture(client, (await client.ListToolsAsync()).ToArray());

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => host.InitializeAsync());

        Assert.Equal("The app resource must contain exactly one matching HTML document.", error.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LargeTextAndBlobDocumentsAreDeliveredWithoutSizeLimits(bool blob)
    {
        var html = "\u00e9" + new string('x', 2 * 1024 * 1024 + 1);
        var server = Server(html);
        if (blob)
        {
            server.ResourceResult!.Contents[0] = BlobResourceContents.FromBytes(
                Encoding.UTF8.GetBytes(html), "ui://test/app", McpClientFixture.HtmlMimeType);
        }
        await using var client = await McpClient.CreateAsync(server);
        await using var host = new McpHostFixture(client, (await client.ListToolsAsync()).ToArray());

        await host.InitializeAsync();

        Assert.Equal(html, host.Browser.Document.GetProperty("html").GetString());
    }

    [Fact]
    public async Task BlobUsesStrictUtf8()
    {
        var server = Server("app");
        server.ResourceResult!.Contents[0] = BlobResourceContents.FromBytes(
            new byte[] { 0xff }, "ui://test/app", McpClientFixture.HtmlMimeType);
        await using var client = await McpClient.CreateAsync(server);
        await using var host = new McpHostFixture(client, (await client.ListToolsAsync()).ToArray());

        await Assert.ThrowsAsync<DecoderFallbackException>(() => host.InitializeAsync());
    }

    [Fact]
    public async Task ContentMetadataOverridesListingAndRetainsOmittedDeclarations()
    {
        var server = Server("<p>app</p>", """{"csp":{"resourceDomains":["https://assets.example"],"connectDomains":["https://api.example"]}}""");
        server.Resources[0].Meta = new()
        {
            ["ui"] = JsonNode.Parse("""{"csp":{"resourceDomains":["https://old.example"]},"permissions":{"clipboardWrite":{}}}""")
        };
        await using var client = await McpClient.CreateAsync(server);
        await using var host = new McpHostFixture(client, (await client.ListToolsAsync()).ToArray());

        await host.InitializeAsync();

        var document = host.Browser.Document;
        Assert.Equal("https://assets.example", document.GetProperty("csp").GetProperty("resourceDomains")[0].GetString());
        Assert.Equal("https://api.example", document.GetProperty("csp").GetProperty("connectDomains")[0].GetString());
        Assert.Equal("{}", document.GetProperty("permissions").GetProperty("clipboardWrite").GetRawText());
        Assert.Equal("https://old.example", server.Resources[0].Meta!["ui"]!["csp"]!["resourceDomains"]![0]!.GetValue<string>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ResourceMetadataFallsBackToListingAndSupportsUnlistedReads(bool listed)
    {
        var server = Server("app");
        if (listed)
        {
            server.Resources[0].Meta = new()
            {
                ["ui"] = JsonNode.Parse("""{"csp":{"resourceDomains":["https://assets.example"]}}""")
            };
        }
        else
        {
            server.Resources.Clear();
        }
        await using var client = await McpClient.CreateAsync(server);
        await using var host = new McpHostFixture(client, (await client.ListToolsAsync()).ToArray());

        await host.InitializeAsync();

        if (listed)
        {
            Assert.Equal("https://assets.example", host.Browser.Document.GetProperty("csp").GetProperty("resourceDomains")[0].GetString());
        }
        else
        {
            Assert.Equal(System.Text.Json.JsonValueKind.Null, host.Browser.Document.GetProperty("csp").ValueKind);
        }
        Assert.Equal(1, server.ResourceReads);
    }

    [Theory]
    [InlineData("""{"csp":[]}""")]
    [InlineData("""{"csp":null}""")]
    [InlineData("""{"permissions":"camera"}""")]
    [InlineData("""{"permissions":null}""")]
    public async Task InvalidContentDeclarationDoesNotFallBackToListing(string declaration)
    {
        var server = Server("app", declaration);
        server.Resources[0].Meta = new()
        {
            ["ui"] = JsonNode.Parse("""{"csp":{},"permissions":{}}""")
        };
        await using var client = await McpClient.CreateAsync(server);
        await using var host = new McpHostFixture(client, (await client.ListToolsAsync()).ToArray());

        await Assert.ThrowsAsync<InvalidOperationException>(() => host.InitializeAsync());
    }

    [Theory]
    [InlineData("http://network.example")]
    [InlineData("https://network.example")]
    [InlineData("ws://network.example")]
    [InlineData("wss://network.example")]
    public async Task NetworkDeclarationsCrossInteropAsJson(string origin)
    {
        var server = Server("app");
        server.ResourceResult!.Contents[0].Meta = new()
        {
            ["ui"] = new JsonObject { ["csp"] = new JsonObject { ["connectDomains"] = new JsonArray(origin) } }
        };
        await using var client = await McpClient.CreateAsync(server);
        await using var host = new McpHostFixture(client, (await client.ListToolsAsync()).ToArray());

        await host.InitializeAsync();

        Assert.Equal(origin, host.Browser.Document.GetProperty("csp").GetProperty("connectDomains")[0].GetString());
    }

    [Fact]
    public async Task EmptyDeclarationsRemainDistinctFromOmittedMetadata()
    {
        var server = Server("app", """{"csp":{},"permissions":{}}""");
        await using var client = await McpClient.CreateAsync(server);
        await using var host = new McpHostFixture(client, (await client.ListToolsAsync()).ToArray());

        await host.InitializeAsync();

        Assert.Equal("{}", host.Browser.Document.GetProperty("csp").GetRawText());
        Assert.Equal("{}", host.Browser.Document.GetProperty("permissions").GetRawText());
    }

    private static McpClientFixture Server(string html, string? ui = null) => new("server")
    {
        ResourceResult = new()
        {
            Contents =
            [
                new TextResourceContents
                {
                    Uri = "ui://test/app", MimeType = McpClientFixture.HtmlMimeType, Text = html,
                    Meta = ui is null ? null : new() { ["ui"] = JsonNode.Parse(ui) }
                }
            ]
        }
    };
}
