// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Protocol;

namespace Microsoft.AspNetCore.Components.AI.Mcp.Tests;

public class McpAppPolicyTests
{
    [Theory]
    [InlineData(null, true, true)]
    [InlineData("""{"visibility":["app"]}""", false, true)]
    [InlineData("""{"visibility":["model"]}""", true, false)]
    [InlineData("""{"visibility":[]}""", false, false)]
    public void VisibilityUsesStandardDefaultAndAudience(string? metadata, bool model, bool app)
    {
        var tool = CreateTool(metadata);

        Assert.Equal(model, McpAppPolicy.VisibleTo(tool, "model"));
        Assert.Equal(app, McpAppPolicy.VisibleTo(tool, "app"));
    }

    [Theory]
    [InlineData("""{"visibility":"app"}""")]
    [InlineData("""{"visibility":["unknown"]}""")]
    [InlineData("""{"resourceUri":"/relative"}""")]
    [InlineData("""{"resourceUri":"ui:opaque"}""")]
    [InlineData("""{"resourceUri":"https://not-ui.example"}""")]
    public void MalformedToolMetadataIsNotSilentlyAccepted(string metadata)
    {
        var tool = CreateTool(metadata);

        Assert.Throws<InvalidOperationException>(() =>
        {
            McpAppPolicy.VisibleTo(tool, "model");
            McpAppPolicy.ResourceUri(tool);
        });
    }

    [Fact]
    public void NestedResourceUriTakesPrecedenceOverLegacyMetadata()
    {
        var tool = CreateTool("""{"resourceUri":"ui://new/app"}""");
        tool.Meta!["ui/resourceUri"] = "ui://legacy/app";

        Assert.Equal("ui://new/app", McpAppPolicy.ResourceUri(tool));
    }

    [Fact]
    public void DocumentSelectionSupportsAdditionalNonDocumentContents()
    {
        var result = ReadResult(new TextResourceContents
        {
            Uri = "ui://test/app", MimeType = McpAppPolicy.HtmlMimeType, Text = "<p>selected</p>"
        });
        result.Contents.Add(new TextResourceContents { Uri = "data://other/item", MimeType = "text/plain", Text = "other" });

        var document = McpAppPolicy.ReadDocument(result, "ui://test/app", null, Options());

        Assert.Equal("<p>selected</p>", document.Html);
        Assert.Empty(document.Policy.ResourceDomains);
        Assert.Empty(document.Policy.Permissions);
    }

    [Fact]
    public void AmbiguousHtmlDocumentsFailExplicitly()
    {
        var document = new TextResourceContents { Uri = "ui://test/app", MimeType = McpAppPolicy.HtmlMimeType, Text = "one" };
        var result = ReadResult(document);
        result.Contents.Add(new TextResourceContents { Uri = "ui://test/app", MimeType = McpAppPolicy.HtmlMimeType, Text = "two" });

        Assert.Throws<InvalidOperationException>(() => McpAppPolicy.ReadDocument(result, "ui://test/app", null, Options()));
    }

    [Fact]
    public void BlobUsesStrictUtf8AndDocumentBudget()
    {
        var content = BlobResourceContents.FromBytes(Encoding.UTF8.GetBytes("é"), "ui://test/app", McpAppPolicy.HtmlMimeType);
        var options = Options();
        options.MaximumDocumentBytes = 2;

        Assert.Equal("é", McpAppPolicy.ReadDocument(ReadResult(content), content.Uri, null, options).Html);
        options.MaximumDocumentBytes = 1;
        Assert.Throws<InvalidOperationException>(() => McpAppPolicy.ReadDocument(ReadResult(content), content.Uri, null, options));
        content = BlobResourceContents.FromBytes(new byte[] { 0xff }, content.Uri, content.MimeType);
        options.MaximumDocumentBytes = 2;
        Assert.Throws<DecoderFallbackException>(() => McpAppPolicy.ReadDocument(ReadResult(content), content.Uri, null, options));
    }

    [Fact]
    public void ContentMetadataOverridesListingAndExplicitGrantsAreApplied()
    {
        var options = Options();
        options.BrowserPolicy.ResourceDomains.Add("https://assets.example");
        options.BrowserPolicy.ConnectDomains.Add("https://api.example");
        options.BrowserPolicy.Permissions.Add("clipboard-write");
        var content = new TextResourceContents
        {
            Uri = "ui://test/app", MimeType = McpAppPolicy.HtmlMimeType, Text = "<p>app</p>",
            Meta = new()
            {
                ["ui"] = JsonNode.Parse("""{"csp":{"resourceDomains":["https://assets.example"],"connectDomains":["https://api.example"]},"permissions":{"clipboardWrite":{}}}""")
            }
        };
        var listing = new Resource
        {
            Name = "App", Uri = content.Uri,
            Meta = new() { ["ui"] = JsonNode.Parse("""{"csp":{"resourceDomains":["https://denied.example"]}}""") }
        };

        var document = McpAppPolicy.ReadDocument(ReadResult(content), content.Uri, listing, options);

        Assert.Equal("https://assets.example", Assert.Single(document.Policy.ResourceDomains));
        Assert.Equal("https://api.example", Assert.Single(document.Policy.ConnectDomains));
        Assert.Equal("clipboard-write", Assert.Single(document.Policy.Permissions));
    }

    [Fact]
    public void MissingContentMetadataUsesListingFallbackEvenWhenReadWasNotListed()
    {
        var options = Options();
        options.BrowserPolicy.ResourceDomains.Add("https://assets.example");
        var content = new TextResourceContents { Uri = "ui://test/app", MimeType = McpAppPolicy.HtmlMimeType, Text = "app" };
        var listing = new Resource
        {
            Name = "App", Uri = content.Uri,
            Meta = new() { ["ui"] = JsonNode.Parse("""{"csp":{"resourceDomains":["https://assets.example"]}}""") }
        };

        Assert.Equal("https://assets.example", Assert.Single(McpAppPolicy.ReadDocument(ReadResult(content), content.Uri, listing, options).Policy.ResourceDomains));
        Assert.Empty(McpAppPolicy.ReadDocument(ReadResult(content), content.Uri, null, options).Policy.ResourceDomains);
    }

    [Theory]
    [InlineData("""{"csp":{"resourceDomains":["https://denied.example"]}}""")]
    [InlineData("""{"csp":{"resourceDomains":["*"]}}""")]
    [InlineData("""{"csp":{"resourceDomains":["https://assets.example/path"]}}""")]
    [InlineData("""{"permissions":{"camera":{}}}""")]
    public void UngrantedBrowserRequirementsFailInsteadOfSilentlyExpandingPolicy(string declaration)
    {
        var content = new TextResourceContents
        {
            Uri = "ui://test/app", MimeType = McpAppPolicy.HtmlMimeType, Text = "app",
            Meta = new() { ["ui"] = JsonNode.Parse(declaration) }
        };

        Assert.Throws<InvalidOperationException>(() => McpAppPolicy.ReadDocument(ReadResult(content), content.Uri, null, Options()));
    }

    [Fact]
    public void ModelProjectionPreservesBusinessContentWithoutForwardingPrivateMetadata()
    {
        var result = new CallToolResult
        {
            IsError = true,
            Content =
            [
                new TextContentBlock { Text = "business failure", Meta = new() { ["private"] = "content-only metadata" } },
                ImageContentBlock.FromBytes(new byte[] { 1, 2 }, "image/png")
            ],
            Meta = new() { ["ui"] = new JsonObject { ["private"] = true }, ["business"] = "preserved" },
            StructuredContent = JsonDocument.Parse("""{"businessField":42}""").RootElement.Clone()
        };

        var projection = Assert.IsType<JsonElement>(McpAppPolicy.ProjectForModel(result));

        Assert.True(projection.GetProperty("isError").GetBoolean());
        Assert.Equal(2, projection.GetProperty("content").GetArrayLength());
        Assert.Equal("AQI=", projection.GetProperty("content")[1].GetProperty("data").GetString());
        Assert.False(projection.TryGetProperty("_meta", out _));
        Assert.False(projection.GetProperty("content")[0].TryGetProperty("_meta", out _));
        Assert.Equal(42, projection.GetProperty("structuredContent").GetProperty("businessField").GetInt32());
        Assert.True(result.Meta["ui"]!["private"]!.GetValue<bool>());
        Assert.Equal("preserved", result.Meta["business"]!.GetValue<string>());
        Assert.Equal("content-only metadata", result.Content[0].Meta!["private"]!.GetValue<string>());
    }

    [Theory]
    [InlineData("http://network.example")]
    [InlineData("https://network.example")]
    [InlineData("ws://network.example")]
    [InlineData("wss://network.example")]
    public void ExplicitNetworkGrantsSupportHttpAndWebSocketOrigins(string origin)
    {
        var options = Options();
        options.BrowserPolicy.ConnectDomains.Add(origin);
        var content = new TextResourceContents
        {
            Uri = "ui://test/app",
            MimeType = McpAppPolicy.HtmlMimeType,
            Text = "<p>Network app</p>",
            Meta = new()
            {
                ["ui"] = new JsonObject { ["csp"] = new JsonObject { ["connectDomains"] = new JsonArray(origin) } },
            },
        };

        var document = McpAppPolicy.ReadDocument(ReadResult(content), content.Uri, null, options);

        Assert.Equal(new[] { origin }, document.Policy.ConnectDomains);
    }

    [Theory]
    [InlineData("ws://network.example")]
    [InlineData("wss://network.example")]
    public void WebSocketOriginsCannotGrantScriptResources(string origin)
    {
        var options = Options();
        options.BrowserPolicy.ResourceDomains.Add(origin);
        var content = new TextResourceContents
        {
            Uri = "ui://test/app",
            MimeType = McpAppPolicy.HtmlMimeType,
            Text = "<p>Network app</p>",
            Meta = new()
            {
                ["ui"] = new JsonObject { ["csp"] = new JsonObject { ["resourceDomains"] = new JsonArray(origin) } },
            },
        };

        Assert.Throws<InvalidOperationException>(() => McpAppPolicy.ReadDocument(ReadResult(content), content.Uri, null, options));
    }

    [Fact]
    public void EmbeddedResourceMetadataIsRemovedWithoutChangingStructuredBusinessFields()
    {
        var result = JsonSerializer.Deserialize<CallToolResult>("""
            {
                "isError": true,
                "content": [{
                    "type": "resource",
                    "resource": {
                        "uri": "note://details",
                        "mimeType": "text/plain",
                        "text": "Business details",
                        "_meta": { "viewOnly": "Private resource details" }
                    }
                }],
                "structuredContent": { "_meta": { "business": "Preserved" } }
            }
            """, ModelContextProtocol.McpJsonUtilities.DefaultOptions)!;

        var projection = Assert.IsType<JsonElement>(McpAppPolicy.ProjectForModel(result));

        Assert.False(projection.GetProperty("content")[0].GetProperty("resource").TryGetProperty("_meta", out _));
        Assert.Equal("Business details", projection.GetProperty("content")[0].GetProperty("resource").GetProperty("text").GetString());
        Assert.Equal("Preserved", projection.GetProperty("structuredContent").GetProperty("_meta").GetProperty("business").GetString());
        Assert.Equal("Private resource details",
            McpAppPolicy.Wire(result).GetProperty("content")[0].GetProperty("resource").GetProperty("_meta").GetProperty("viewOnly").GetString());
    }

    [Fact]
    public void SimpleMultimodalProjectionUsesSdkContentConversions()
    {
        var result = new CallToolResult
        {
            Content = [new TextContentBlock { Text = "text" }, ImageContentBlock.FromBytes(new byte[] { 1, 2 }, "image/png")],
            Meta = new() { ["ui"] = new JsonObject { ["private"] = true } }
        };

        var projection = Assert.IsType<AIContent[]>(McpAppPolicy.ProjectForModel(result));

        Assert.Equal("text", Assert.IsType<TextContent>(projection[0]).Text);
        Assert.Equal(new byte[] { 1, 2 }, Assert.IsType<DataContent>(projection[1]).Data.ToArray());
        Assert.True(result.Meta["ui"]!["private"]!.GetValue<bool>());
    }

    private static Tool CreateTool(string? ui) => new()
    {
        Name = "test",
        InputSchema = JsonDocument.Parse("""{"type":"object"}""").RootElement.Clone(),
        Meta = ui is null ? null : new() { ["ui"] = JsonNode.Parse(ui) }
    };

    private static ReadResourceResult ReadResult(ResourceContents content) => new() { Contents = [content] };
    private static McpContentOptions Options() => new() { ProxyUrl = new("https://proxy.example/proxy") };
}
