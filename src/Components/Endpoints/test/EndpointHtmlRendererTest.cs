// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.Endpoints.Tests.TestComponents;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Moq;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class EndpointHtmlRendererTest
{
    private const string MarkerPrefix = "<!--Blazor:";
    private const string PrerenderedComponentPattern = "^<!--Blazor:(?<preamble>.*?)-->(?<content>.+?)<!--Blazor:(?<epilogue>.*?)-->$";
    private const string ComponentPattern = "^<!--Blazor:(.*?)-->$";

    private static readonly IDataProtectionProvider _dataprotectorProvider = new EphemeralDataProtectionProvider();

    private readonly IServiceProvider _services = CreateDefaultServiceCollection().BuildServiceProvider();
    private readonly TestEndpointHtmlRenderer renderer;

    public EndpointHtmlRendererTest()
    {
        renderer = GetEndpointHtmlRenderer();
    }

    [Fact]
    public async Task CanRender_ParameterlessComponent_ClientMode()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var writer = new StringWriter();

        // Act
        var result = await renderer.PrerenderComponentAsync(httpContext, typeof(SimpleComponent), new WebAssemblyRenderMode(prerender: false), ParameterView.Empty);
        await renderer.Dispatcher.InvokeAsync(() => result.WriteTo(writer, HtmlEncoder.Default));
        var content = writer.ToString();
        var match = Regex.Match(content, ComponentPattern);

        // Assert
        Assert.True(match.Success);
        var marker = JsonSerializer.Deserialize<WebAssemblyComponentMarker>(match.Groups[1].Value, ServerComponentSerializationSettings.JsonSerializationOptions);
        Assert.Null(marker.PrerenderId);
        Assert.Equal("webassembly", marker.Type);
        Assert.Equal(typeof(SimpleComponent).Assembly.GetName().Name, marker.Assembly);
        Assert.Equal(typeof(SimpleComponent).FullName, marker.TypeName);
        Assert.Empty(httpContext.Items);
    }

    [Fact]
    public async Task CanPrerender_ParameterlessComponent_ClientMode()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var writer = new StringWriter();

        // Act
        var result = await renderer.PrerenderComponentAsync(httpContext, typeof(SimpleComponent), RenderMode.WebAssembly, ParameterView.Empty);
        await renderer.Dispatcher.InvokeAsync(() => result.WriteTo(writer, HtmlEncoder.Default));
        var content = writer.ToString();
        var match = Regex.Match(content, PrerenderedComponentPattern, RegexOptions.Multiline);

        // Assert
        Assert.True(match.Success);
        var preamble = match.Groups["preamble"].Value;
        var preambleMarker = JsonSerializer.Deserialize<WebAssemblyComponentMarker>(preamble, ServerComponentSerializationSettings.JsonSerializationOptions);
        Assert.NotNull(preambleMarker.PrerenderId);
        Assert.Equal("webassembly", preambleMarker.Type);
        Assert.Equal(typeof(SimpleComponent).Assembly.GetName().Name, preambleMarker.Assembly);
        Assert.Equal(typeof(SimpleComponent).FullName, preambleMarker.TypeName);

        var prerenderedContent = match.Groups["content"].Value;
        Assert.Equal("<h1>Hello from SimpleComponent</h1>", prerenderedContent);

        var epilogue = match.Groups["epilogue"].Value;
        var epilogueMarker = JsonSerializer.Deserialize<WebAssemblyComponentMarker>(epilogue, ServerComponentSerializationSettings.JsonSerializationOptions);
        Assert.Equal(preambleMarker.PrerenderId, epilogueMarker.PrerenderId);
        Assert.Null(epilogueMarker.Assembly);
        Assert.Null(epilogueMarker.TypeName);
        Assert.Null(epilogueMarker.Type);
        Assert.Null(epilogueMarker.ParameterDefinitions);
        Assert.Null(epilogueMarker.ParameterValues);
        var (_, mode) = Assert.Single(httpContext.Items);
        var invoked = Assert.IsType<InvokedRenderModes>(mode);
        Assert.Equal(InvokedRenderModes.Mode.WebAssembly, invoked.Value);
    }

    [Fact]
    public async Task CanRender_ComponentWithParameters_ClientMode()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var writer = new StringWriter();

        // Act
        var result = await renderer.PrerenderComponentAsync(httpContext, typeof(GreetingComponent),
            new WebAssemblyRenderMode(prerender: false),
            ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { "Name", "Daniel" }
            }));
        await renderer.Dispatcher.InvokeAsync(() => result.WriteTo(writer, HtmlEncoder.Default));
        var content = writer.ToString();
        var match = Regex.Match(content, ComponentPattern);

        // Assert
        Assert.True(match.Success);
        var marker = JsonSerializer.Deserialize<WebAssemblyComponentMarker>(match.Groups[1].Value, ServerComponentSerializationSettings.JsonSerializationOptions);
        Assert.Null(marker.PrerenderId);
        Assert.Equal("webassembly", marker.Type);
        Assert.Equal(typeof(GreetingComponent).Assembly.GetName().Name, marker.Assembly);
        Assert.Equal(typeof(GreetingComponent).FullName, marker.TypeName);

        var parameterDefinition = Assert.Single(
            JsonSerializer.Deserialize<ComponentParameter[]>(Convert.FromBase64String(marker.ParameterDefinitions), WebAssemblyComponentSerializationSettings.JsonSerializationOptions));
        Assert.Equal("Name", parameterDefinition.Name);
        Assert.Equal("System.String", parameterDefinition.TypeName);
        Assert.Equal("System.Private.CoreLib", parameterDefinition.Assembly);

        var value = Assert.Single(JsonSerializer.Deserialize<object[]>(Convert.FromBase64String(marker.ParameterValues), WebAssemblyComponentSerializationSettings.JsonSerializationOptions));
        var rawValue = Assert.IsType<JsonElement>(value);
        Assert.Equal("Daniel", rawValue.GetString());
    }

    [Fact]
    public async Task CanRender_ComponentWithNullParameters_ClientMode()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var writer = new StringWriter();

        // Act
        var result = await renderer.PrerenderComponentAsync(httpContext, typeof(GreetingComponent),
            new WebAssemblyRenderMode(prerender: false),
            ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { "Name", null }
            }));
        await renderer.Dispatcher.InvokeAsync(() => result.WriteTo(writer, HtmlEncoder.Default));
        var content = writer.ToString();
        var match = Regex.Match(content, ComponentPattern);

        // Assert
        Assert.True(match.Success);
        var marker = JsonSerializer.Deserialize<WebAssemblyComponentMarker>(match.Groups[1].Value, ServerComponentSerializationSettings.JsonSerializationOptions);
        Assert.Null(marker.PrerenderId);
        Assert.Equal("webassembly", marker.Type);
        Assert.Equal(typeof(GreetingComponent).Assembly.GetName().Name, marker.Assembly);
        Assert.Equal(typeof(GreetingComponent).FullName, marker.TypeName);

        var parameterDefinition = Assert.Single(JsonSerializer.Deserialize<ComponentParameter[]>(Convert.FromBase64String(marker.ParameterDefinitions), WebAssemblyComponentSerializationSettings.JsonSerializationOptions));
        Assert.Equal("Name", parameterDefinition.Name);
        Assert.Null(parameterDefinition.TypeName);
        Assert.Null(parameterDefinition.Assembly);

        var value = Assert.Single(JsonSerializer.Deserialize<object[]>(Convert.FromBase64String(marker.ParameterValues), WebAssemblyComponentSerializationSettings.JsonSerializationOptions));
        Assert.Null(value);
    }

    [Fact]
    public async Task CanPrerender_ComponentWithParameters_ClientMode()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var writer = new StringWriter();

        // Act
        var result = await renderer.PrerenderComponentAsync(httpContext, typeof(GreetingComponent),
            RenderMode.WebAssembly,
            ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { "Name", "Daniel" }
            }));
        await renderer.Dispatcher.InvokeAsync(() => result.WriteTo(writer, HtmlEncoder.Default));
        var content = writer.ToString();
        var match = Regex.Match(content, PrerenderedComponentPattern, RegexOptions.Multiline);

        // Assert
        Assert.True(match.Success);
        var preamble = match.Groups["preamble"].Value;
        var preambleMarker = JsonSerializer.Deserialize<WebAssemblyComponentMarker>(preamble, ServerComponentSerializationSettings.JsonSerializationOptions);
        Assert.NotNull(preambleMarker.PrerenderId);
        Assert.Equal("webassembly", preambleMarker.Type);
        Assert.Equal(typeof(GreetingComponent).Assembly.GetName().Name, preambleMarker.Assembly);
        Assert.Equal(typeof(GreetingComponent).FullName, preambleMarker.TypeName);

        var parameterDefinition = Assert.Single(JsonSerializer.Deserialize<ComponentParameter[]>(Convert.FromBase64String(preambleMarker.ParameterDefinitions), WebAssemblyComponentSerializationSettings.JsonSerializationOptions));
        Assert.Equal("Name", parameterDefinition.Name);
        Assert.Equal("System.String", parameterDefinition.TypeName);
        Assert.Equal("System.Private.CoreLib", parameterDefinition.Assembly);

        var value = Assert.Single(JsonSerializer.Deserialize<object[]>(Convert.FromBase64String(preambleMarker.ParameterValues), WebAssemblyComponentSerializationSettings.JsonSerializationOptions));
        var rawValue = Assert.IsType<JsonElement>(value);
        Assert.Equal("Daniel", rawValue.GetString());

        var prerenderedContent = match.Groups["content"].Value;
        Assert.Equal("<p>Hello Daniel!</p>", prerenderedContent);

        var epilogue = match.Groups["epilogue"].Value;
        var epilogueMarker = JsonSerializer.Deserialize<WebAssemblyComponentMarker>(epilogue, ServerComponentSerializationSettings.JsonSerializationOptions);
        Assert.Equal(preambleMarker.PrerenderId, epilogueMarker.PrerenderId);
        Assert.Null(epilogueMarker.Assembly);
        Assert.Null(epilogueMarker.TypeName);
        Assert.Null(epilogueMarker.Type);
        Assert.Null(epilogueMarker.ParameterDefinitions);
        Assert.Null(epilogueMarker.ParameterValues);
    }

    [Fact]
    public async Task CanPrerender_ComponentWithNullParameters_ClientMode()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var writer = new StringWriter();

        // Act
        var result = await renderer.PrerenderComponentAsync(httpContext, typeof(GreetingComponent),
            RenderMode.WebAssembly,
            ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { "Name", null }
            }));
        await renderer.Dispatcher.InvokeAsync(() => result.WriteTo(writer, HtmlEncoder.Default));
        var content = writer.ToString();
        var match = Regex.Match(content, PrerenderedComponentPattern, RegexOptions.Multiline);

        // Assert
        Assert.True(match.Success);
        var preamble = match.Groups["preamble"].Value;
        var preambleMarker = JsonSerializer.Deserialize<WebAssemblyComponentMarker>(preamble, ServerComponentSerializationSettings.JsonSerializationOptions);
        Assert.NotNull(preambleMarker.PrerenderId);
        Assert.Equal("webassembly", preambleMarker.Type);
        Assert.Equal(typeof(GreetingComponent).Assembly.GetName().Name, preambleMarker.Assembly);
        Assert.Equal(typeof(GreetingComponent).FullName, preambleMarker.TypeName);

        var parameterDefinition = Assert.Single(JsonSerializer.Deserialize<ComponentParameter[]>(Convert.FromBase64String(preambleMarker.ParameterDefinitions), WebAssemblyComponentSerializationSettings.JsonSerializationOptions));
        Assert.Equal("Name", parameterDefinition.Name);
        Assert.Null(parameterDefinition.TypeName);
        Assert.Null(parameterDefinition.Assembly);

        var value = Assert.Single(JsonSerializer.Deserialize<object[]>(Convert.FromBase64String(preambleMarker.ParameterValues), WebAssemblyComponentSerializationSettings.JsonSerializationOptions));
        Assert.Null(value);

        var prerenderedContent = match.Groups["content"].Value;
        Assert.Equal("<p>Hello (null)!</p>", prerenderedContent);

        var epilogue = match.Groups["epilogue"].Value;
        var epilogueMarker = JsonSerializer.Deserialize<WebAssemblyComponentMarker>(epilogue, ServerComponentSerializationSettings.JsonSerializationOptions);
        Assert.Equal(preambleMarker.PrerenderId, epilogueMarker.PrerenderId);
        Assert.Null(epilogueMarker.Assembly);
        Assert.Null(epilogueMarker.TypeName);
        Assert.Null(epilogueMarker.Type);
        Assert.Null(epilogueMarker.ParameterDefinitions);
        Assert.Null(epilogueMarker.ParameterValues);
    }

    [Fact]
    public async Task CanRender_ParameterlessComponent()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var writer = new StringWriter();

        // Act
        var result = await renderer.PrerenderComponentAsync(httpContext, typeof(SimpleComponent), null, ParameterView.Empty);
        await renderer.Dispatcher.InvokeAsync(() => result.WriteTo(writer, HtmlEncoder.Default));
        var content = writer.ToString();

        // Assert
        Assert.Equal("<h1>Hello from SimpleComponent</h1>", content);
    }

    [Fact]
    public async Task CanRender_ParameterlessComponent_ServerMode()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var protector = _dataprotectorProvider.CreateProtector(ServerComponentSerializationSettings.DataProtectionProviderPurpose)
            .ToTimeLimitedDataProtector();

        // Act
        var result = await renderer.PrerenderComponentAsync(httpContext, typeof(SimpleComponent), new ServerRenderMode(false), ParameterView.Empty);
        var content = await renderer.Dispatcher.InvokeAsync(() => HtmlContentToString(result));
        var match = Regex.Match(content, ComponentPattern);

        // Assert
        Assert.True(match.Success);
        var marker = JsonSerializer.Deserialize<ServerComponentMarker>(match.Groups[1].Value, ServerComponentSerializationSettings.JsonSerializationOptions);
        Assert.Equal(0, marker.Sequence);
        Assert.Null(marker.PrerenderId);
        Assert.NotNull(marker.Descriptor);
        Assert.Equal("server", marker.Type);

        var unprotectedServerComponent = protector.Unprotect(marker.Descriptor);
        var serverComponent = JsonSerializer.Deserialize<ServerComponent>(unprotectedServerComponent, ServerComponentSerializationSettings.JsonSerializationOptions);
        Assert.Equal(0, serverComponent.Sequence);
        Assert.Equal(typeof(SimpleComponent).Assembly.GetName().Name, serverComponent.AssemblyName);
        Assert.Equal(typeof(SimpleComponent).FullName, serverComponent.TypeName);
        Assert.NotEqual(Guid.Empty, serverComponent.InvocationId);

        Assert.Equal("no-cache, no-store, max-age=0", httpContext.Response.Headers.CacheControl);
        Assert.DoesNotContain(httpContext.Items.Values, value => value is InvokedRenderModes);
    }

    [Fact]
    public async Task CanPrerender_ParameterlessComponent_ServerMode()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var protector = _dataprotectorProvider.CreateProtector(ServerComponentSerializationSettings.DataProtectionProviderPurpose)
            .ToTimeLimitedDataProtector();

        // Act
        var result = await renderer.PrerenderComponentAsync(httpContext, typeof(SimpleComponent), RenderMode.Server, ParameterView.Empty);
        var content = await renderer.Dispatcher.InvokeAsync(() => HtmlContentToString(result));
        var match = Regex.Match(content, PrerenderedComponentPattern, RegexOptions.Multiline);

        // Assert
        Assert.True(match.Success);
        var preamble = match.Groups["preamble"].Value;
        var preambleMarker = JsonSerializer.Deserialize<ServerComponentMarker>(preamble, ServerComponentSerializationSettings.JsonSerializationOptions);
        Assert.Equal(0, preambleMarker.Sequence);
        Assert.NotNull(preambleMarker.PrerenderId);
        Assert.NotNull(preambleMarker.Descriptor);
        Assert.Equal("server", preambleMarker.Type);

        var unprotectedServerComponent = protector.Unprotect(preambleMarker.Descriptor);
        var serverComponent = JsonSerializer.Deserialize<ServerComponent>(unprotectedServerComponent, ServerComponentSerializationSettings.JsonSerializationOptions);
        Assert.NotEqual(default, serverComponent);
        Assert.Equal(0, serverComponent.Sequence);
        Assert.Equal(typeof(SimpleComponent).Assembly.GetName().Name, serverComponent.AssemblyName);
        Assert.Equal(typeof(SimpleComponent).FullName, serverComponent.TypeName);
        Assert.NotEqual(Guid.Empty, serverComponent.InvocationId);

        var prerenderedContent = match.Groups["content"].Value;
        Assert.Equal("<h1>Hello from SimpleComponent</h1>", prerenderedContent);

        var epilogue = match.Groups["epilogue"].Value;
        var epilogueMarker = JsonSerializer.Deserialize<ServerComponentMarker>(epilogue, ServerComponentSerializationSettings.JsonSerializationOptions);
        Assert.Equal(preambleMarker.PrerenderId, epilogueMarker.PrerenderId);
        Assert.Null(epilogueMarker.Sequence);
        Assert.Null(epilogueMarker.Descriptor);
        Assert.Null(epilogueMarker.Type);

        Assert.Equal("no-cache, no-store, max-age=0", httpContext.Response.Headers.CacheControl);
        var (_, mode) = Assert.Single(httpContext.Items, (kvp) => kvp.Value is InvokedRenderModes);
        Assert.Equal(InvokedRenderModes.Mode.Server, ((InvokedRenderModes)mode).Value);
    }

    [Fact]
    public async Task Prerender_ServerAndClientComponentUpdatesInvokedPrerenderModes()
    {
        // Arrange
        var httpContext = GetHttpContext();

        // Act
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object> { { "Name", "SomeName" } });
        var server = await renderer.PrerenderComponentAsync(httpContext, typeof(GreetingComponent), RenderMode.Server, parameters);
        var client = await renderer.PrerenderComponentAsync(httpContext, typeof(GreetingComponent), RenderMode.WebAssembly, parameters);

        // Assert
        var (_, mode) = Assert.Single(httpContext.Items, (kvp) => kvp.Value is InvokedRenderModes);
        Assert.Equal(InvokedRenderModes.Mode.ServerAndWebAssembly, ((InvokedRenderModes)mode).Value);
    }

    [Fact]
    public async Task CanRenderMultipleServerComponents()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var protector = _dataprotectorProvider.CreateProtector(ServerComponentSerializationSettings.DataProtectionProviderPurpose)
            .ToTimeLimitedDataProtector();

        // Act
        var firstResult = await renderer.PrerenderComponentAsync(httpContext, typeof(SimpleComponent), new ServerRenderMode(true), ParameterView.Empty);
        var firstComponent = await renderer.Dispatcher.InvokeAsync(() => HtmlContentToString(firstResult));
        var firstMatch = Regex.Match(firstComponent, PrerenderedComponentPattern, RegexOptions.Multiline);

        var secondResult = await renderer.PrerenderComponentAsync(httpContext, typeof(SimpleComponent), new ServerRenderMode(false), ParameterView.Empty);
        var secondComponent = await renderer.Dispatcher.InvokeAsync(() => HtmlContentToString(secondResult));
        var secondMatch = Regex.Match(secondComponent, ComponentPattern);

        // Assert
        Assert.True(firstMatch.Success);
        var preamble = firstMatch.Groups["preamble"].Value;
        var preambleMarker = JsonSerializer.Deserialize<ServerComponentMarker>(preamble, ServerComponentSerializationSettings.JsonSerializationOptions);
        Assert.Equal(0, preambleMarker.Sequence);
        Assert.NotNull(preambleMarker.Descriptor);

        var unprotectedFirstServerComponent = protector.Unprotect(preambleMarker.Descriptor);
        var firstServerComponent = JsonSerializer.Deserialize<ServerComponent>(unprotectedFirstServerComponent, ServerComponentSerializationSettings.JsonSerializationOptions);
        Assert.Equal(0, firstServerComponent.Sequence);
        Assert.NotEqual(Guid.Empty, firstServerComponent.InvocationId);

        Assert.True(secondMatch.Success);
        var marker = secondMatch.Groups[1].Value;
        var markerMarker = JsonSerializer.Deserialize<ServerComponentMarker>(marker, ServerComponentSerializationSettings.JsonSerializationOptions);
        Assert.Equal(1, markerMarker.Sequence);
        Assert.NotNull(markerMarker.Descriptor);

        var unprotectedSecondServerComponent = protector.Unprotect(markerMarker.Descriptor);
        var secondServerComponent = JsonSerializer.Deserialize<ServerComponent>(unprotectedSecondServerComponent, ServerComponentSerializationSettings.JsonSerializationOptions);
        Assert.Equal(1, secondServerComponent.Sequence);

        Assert.Equal(firstServerComponent.InvocationId, secondServerComponent.InvocationId);
    }

    [Fact]
    public async Task CanRender_ComponentWithParametersObject()
    {
        // Arrange
        var httpContext = GetHttpContext();

        // Act
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object> { { "Name", "SomeName" } });
        var result = await renderer.PrerenderComponentAsync(httpContext, typeof(GreetingComponent), null, parameters);

        // Assert
        var content = await renderer.Dispatcher.InvokeAsync(() => HtmlContentToString(result));
        Assert.Equal("<p>Hello SomeName!</p>", content);
    }

    [Fact]
    public async Task CanRender_ComponentWithParameters_ServerMode()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var protector = _dataprotectorProvider.CreateProtector(ServerComponentSerializationSettings.DataProtectionProviderPurpose)
            .ToTimeLimitedDataProtector();

        // Act
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object> { { "Name", "SomeName" } });
        var result = await renderer.PrerenderComponentAsync(httpContext, typeof(GreetingComponent), new ServerRenderMode(false), parameters);
        var content = await renderer.Dispatcher.InvokeAsync(() => HtmlContentToString(result));
        var match = Regex.Match(content, ComponentPattern);

        // Assert
        Assert.True(match.Success);
        var marker = JsonSerializer.Deserialize<ServerComponentMarker>(match.Groups[1].Value, ServerComponentSerializationSettings.JsonSerializationOptions);
        Assert.Equal(0, marker.Sequence);
        Assert.Null(marker.PrerenderId);
        Assert.NotNull(marker.Descriptor);
        Assert.Equal("server", marker.Type);

        var unprotectedServerComponent = protector.Unprotect(marker.Descriptor);
        var serverComponent = JsonSerializer.Deserialize<ServerComponent>(unprotectedServerComponent, ServerComponentSerializationSettings.JsonSerializationOptions);
        Assert.Equal(0, serverComponent.Sequence);
        Assert.Equal(typeof(GreetingComponent).Assembly.GetName().Name, serverComponent.AssemblyName);
        Assert.Equal(typeof(GreetingComponent).FullName, serverComponent.TypeName);
        Assert.NotEqual(Guid.Empty, serverComponent.InvocationId);

        var parameterDefinition = Assert.Single(serverComponent.ParameterDefinitions);
        Assert.Equal("Name", parameterDefinition.Name);
        Assert.Equal("System.String", parameterDefinition.TypeName);
        Assert.Equal("System.Private.CoreLib", parameterDefinition.Assembly);

        var value = Assert.Single(serverComponent.ParameterValues);
        var rawValue = Assert.IsType<JsonElement>(value);
        Assert.Equal("SomeName", rawValue.GetString());
    }

    [Fact]
    public async Task CanRender_ComponentWithNullParameters_ServerMode()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var protector = _dataprotectorProvider.CreateProtector(ServerComponentSerializationSettings.DataProtectionProviderPurpose)
            .ToTimeLimitedDataProtector();

        // Act
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object> { { "Name", null } });
        var result = await renderer.PrerenderComponentAsync(httpContext, typeof(GreetingComponent), new ServerRenderMode(false), parameters);
        var content = await renderer.Dispatcher.InvokeAsync(() => HtmlContentToString(result));
        var match = Regex.Match(content, ComponentPattern);

        // Assert
        Assert.True(match.Success);
        var marker = JsonSerializer.Deserialize<ServerComponentMarker>(match.Groups[1].Value, ServerComponentSerializationSettings.JsonSerializationOptions);
        Assert.Equal(0, marker.Sequence);
        Assert.Null(marker.PrerenderId);
        Assert.NotNull(marker.Descriptor);
        Assert.Equal("server", marker.Type);

        var unprotectedServerComponent = protector.Unprotect(marker.Descriptor);
        var serverComponent = JsonSerializer.Deserialize<ServerComponent>(unprotectedServerComponent, ServerComponentSerializationSettings.JsonSerializationOptions);
        Assert.Equal(0, serverComponent.Sequence);
        Assert.Equal(typeof(GreetingComponent).Assembly.GetName().Name, serverComponent.AssemblyName);
        Assert.Equal(typeof(GreetingComponent).FullName, serverComponent.TypeName);
        Assert.NotEqual(Guid.Empty, serverComponent.InvocationId);

        Assert.NotNull(serverComponent.ParameterDefinitions);
        var parameterDefinition = Assert.Single(serverComponent.ParameterDefinitions);
        Assert.Equal("Name", parameterDefinition.Name);
        Assert.Null(parameterDefinition.TypeName);
        Assert.Null(parameterDefinition.Assembly);

        var value = Assert.Single(serverComponent.ParameterValues);
        Assert.Null(value);
    }

    [Fact]
    public async Task CanPrerender_ComponentWithParameters_ServerPrerenderedMode()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var protector = _dataprotectorProvider.CreateProtector(ServerComponentSerializationSettings.DataProtectionProviderPurpose)
            .ToTimeLimitedDataProtector();

        // Act
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object> { { "Name", "SomeName" } });
        var result = await renderer.PrerenderComponentAsync(httpContext, typeof(GreetingComponent), RenderMode.Server, parameters);
        var content = await renderer.Dispatcher.InvokeAsync(() => HtmlContentToString(result));
        var match = Regex.Match(content, PrerenderedComponentPattern, RegexOptions.Multiline);

        // Assert
        Assert.True(match.Success);
        var preamble = match.Groups["preamble"].Value;
        var preambleMarker = JsonSerializer.Deserialize<ServerComponentMarker>(preamble, ServerComponentSerializationSettings.JsonSerializationOptions);
        Assert.Equal(0, preambleMarker.Sequence);
        Assert.NotNull(preambleMarker.PrerenderId);
        Assert.NotNull(preambleMarker.Descriptor);
        Assert.Equal("server", preambleMarker.Type);

        var unprotectedServerComponent = protector.Unprotect(preambleMarker.Descriptor);
        var serverComponent = JsonSerializer.Deserialize<ServerComponent>(unprotectedServerComponent, ServerComponentSerializationSettings.JsonSerializationOptions);
        Assert.NotEqual(default, serverComponent);
        Assert.Equal(0, serverComponent.Sequence);
        Assert.Equal(typeof(GreetingComponent).Assembly.GetName().Name, serverComponent.AssemblyName);
        Assert.Equal(typeof(GreetingComponent).FullName, serverComponent.TypeName);
        Assert.NotEqual(Guid.Empty, serverComponent.InvocationId);

        var parameterDefinition = Assert.Single(serverComponent.ParameterDefinitions);
        Assert.Equal("Name", parameterDefinition.Name);
        Assert.Equal("System.String", parameterDefinition.TypeName);
        Assert.Equal("System.Private.CoreLib", parameterDefinition.Assembly);

        var value = Assert.Single(serverComponent.ParameterValues);
        var rawValue = Assert.IsType<JsonElement>(value);
        Assert.Equal("SomeName", rawValue.GetString());

        var prerenderedContent = match.Groups["content"].Value;
        Assert.Equal("<p>Hello SomeName!</p>", prerenderedContent);

        var epilogue = match.Groups["epilogue"].Value;
        var epilogueMarker = JsonSerializer.Deserialize<ServerComponentMarker>(epilogue, ServerComponentSerializationSettings.JsonSerializationOptions);
        Assert.Equal(preambleMarker.PrerenderId, epilogueMarker.PrerenderId);
        Assert.Null(epilogueMarker.Sequence);
        Assert.Null(epilogueMarker.Descriptor);
        Assert.Null(epilogueMarker.Type);
    }

    [Fact]
    public async Task CanPrerender_ComponentWithNullParameters_ServerPrerenderedMode()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var protector = _dataprotectorProvider.CreateProtector(ServerComponentSerializationSettings.DataProtectionProviderPurpose)
            .ToTimeLimitedDataProtector();

        // Act
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object> { { "Name", null } });
        var result = await renderer.PrerenderComponentAsync(httpContext, typeof(GreetingComponent), RenderMode.Server, parameters);
        var content = await renderer.Dispatcher.InvokeAsync(() => HtmlContentToString(result));
        var match = Regex.Match(content, PrerenderedComponentPattern, RegexOptions.Multiline);

        // Assert
        Assert.True(match.Success);
        var preamble = match.Groups["preamble"].Value;
        var preambleMarker = JsonSerializer.Deserialize<ServerComponentMarker>(preamble, ServerComponentSerializationSettings.JsonSerializationOptions);
        Assert.Equal(0, preambleMarker.Sequence);
        Assert.NotNull(preambleMarker.PrerenderId);
        Assert.NotNull(preambleMarker.Descriptor);
        Assert.Equal("server", preambleMarker.Type);

        var unprotectedServerComponent = protector.Unprotect(preambleMarker.Descriptor);
        var serverComponent = JsonSerializer.Deserialize<ServerComponent>(unprotectedServerComponent, ServerComponentSerializationSettings.JsonSerializationOptions);
        Assert.NotEqual(default, serverComponent);
        Assert.Equal(0, serverComponent.Sequence);
        Assert.Equal(typeof(GreetingComponent).Assembly.GetName().Name, serverComponent.AssemblyName);
        Assert.Equal(typeof(GreetingComponent).FullName, serverComponent.TypeName);
        Assert.NotEqual(Guid.Empty, serverComponent.InvocationId);

        Assert.NotNull(serverComponent.ParameterDefinitions);
        var parameterDefinition = Assert.Single(serverComponent.ParameterDefinitions);
        Assert.Equal("Name", parameterDefinition.Name);
        Assert.Null(parameterDefinition.TypeName);
        Assert.Null(parameterDefinition.Assembly);

        var value = Assert.Single(serverComponent.ParameterValues);
        Assert.Null(value);

        var prerenderedContent = match.Groups["content"].Value;
        Assert.Equal("<p>Hello (null)!</p>", prerenderedContent);

        var epilogue = match.Groups["epilogue"].Value;
        var epilogueMarker = JsonSerializer.Deserialize<ServerComponentMarker>(epilogue, ServerComponentSerializationSettings.JsonSerializationOptions);
        Assert.Equal(preambleMarker.PrerenderId, epilogueMarker.PrerenderId);
        Assert.Null(epilogueMarker.Sequence);
        Assert.Null(epilogueMarker.Descriptor);
        Assert.Null(epilogueMarker.Type);
    }

    [Fact]
    public async Task ComponentWithInvalidRenderMode_Throws()
    {
        // Arrange
        var httpContext = GetHttpContext();

        // Act & Assert
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object> { { "Name", "SomeName" } });
        var ex = await ExceptionAssert.ThrowsArgumentAsync(
            async () => await renderer.PrerenderComponentAsync(httpContext, typeof(GreetingComponent), new NonexistentRenderMode(), parameters),
            "renderMode",
            $"Server-side rendering does not support the render mode '{typeof(NonexistentRenderMode)}'.");
    }

    class NonexistentRenderMode : IComponentRenderMode { }

    [Fact]
    public async Task RenderComponent_DoesNotInvokeOnAfterRenderInComponent()
    {
        // Arrange
        var httpContext = GetHttpContext();

        // Act
        var state = new OnAfterRenderState();
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object> { { "state", state } });
        var result = await renderer.PrerenderComponentAsync(httpContext, typeof(OnAfterRenderComponent), null, parameters);

        // Assert
        var content = await renderer.Dispatcher.InvokeAsync(() => HtmlContentToString(result));
        Assert.Equal("<p>Hello</p>", content);
        Assert.False(state.OnAfterRenderRan);
    }

    [Fact]
    public async Task DisposableComponents_GetDisposedAfterScopeCompletes()
    {
        // Arrange
        var collection = CreateDefaultServiceCollection();
        collection.TryAddScoped<EndpointHtmlRenderer>();
        collection.TryAddSingleton(HtmlEncoder.Default);
        collection.TryAddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        collection.TryAddSingleton<ServerComponentSerializer>();
        collection.TryAddSingleton(_dataprotectorProvider);
        collection.TryAddSingleton<WebAssemblyComponentSerializer>();

        var provider = collection.BuildServiceProvider();
        var scope = provider.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var scopedProvider = scope.ServiceProvider;
        var context = new DefaultHttpContext() { RequestServices = scopedProvider };
        var httpContext = GetHttpContext(context);
        var renderer = scopedProvider.GetRequiredService<EndpointHtmlRenderer>();

        // Act
        var state = new AsyncDisposableState();
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object> { { "state", state } });
        var result = await renderer.PrerenderComponentAsync(httpContext, typeof(AsyncDisposableComponent), null, parameters);

        // Assert
        var content = await renderer.Dispatcher.InvokeAsync(() => HtmlContentToString(result));
        Assert.Equal("<p>Hello</p>", content);
        await ((IAsyncDisposable)scope).DisposeAsync();

        Assert.True(state.AsyncDisposableRan);
    }

    [Fact]
    public async Task CanCatch_ComponentWithSynchronousException()
    {
        // Arrange
        var httpContext = GetHttpContext();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await renderer.PrerenderComponentAsync(
            httpContext,
            typeof(ExceptionComponent),
            null,
            ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { "IsAsync", false }
            })));

        // Assert
        Assert.Equal("Threw an exception synchronously", exception.Message);
    }

    [Fact]
    public async Task CanCatch_ComponentWithAsynchronousException()
    {
        // Arrange
        var httpContext = GetHttpContext();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await renderer.PrerenderComponentAsync(
            httpContext,
            typeof(ExceptionComponent),
            null,
            ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { "IsAsync", true }
            })));

        // Assert
        Assert.Equal("Threw an exception asynchronously", exception.Message);
    }

    [Fact]
    public async Task Rendering_ComponentWithJsInteropThrows()
    {
        // Arrange
        var httpContext = GetHttpContext();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await renderer.PrerenderComponentAsync(
            httpContext,
            typeof(ExceptionComponent),
            null,
            ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { "JsInterop", true }
            })));

        // Assert
        Assert.Equal("JavaScript interop calls cannot be issued during server-side static rendering, because the page "
            + "has not yet loaded in the browser. Statically-rendered components must wrap any JavaScript interop calls "
            + "in conditional logic to ensure those interop calls are not attempted during static rendering.",
            exception.Message);
    }

    [Fact]
    public async Task UriHelperRedirect_ThrowsInvalidOperationException_WhenResponseHasAlreadyStarted()
    {
        // Arrange
        var ctx = new DefaultHttpContext();
        ctx.Request.Scheme = "http";
        ctx.Request.Host = new HostString("localhost");
        ctx.Request.PathBase = "/base";
        ctx.Request.Path = "/path";
        ctx.Request.QueryString = new QueryString("?query=value");
        var responseMock = new Mock<IHttpResponseFeature>();
        responseMock.Setup(r => r.HasStarted).Returns(true);
        ctx.Features.Set(responseMock.Object);
        var httpContext = GetHttpContext(ctx);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await renderer.PrerenderComponentAsync(
            httpContext,
            typeof(RedirectComponent),
            null,
            ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { "RedirectUri", "http://localhost/redirect" }
            })));

        Assert.Equal("A navigation command was attempted during prerendering after the server already started sending the response. " +
                        "Navigation commands can not be issued during server-side prerendering after the response from the server has started. Applications must buffer the" +
                        "response and avoid using features like FlushAsync() before all components on the page have been rendered to prevent failed navigation commands.",
            exception.Message);
    }

    [Fact]
    public async Task HtmlHelper_Redirects_WhenComponentNavigates()
    {
        // Arrange
        var ctx = new DefaultHttpContext();
        ctx.Request.Scheme = "http";
        ctx.Request.Host = new HostString("localhost");
        ctx.Request.PathBase = "/base";
        ctx.Request.Path = "/path";
        ctx.Request.QueryString = new QueryString("?query=value");
        var httpContext = GetHttpContext(ctx);

        // Act
        await renderer.PrerenderComponentAsync(
            httpContext,
            typeof(RedirectComponent),
            null,
            ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { "RedirectUri", "http://localhost/redirect" }
            }));

        // Assert
        Assert.Equal(302, ctx.Response.StatusCode);
        Assert.Equal("http://localhost/redirect", ctx.Response.Headers.Location);
    }

    [Fact]
    public async Task CanRender_AsyncComponent()
    {
        // Arrange
        var httpContext = GetHttpContext();

        // Act
        var result = await renderer.PrerenderComponentAsync(httpContext, typeof(AsyncComponent), null, ParameterView.Empty);
        var content = await renderer.Dispatcher.InvokeAsync(() => HtmlContentToString(result));

        // Assert
        Assert.Equal("Loaded", content);
    }

    [Fact]
    public void Duplicate_NamedEventHandlers_AcrossComponents_Throws()
    {
        // Arrange
        var expectedError = @"Two different components are trying to define the same named event 'default':
'TestComponent > NamedEventHandlerComponent'
'TestComponent > OtherNamedEventHandlerComponent'";

        var renderer = GetEndpointHtmlRenderer();
        renderer.SetFormHandlerName("default");

        var component = new TestComponent(builder =>
        {
            builder.OpenComponent<NamedEventHandlerComponent>(0);
            builder.CloseComponent();
            builder.OpenComponent<OtherNamedEventHandlerComponent>(1);
            builder.CloseComponent();
        });

        // Act
        var componentId = renderer.TestAssignRootComponentId(component);

        var exception = Assert.Throws<InvalidOperationException>(component.TriggerRender);
        Assert.Equal(expectedError, exception.Message);
    }

    [Fact]
    public void RecreatedComponent_AcrossDifferentBatches_WithNamedEventHandler_Throws()
    {
        // Arrange
        var expectedError = "The named event 'default' was already defined earlier by a component with id '1' but this " +
            "component was removed before receiving the dispatched event.";

        var renderer = GetEndpointHtmlRenderer();
        renderer.SetFormHandlerName("default");

        RenderFragment addComponentRender = builder =>
        {
            builder.OpenComponent<NamedEventHandlerComponent>(0);
            builder.CloseComponent();
        };
        RenderFragment removeComponentRender = builder =>
        {
        };

        var currentRender = addComponentRender;
        var component = new TestComponent(builder =>
        {
            currentRender(builder);
        });

        // Act
        var componentId = renderer.TestAssignRootComponentId(component);
        renderer.Dispatcher.InvokeAsync(component.TriggerRender);
        currentRender = removeComponentRender;
        renderer.Dispatcher.InvokeAsync(component.TriggerRender);
        currentRender = addComponentRender;

        var exception = Assert.Throws<InvalidOperationException>(component.TriggerRender);
        Assert.Equal(expectedError, exception.Message);
    }

    [Fact]
    public void SameComponent_WithNamedEvent_CanRenderSynchronously_MultipleTimes()
    {
        // Arrange
        var renderer = GetEndpointHtmlRenderer();
        renderer.SetFormHandlerName("default");

        var component = new TestComponent(builder =>
        {
            builder.OpenComponent<MultiRenderNamedEventHandlerComponent>(0);
            builder.CloseComponent();
        });

        // Act
        var componentId = renderer.TestAssignRootComponentId(component);
        renderer.Dispatcher.InvokeAsync(component.TriggerRender);

        // Assert
        Assert.Equal(2, renderer.TrackedNamedEvents.Count);
    }

    [Fact]
    public async Task SameComponent_WithNamedEvent_CanRenderAsynchronously_MultipleTimes()
    {
        // Arrange
        var renderer = GetEndpointHtmlRenderer();
        renderer.SetFormHandlerName("default");
        var continueTaskCompletion = new TaskCompletionSource();

        // Act
        var result = await renderer.Dispatcher.InvokeAsync(() => renderer.BeginRenderingComponent(
            typeof(MultiAsyncRenderNamedEventHandlerComponent),
            ParameterView.FromDictionary(new Dictionary<string, object>
            {
                [nameof(MultiAsyncRenderNamedEventHandlerComponent.Continue)] = continueTaskCompletion.Task
            })));

        // Assert
        continueTaskCompletion.SetResult();
        await result.QuiescenceTask;
        Assert.Equal(2, renderer.TrackedNamedEvents.Count);
    }

    [Fact]
    public async Task CanDispatchNamedEvent_ToComponent()
    {
        // Arrange
        var renderer = GetEndpointHtmlRenderer();
        renderer.SetFormHandlerName("default");
        var continueTaskCompletion = new TaskCompletionSource();
        var invoked = false;
        Action handler = () => invoked = true;
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var result = renderer.BeginRenderingComponent(
                typeof(NamedEventHandlerComponent),
                ParameterView.FromDictionary(new Dictionary<string, object>
                {
                    [nameof(NamedEventHandlerComponent.Handler)] = handler
                }));

            await result.QuiescenceTask;

            // Act
            await renderer.DispatchCapturedEvent();
        });

        // Assert
        Assert.True(invoked);
    }

    [Fact]
    public async Task Dispatching_WhenEventHasNotBeenFound_Throws()
    {
        // Arrange
        var renderer = GetEndpointHtmlRenderer();
        renderer.SetFormHandlerName("other");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            renderer.Dispatcher.InvokeAsync(async () =>
            {
                var result = renderer.BeginRenderingComponent(typeof(NamedEventHandlerComponent), ParameterView.Empty);
                await result.QuiescenceTask;

                // Act
                await renderer.DispatchCapturedEvent();
            }));

        Assert.Equal("No named event handler was captured for 'other'.", exception.Message);
    }

    [Fact]
    public void NamedEventHandlers_DifferentComponents_SameNamedHandlerInDifferentBatches_Throws()
    {
        // Arrange
        var expectedError = "The named event 'default' was already defined earlier by a component with id '1' but this " +
            "component was removed before receiving the dispatched event.";

        var renderer = GetEndpointHtmlRenderer();
        renderer.SetFormHandlerName("default");

        RenderFragment addComponentRender = builder =>
        {
            builder.OpenComponent<NamedEventHandlerComponent>(0);
            builder.CloseComponent();
        };

        RenderFragment removeComponentRender = builder =>
        {
        };
        RenderFragment thirdRender = builder =>
        {
            builder.OpenComponent<NamedEventHandlerComponent>(0);
            builder.CloseComponent();
        };

        var currentRender = addComponentRender;
        var component = new TestComponent(builder =>
        {
            currentRender(builder);
        });

        // Act
        var componentId = renderer.TestAssignRootComponentId(component);
        renderer.Dispatcher.InvokeAsync(component.TriggerRender);
        currentRender = removeComponentRender;
        renderer.Dispatcher.InvokeAsync(component.TriggerRender);
        currentRender = thirdRender;

        var exception = Assert.Throws<InvalidOperationException>(component.TriggerRender);
        Assert.Equal(expectedError, exception.Message);
    }

    [Fact]
    public async Task RenderMode_CanRenderInteractiveComponents()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var writer = new StringWriter();
        var protector = _dataprotectorProvider.CreateProtector(ServerComponentSerializationSettings.DataProtectionProviderPurpose)
            .ToTimeLimitedDataProtector();

        // Act
        var result = await renderer.PrerenderComponentAsync(httpContext, typeof(ComponentWithInteractiveChildren), null, ParameterView.Empty);
        await renderer.Dispatcher.InvokeAsync(() => result.WriteTo(writer, HtmlEncoder.Default));
        var content = writer.ToString();

        // Assert
        var lines = content.Replace("\r\n", "\n").Split('\n');
        var serverMarkerMatch = Regex.Match(lines[0], PrerenderedComponentPattern);
        var serverNonPrerenderedMarkerMatch = Regex.Match(lines[1], ComponentPattern);
        var webAssemblyMarkerMatch = Regex.Match(lines[2], PrerenderedComponentPattern);
        var webAssemblyNonPrerenderedMarkerMatch = Regex.Match(lines[3], ComponentPattern);

        // Server
        {
            var markerText = serverMarkerMatch.Groups[1].Value;
            var innerHtml = serverMarkerMatch.Groups[2].Value;

            var marker = JsonSerializer.Deserialize<ServerComponentMarker>(markerText, ServerComponentSerializationSettings.JsonSerializationOptions);
            Assert.Equal(0, marker.Sequence);
            Assert.NotNull(marker.PrerenderId);
            Assert.NotNull(marker.Descriptor);
            Assert.Equal("server", marker.Type);

            var unprotectedServerComponent = protector.Unprotect(marker.Descriptor);
            var serverComponent = JsonSerializer.Deserialize<ServerComponent>(unprotectedServerComponent, ServerComponentSerializationSettings.JsonSerializationOptions);
            Assert.Equal(0, serverComponent.Sequence);
            Assert.Equal(typeof(InteractiveGreetingServer).Assembly.GetName().Name, serverComponent.AssemblyName);
            Assert.Equal(typeof(InteractiveGreetingServer).FullName, serverComponent.TypeName);
            Assert.NotEqual(Guid.Empty, serverComponent.InvocationId);

            var parameterDefinition = Assert.Single(serverComponent.ParameterDefinitions);
            Assert.Equal("Name", parameterDefinition.Name);
            Assert.Equal("System.String", parameterDefinition.TypeName);
            Assert.Equal("System.Private.CoreLib", parameterDefinition.Assembly);

            var value = Assert.Single(serverComponent.ParameterValues);
            var rawValue = Assert.IsType<JsonElement>(value);
            Assert.Equal("ServerPrerendered", rawValue.GetString());

            Assert.Equal("<p>Hello ServerPrerendered!</p>", innerHtml);
        }

        // ServerNonPrerendered
        {
            var markerText = serverNonPrerenderedMarkerMatch.Groups[1].Value;

            var marker = JsonSerializer.Deserialize<ServerComponentMarker>(markerText, ServerComponentSerializationSettings.JsonSerializationOptions);
            Assert.Equal(1, marker.Sequence);
            Assert.Null(marker.PrerenderId);
            Assert.NotNull(marker.Descriptor);
            Assert.Equal("server", marker.Type);

            var unprotectedServerComponent = protector.Unprotect(marker.Descriptor);
            var serverComponent = JsonSerializer.Deserialize<ServerComponent>(unprotectedServerComponent, ServerComponentSerializationSettings.JsonSerializationOptions);
            Assert.Equal(1, serverComponent.Sequence);
            Assert.Equal(typeof(InteractiveGreetingServer).Assembly.GetName().Name, serverComponent.AssemblyName);
            Assert.Equal(typeof(InteractiveGreetingServerNonPrerendered).FullName, serverComponent.TypeName);
            Assert.NotEqual(Guid.Empty, serverComponent.InvocationId);

            var parameterDefinition = Assert.Single(serverComponent.ParameterDefinitions);
            Assert.Equal("Name", parameterDefinition.Name);
            Assert.Equal("System.String", parameterDefinition.TypeName);
            Assert.Equal("System.Private.CoreLib", parameterDefinition.Assembly);

            var value = Assert.Single(serverComponent.ParameterValues);
            var rawValue = Assert.IsType<JsonElement>(value);
            Assert.Equal("Server", rawValue.GetString());
        }

        // WebAssembly
        {
            var markerText = webAssemblyMarkerMatch.Groups[1].Value;
            var innerHtml = webAssemblyMarkerMatch.Groups[2].Value;

            var marker = JsonSerializer.Deserialize<WebAssemblyComponentMarker>(markerText, ServerComponentSerializationSettings.JsonSerializationOptions);
            Assert.Equal(typeof(InteractiveGreetingWebAssembly).FullName, marker.TypeName);

            var parameterValues = JsonSerializer.Deserialize<object[]>(Convert.FromBase64String(marker.ParameterValues), WebAssemblyComponentSerializationSettings.JsonSerializationOptions);
            Assert.Equal("WebAssemblyPrerendered", parameterValues.Single().ToString());

            Assert.Equal("<p>Hello WebAssemblyPrerendered!</p>", innerHtml);
        }

        // WebAssemblyNonPrerendered
        {
            var markerText = webAssemblyNonPrerenderedMarkerMatch.Groups[1].Value;

            var marker = JsonSerializer.Deserialize<WebAssemblyComponentMarker>(markerText, ServerComponentSerializationSettings.JsonSerializationOptions);
            Assert.Equal(typeof(InteractiveGreetingWebAssemblyNonPrerendered).FullName, marker.TypeName);

            var parameterValues = JsonSerializer.Deserialize<object[]>(Convert.FromBase64String(marker.ParameterValues), WebAssemblyComponentSerializationSettings.JsonSerializationOptions);
            Assert.Equal("WebAssembly", parameterValues.Single().ToString());
        }
    }

    [Fact]
    public async Task DoesNotEmitNestedRenderModeBoundaries()
    {
        // Arrange
        var httpContext = GetHttpContext();
        var writer = new StringWriter();

        // Act
        var result = await renderer.PrerenderComponentAsync(httpContext, typeof(InteractiveWithInteractiveChild),
            null,
            ParameterView.Empty);
        await renderer.Dispatcher.InvokeAsync(() => result.WriteTo(writer, HtmlEncoder.Default));
        var content = writer.ToString();

        // Assert
        var numMarkers = Regex.Matches(content, MarkerPrefix).Count;
        Assert.Equal(2, numMarkers); // A start and an end marker

        var match = Regex.Match(content, PrerenderedComponentPattern, RegexOptions.Singleline);
        Assert.True(match.Success);
        var preamble = match.Groups["preamble"].Value;
        var preambleMarker = JsonSerializer.Deserialize<WebAssemblyComponentMarker>(preamble, ServerComponentSerializationSettings.JsonSerializationOptions);
        Assert.NotNull(preambleMarker.PrerenderId);
        Assert.Equal("webassembly", preambleMarker.Type);

        var prerenderedContent = match.Groups["content"].Value;
        Assert.Equal("<h1>This is InteractiveWithInteractiveChild</h1>\n\n<p>Hello from InteractiveGreetingServer!</p>", prerenderedContent.Replace("\r\n", "\n"));
    }

    private class NamedEventHandlerComponent : ComponentBase
    {
        [Parameter]
        public Action Handler { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "form");
            builder.AddAttribute(1, "onsubmit", Handler ?? (() => { }));
            builder.SetEventHandlerName("default");
            builder.CloseElement();
        }
    }

    private class MultiRenderNamedEventHandlerComponent : ComponentBase
    {
        private bool hasRendered;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "form");
            if (!hasRendered)
            {
                builder.AddAttribute(1, "onsubmit", () => { });
                builder.SetEventHandlerName("default");
            }
            else
            {
                builder.AddAttribute(1, "onsubmit", () => { GC.KeepAlive(new object()); });
                builder.SetEventHandlerName("default");
            }
            builder.CloseElement();
            if (!hasRendered)
            {
                hasRendered = true;
                StateHasChanged();
            }
        }
    }

    private class MultiAsyncRenderNamedEventHandlerComponent : ComponentBase
    {
        private bool hasRendered;

        [Parameter] public Task Continue { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "form");
            if (!hasRendered)
            {
                builder.AddAttribute(1, "onsubmit", () => { });
                builder.SetEventHandlerName("default");
            }
            else
            {
                builder.AddAttribute(1, "onsubmit", () => { GC.KeepAlive(new object()); });
                builder.SetEventHandlerName("default");
            }
            builder.CloseElement();
        }

        protected override async Task OnInitializedAsync()
        {
            await Continue;
            hasRendered = true;
        }
    }

    private class OtherNamedEventHandlerComponent : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "form");
            builder.AddAttribute(1, "onsubmit", () => { });
            builder.SetEventHandlerName("default");
            builder.CloseElement();
        }
    }

    class TestComponent : AutoRenderComponent
    {
        private readonly RenderFragment _renderFragment;

        public TestComponent(RenderFragment renderFragment)
        {
            _renderFragment = renderFragment;
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
            => _renderFragment(builder);
    }

    private static string HtmlContentToString(IHtmlAsyncContent result)
    {
        var writer = new StringWriter();
        result.WriteTo(writer, HtmlEncoder.Default);
        return writer.ToString();
    }

    private TestEndpointHtmlRenderer GetEndpointHtmlRenderer(IServiceProvider services = null)
    {
        var effectiveServices = services ?? _services;
        return new TestEndpointHtmlRenderer(effectiveServices, NullLoggerFactory.Instance);
    }

    private class TestEndpointHtmlRenderer : EndpointHtmlRenderer
    {
        public TestEndpointHtmlRenderer(IServiceProvider serviceProvider, ILoggerFactory loggerFactory) : base(serviceProvider, loggerFactory)
        {
        }

        internal int TestAssignRootComponentId(IComponent component)
        {
            return base.AssignRootComponentId(component);
        }

        protected override void TrackNamedEventId(ulong eventHandlerId, int componentId, string eventNameId)
        {
            TrackedNamedEvents.Add(new TrackedNamedEvent(eventHandlerId, componentId, eventNameId));
            base.TrackNamedEventId(eventHandlerId, componentId, eventNameId);
        }

        public List<TrackedNamedEvent> TrackedNamedEvents { get; set; } = new List<TrackedNamedEvent>();

        public readonly record struct TrackedNamedEvent(ulong EventHandlerId, int ComponentId, string EventNameId);
    }

    private HttpContext GetHttpContext(HttpContext context = null)
    {
        context ??= new DefaultHttpContext();
        context.RequestServices ??= _services;
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("localhost");
        context.Request.PathBase = "/base";
        context.Request.Path = "/path";
        context.Request.QueryString = QueryString.FromUriComponent("?query=value");

        return context;
    }

    private static ServiceCollection CreateDefaultServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_dataprotectorProvider);
        services.AddSingleton<IJSRuntime, UnsupportedJavaScriptRuntime>();
        services.AddSingleton<NavigationManager, HttpNavigationManager>();
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton<ILogger<ComponentStatePersistenceManager>, NullLogger<ComponentStatePersistenceManager>>();
        services.AddSingleton<ComponentStatePersistenceManager>();
        services.AddSingleton(sp => sp.GetRequiredService<ComponentStatePersistenceManager>().State);
        services.AddSingleton<ServerComponentSerializer>();
        services.AddSingleton<FormDataProvider, HttpContextFormDataProvider>();
        return services;
    }

    private class RedirectComponent : ComponentBase
    {
        [Inject] NavigationManager NavigationManager { get; set; }

        [Parameter] public string RedirectUri { get; set; }

        [Parameter] public bool Force { get; set; }

        protected override void OnInitialized()
        {
            NavigationManager.NavigateTo(RedirectUri, Force);
        }
    }

    private class ExceptionComponent : ComponentBase
    {
        [Parameter] public bool IsAsync { get; set; }

        [Parameter] public bool JsInterop { get; set; }

        [Inject] IJSRuntime JsRuntime { get; set; }

        protected override async Task OnParametersSetAsync()
        {
            if (JsInterop)
            {
                await JsRuntime.InvokeAsync<int>("window.alert", "Interop!");
            }

            if (!IsAsync)
            {
                throw new InvalidOperationException("Threw an exception synchronously");
            }
            else
            {
                await Task.Yield();
                throw new InvalidOperationException("Threw an exception asynchronously");
            }
        }
    }

    private class OnAfterRenderComponent : ComponentBase
    {
        [Parameter] public OnAfterRenderState State { get; set; }

        protected override void OnAfterRender(bool firstRender)
        {
            State.OnAfterRenderRan = true;
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddMarkupContent(0, "<p>Hello</p>");
        }
    }

    private class OnAfterRenderState
    {
        public bool OnAfterRenderRan { get; set; }
    }

    private class AsyncDisposableComponent : ComponentBase, IAsyncDisposable
    {
        [Parameter] public AsyncDisposableState State { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddMarkupContent(0, "<p>Hello</p>");
        }

        public ValueTask DisposeAsync()
        {
            State.AsyncDisposableRan = true;
            return default;
        }
    }

    private class AsyncDisposableState
    {
        public bool AsyncDisposableRan { get; set; }
    }
}
