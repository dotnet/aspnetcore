// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.AI.Mcp.Tests;

public class McpSandboxBaseTests
{
    [Fact]
    public async Task ConcreteBaseRendersStandaloneDocumentAndDefaultFrame()
    {
        await using var services = new ServiceCollection().AddLogging()
            .AddSingleton<NavigationManager>(new TestNavigationManager()).BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());

        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var root = await renderer.RenderComponentAsync<McpSandboxBase>(ParameterView.FromDictionary(
                new Dictionary<string, object?> { [nameof(McpSandboxBase.HostOrigin)] = "https://host.example" }));
            return root.ToHtmlString();
        });

        Assert.False(typeof(ComponentBase).IsAssignableFrom(typeof(McpSandboxBase)));
        Assert.StartsWith("<!doctype html><html>", html);
        Assert.EndsWith("</body></html>", html);
        Assert.Single(Regex.Matches(html, "<iframe"));
        Assert.Contains("id=\"mcp-app\"", html);
        Assert.Contains("sandbox=\"allow-scripts\"", html);
        Assert.Contains("referrerpolicy=\"no-referrer\"", html);
        Assert.Single(Regex.Matches(html, "<script"));
        Assert.Contains("mcp-sandbox.js", html);
        Assert.Contains("\"https://host.example\"", html);
        Assert.DoesNotContain("blazor.web.js", html);
    }

    [Fact]
    public async Task BodyOverrideUsesItsOwnSequenceNumbersAndReceivesParameters()
    {
        await using var services = new ServiceCollection().AddLogging()
            .AddSingleton<NavigationManager>(new TestNavigationManager()).BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());

        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var root = await renderer.RenderComponentAsync<CustomSandbox>(ParameterView.FromDictionary(
                new Dictionary<string, object?>
                {
                    [nameof(McpSandboxBase.HostOrigin)] = "https://host.example:5001",
                    [nameof(CustomSandbox.FrameTitle)] = "Custom <app>"
                }));
            return root.ToHtmlString();
        });

        Assert.Single(Regex.Matches(html, "<html>"));
        Assert.Single(Regex.Matches(html, "<iframe"));
        Assert.Contains("<section><iframe id=\"mcp-app\" title=\"Custom &lt;app&gt;\"", html);
        Assert.Contains("\"https://host.example:5001\"", html);
        Assert.DoesNotContain("MCP app document", html);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("/relative")]
    [InlineData("file:///app")]
    [InlineData("https://user:pass@host.example")]
    [InlineData("https://host.example/document")]
    [InlineData("https://host.example/?query")]
    [InlineData("https://host.example/#fragment")]
    public async Task SuppliedOriginIsRenderedWithoutParsingOrValidation(string? origin)
    {
        await using var services = new ServiceCollection().AddLogging()
            .AddSingleton<NavigationManager>(new TestNavigationManager()).BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());

        var html = await renderer.Dispatcher.InvokeAsync(async () =>
            (await renderer.RenderComponentAsync<McpSandboxBase>(ParameterView.FromDictionary(
                new Dictionary<string, object?> { [nameof(McpSandboxBase.HostOrigin)] = origin }))).ToHtmlString());

        Assert.Contains($"await start(document.getElementById('mcp-app'),{System.Text.Json.JsonSerializer.Serialize(origin)})", html);
    }

    private sealed class CustomSandbox : McpSandboxBase
    {
        [Parameter] public string FrameTitle { get; set; } = "";

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "section");
            builder.OpenElement(1, "iframe");
            builder.AddMultipleAttributes(2, new Dictionary<string, object>
            {
                ["id"] = "mcp-app", ["title"] = FrameTitle
            });
            builder.CloseElement();
            builder.CloseElement();
        }

    }

    [Theory]
    [InlineData("https://sandbox.example/")]
    [InlineData("https://sandbox.example/chat/")]
    public async Task BootstrapResolvesModuleAgainstApplicationBaseUri(string baseUri)
    {
        await using var services = new ServiceCollection().AddLogging()
            .AddSingleton<NavigationManager>(new TestNavigationManager(baseUri)).BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());

        var html = await renderer.Dispatcher.InvokeAsync(async () =>
            (await renderer.RenderComponentAsync<McpSandboxBase>(ParameterView.FromDictionary(
                new Dictionary<string, object?> { [nameof(McpSandboxBase.HostOrigin)] = "https://host.example" }))).ToHtmlString());

        Assert.Contains(baseUri + "_content/Microsoft.AspNetCore.Components.AI.Mcp/mcp-sandbox.js", html);
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager(string baseUri = "https://sandbox.example/")
        {
            Initialize(baseUri, baseUri);
        }
    }
}
