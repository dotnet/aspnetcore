// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace Microsoft.AspNetCore.Components.Mcp;

public class McpComponentRegistrationTests
{
    [Fact]
    public void ExistingToolAssociation_PreservesTheRegisteredPrimitiveAndSchema()
    {
        var services = new ServiceCollection();
        var tool = McpServerTool.Create((string value) => value, new() { Name = "open" });
        var other = McpServerTool.Create(() => "other", new() { Name = "other" });
        var metadata = new JsonObject { ["business"] = "preserved" };
        tool.ProtocolTool.Meta = metadata;
        other.ProtocolTool.Meta = metadata;
        var schema = tool.ProtocolTool.InputSchema.GetRawText();
        services.AddMcpServer().WithTools([tool, other])
            .WithRazorComponents<TestComponent>("open", _ => new Dictionary<string, object?>(),
                options => options.BaseUri = new Uri("https://example.test/"));
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<McpServerOptions>>().Value;

        Assert.Same(tool, Assert.Single(options.ToolCollection!, candidate => candidate.ProtocolTool.Name == "open"));
        Assert.Equal(schema, tool.ProtocolTool.InputSchema.GetRawText());
        Assert.Equal("preserved", tool.ProtocolTool.Meta!["business"]!.GetValue<string>());
        Assert.NotNull(tool.ProtocolTool.Meta["ui"]!["resourceUri"]);
        Assert.Null(other.ProtocolTool.Meta!["ui"]);
    }

    [Fact]
    public void MissingOpeningTool_FailsExplicitly()
    {
        var services = new ServiceCollection();
        services.AddMcpServer().WithRazorComponents<TestComponent>("missing",
            _ => new Dictionary<string, object?>(), options => options.BaseUri = new Uri("https://example.test/"));
        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IOptions<McpServerOptions>>().Value);

        Assert.Contains("missing", exception.Message);
        Assert.Contains("exactly once", exception.Message);
    }

    [Fact]
    public void DifferentToolsWithTheSameOpeningName_AreNotSilentlyDeduplicated()
    {
        var services = new ServiceCollection();
        services.AddMcpServer().WithTools(
            [McpServerTool.Create(() => "first", new() { Name = "open" }), McpServerTool.Create(() => "second", new() { Name = "open" })])
            .WithRazorComponents<TestComponent>("open", _ => new Dictionary<string, object?>(),
                options => options.BaseUri = new Uri("https://example.test/"));
        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IOptions<McpServerOptions>>().Value);

        Assert.Contains("Multiple MCP tools", exception.Message);
    }

    [Fact]
    public void TheSameSdkToolInstanceCanAlreadyBeRegisteredMoreThanOnce()
    {
        var services = new ServiceCollection();
        var tool = McpServerTool.Create(() => "value", new() { Name = "open" });
        services.AddMcpServer().WithTools([tool, tool])
            .WithRazorComponents<TestComponent>("open", _ => new Dictionary<string, object?>(),
                options => options.BaseUri = new Uri("https://example.test/"));
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<McpServerOptions>>().Value;

        Assert.Single(options.ToolCollection!, candidate => candidate.ProtocolTool.Name == "open");
    }

    private sealed class TestComponent : ComponentBase;
}
