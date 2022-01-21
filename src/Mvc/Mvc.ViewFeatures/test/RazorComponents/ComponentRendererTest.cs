// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Moq;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

public class ComponentRendererTest
{
    private const string PrerenderedComponentPattern = "^<!--Blazor:(?<preamble>.*?)-->(?<content>.+?)<!--Blazor:(?<epilogue>.*?)-->$";
    private const string ComponentPattern = "^<!--Blazor:(.*?)-->$";

    private static readonly IDataProtectionProvider _dataprotectorProvider = new EphemeralDataProtectionProvider();

    private readonly IServiceProvider _services = CreateDefaultServiceCollection().BuildServiceProvider();
    private readonly ComponentRenderer renderer;

    public ComponentRendererTest()
    {
        renderer = GetComponentRenderer();
    }

    [Fact]
    public async Task CanRender_ParameterlessComponent_ClientMode()
    {
        // Arrange
        var viewContext = GetViewContext();
        var writer = new StringWriter();

        // Act
        var result = await renderer.RenderComponentAsync(viewContext, typeof(TestComponent), RenderMode.WebAssembly, null);
        result.WriteTo(writer, HtmlEncoder.Default);
        var content = writer.ToString();
        var match = Regex.Match(content, ComponentPattern);

        // Assert
        Assert.True(match.Success);
        var marker = JsonSerializer.Deserialize<WebAssemblyComponentMarker>(match.Groups[1].Value, ServerComponentSerializationSettings.JsonSerializationOptions);
        Assert.Null(marker.PrerenderId);
        Assert.Equal("webassembly", marker.Type);
        Assert.Equal(typeof(TestComponent).Assembly.GetName().Name, marker.Assembly);
        Assert.Equal(typeof(TestComponent).FullName, marker.TypeName);
        Assert.Empty(viewContext.Items);
    }

    [Fact]
    public async Task CanPrerender_ParameterlessComponent_ClientMode()
    {
        // Arrange
        var viewContext = GetViewContext();
        var writer = new StringWriter();

        // Act
        var result = await renderer.RenderComponentAsync(viewContext, typeof(TestComponent), RenderMode.WebAssemblyPrerendered, null);
        result.WriteTo(writer, HtmlEncoder.Default);
        var content = writer.ToString();
        var match = Regex.Match(content, PrerenderedComponentPattern, RegexOptions.Multiline);

        // Assert
        Assert.True(match.Success);
        var preamble = match.Groups["preamble"].Value;
        var preambleMarker = JsonSerializer.Deserialize<WebAssemblyComponentMarker>(preamble, ServerComponentSerializationSettings.JsonSerializationOptions);
        Assert.NotNull(preambleMarker.PrerenderId);
        Assert.Equal("webassembly", preambleMarker.Type);
        Assert.Equal(typeof(TestComponent).Assembly.GetName().Name, preambleMarker.Assembly);
        Assert.Equal(typeof(TestComponent).FullName, preambleMarker.TypeName);

        var prerenderedContent = match.Groups["content"].Value;
        Assert.Equal("<h1>Hello world!</h1>", prerenderedContent);

        var epilogue = match.Groups["epilogue"].Value;
        var epilogueMarker = JsonSerializer.Deserialize<WebAssemblyComponentMarker>(epilogue, ServerComponentSerializationSettings.JsonSerializationOptions);
        Assert.Equal(preambleMarker.PrerenderId, epilogueMarker.PrerenderId);
        Assert.Null(epilogueMarker.Assembly);
        Assert.Null(epilogueMarker.TypeName);
        Assert.Null(epilogueMarker.Type);
        Assert.Null(epilogueMarker.ParameterDefinitions);
        Assert.Null(epilogueMarker.ParameterValues);
        var (_, mode) = Assert.Single(viewContext.Items);
        var invoked = Assert.IsType<InvokedRenderModes>(mode);
        Assert.Equal(InvokedRenderModes.Mode.WebAssembly, invoked.Value);
    }

    [Fact]
    public async Task CanRender_ComponentWithParameters_ClientMode()
    {
        // Arrange
        var viewContext = GetViewContext();
        var writer = new StringWriter();

        // Act
        var result = await renderer.RenderComponentAsync(viewContext, typeof(GreetingComponent),
            RenderMode.WebAssembly,
            new
            {
                Name = "Daniel"
            });
        result.WriteTo(writer, HtmlEncoder.Default);
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
        var viewContext = GetViewContext();
        var writer = new StringWriter();

        // Act
        var result = await renderer.RenderComponentAsync(viewContext, typeof(GreetingComponent),
            RenderMode.WebAssembly,
            new
            {
                Name = (string)null
            });
        result.WriteTo(writer, HtmlEncoder.Default);
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
        var viewContext = GetViewContext();
        var writer = new StringWriter();

        // Act
        var result = await renderer.RenderComponentAsync(viewContext, typeof(GreetingComponent),
            RenderMode.WebAssemblyPrerendered,
            new
            {
                Name = "Daniel"
            });
        result.WriteTo(writer, HtmlEncoder.Default);
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
        var viewContext = GetViewContext();
        var writer = new StringWriter();

        // Act
        var result = await renderer.RenderComponentAsync(viewContext, typeof(GreetingComponent),
            RenderMode.WebAssemblyPrerendered,
            new
            {
                Name = (string)null
            });
        result.WriteTo(writer, HtmlEncoder.Default);
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
        var viewContext = GetViewContext();
        var writer = new StringWriter();

        // Act
        var result = await renderer.RenderComponentAsync(viewContext, typeof(TestComponent), RenderMode.Static, null);
        result.WriteTo(writer, HtmlEncoder.Default);
        var content = writer.ToString();

        // Assert
        Assert.Equal("<h1>Hello world!</h1>", content);
    }

    [Fact]
    public async Task CanRender_ParameterlessComponent_ServerMode()
    {
        // Arrange
        var viewContext = GetViewContext();
        var protector = _dataprotectorProvider.CreateProtector(ServerComponentSerializationSettings.DataProtectionProviderPurpose)
            .ToTimeLimitedDataProtector();

        // Act
        var result = await renderer.RenderComponentAsync(viewContext, typeof(TestComponent), RenderMode.Server, null);
        var content = HtmlContentUtilities.HtmlContentToString(result, HtmlEncoder.Default);
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
        Assert.Equal(typeof(TestComponent).Assembly.GetName().Name, serverComponent.AssemblyName);
        Assert.Equal(typeof(TestComponent).FullName, serverComponent.TypeName);
        Assert.NotEqual(Guid.Empty, serverComponent.InvocationId);

        Assert.Equal("no-cache, no-store, max-age=0", viewContext.HttpContext.Response.Headers.CacheControl);
        Assert.DoesNotContain(viewContext.Items.Values, value => value is InvokedRenderModes);
    }

    [Fact]
    public async Task CanPrerender_ParameterlessComponent_ServerMode()
    {
        // Arrange
        var viewContext = GetViewContext();
        var protector = _dataprotectorProvider.CreateProtector(ServerComponentSerializationSettings.DataProtectionProviderPurpose)
            .ToTimeLimitedDataProtector();

        // Act
        var result = await renderer.RenderComponentAsync(viewContext, typeof(TestComponent), RenderMode.ServerPrerendered, null);
        var content = HtmlContentUtilities.HtmlContentToString(result, HtmlEncoder.Default);
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
        Assert.Equal(typeof(TestComponent).Assembly.GetName().Name, serverComponent.AssemblyName);
        Assert.Equal(typeof(TestComponent).FullName, serverComponent.TypeName);
        Assert.NotEqual(Guid.Empty, serverComponent.InvocationId);

        var prerenderedContent = match.Groups["content"].Value;
        Assert.Equal("<h1>Hello world!</h1>", prerenderedContent);

        var epilogue = match.Groups["epilogue"].Value;
        var epilogueMarker = JsonSerializer.Deserialize<ServerComponentMarker>(epilogue, ServerComponentSerializationSettings.JsonSerializationOptions);
        Assert.Equal(preambleMarker.PrerenderId, epilogueMarker.PrerenderId);
        Assert.Null(epilogueMarker.Sequence);
        Assert.Null(epilogueMarker.Descriptor);
        Assert.Null(epilogueMarker.Type);

        Assert.Equal("no-cache, no-store, max-age=0", viewContext.HttpContext.Response.Headers.CacheControl);
        var (_, mode) = Assert.Single(viewContext.Items, (kvp) => kvp.Value is InvokedRenderModes);
        Assert.Equal(InvokedRenderModes.Mode.Server, ((InvokedRenderModes)mode).Value);
    }

    [Fact]
    public async Task Prerender_ServerAndClientComponentUpdatesInvokedPrerenderModes()
    {
        // Arrange
        var viewContext = GetViewContext();

        // Act
        var server = await renderer.RenderComponentAsync(viewContext, typeof(GreetingComponent), RenderMode.ServerPrerendered, new { Name = "Steve" });
        var client = await renderer.RenderComponentAsync(viewContext, typeof(GreetingComponent), RenderMode.WebAssemblyPrerendered, new { Name = "Steve" });

        // Assert
        var (_, mode) = Assert.Single(viewContext.Items, (kvp) => kvp.Value is InvokedRenderModes);
        Assert.Equal(InvokedRenderModes.Mode.ServerAndWebAssembly, ((InvokedRenderModes)mode).Value);
    }

    [Fact]
    public async Task CanRenderMultipleServerComponents()
    {
        // Arrange
        var viewContext = GetViewContext();
        var protector = _dataprotectorProvider.CreateProtector(ServerComponentSerializationSettings.DataProtectionProviderPurpose)
            .ToTimeLimitedDataProtector();

        // Act
        var firstResult = await renderer.RenderComponentAsync(viewContext, typeof(TestComponent), RenderMode.ServerPrerendered, null);
        var firstComponent = HtmlContentUtilities.HtmlContentToString(firstResult);
        var firstMatch = Regex.Match(firstComponent, PrerenderedComponentPattern, RegexOptions.Multiline);

        var secondResult = await renderer.RenderComponentAsync(viewContext, typeof(TestComponent), RenderMode.Server, null);
        var secondComponent = HtmlContentUtilities.HtmlContentToString(secondResult);
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
        var viewContext = GetViewContext();

        // Act
        var result = await renderer.RenderComponentAsync(viewContext, typeof(GreetingComponent), RenderMode.Static, new { Name = "Steve" });

        // Assert
        var content = HtmlContentUtilities.HtmlContentToString(result, HtmlEncoder.Default);
        Assert.Equal("<p>Hello Steve!</p>", content);
    }

    [Fact]
    public async Task CanRender_ComponentWithParameters_ServerMode()
    {
        // Arrange
        var viewContext = GetViewContext();
        var protector = _dataprotectorProvider.CreateProtector(ServerComponentSerializationSettings.DataProtectionProviderPurpose)
            .ToTimeLimitedDataProtector();

        // Act
        var result = await renderer.RenderComponentAsync(viewContext, typeof(GreetingComponent), RenderMode.Server, new { Name = "Daniel" });
        var content = HtmlContentUtilities.HtmlContentToString(result, HtmlEncoder.Default);
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
        Assert.Equal("Daniel", rawValue.GetString());
    }

    [Fact]
    public async Task CanRender_ComponentWithNullParameters_ServerMode()
    {
        // Arrange
        var viewContext = GetViewContext();
        var protector = _dataprotectorProvider.CreateProtector(ServerComponentSerializationSettings.DataProtectionProviderPurpose)
            .ToTimeLimitedDataProtector();

        // Act

        var result = await renderer.RenderComponentAsync(viewContext, typeof(GreetingComponent), RenderMode.Server, new { Name = (string)null });
        var content = HtmlContentUtilities.HtmlContentToString(result, HtmlEncoder.Default);
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

        var value = Assert.Single(serverComponent.ParameterValues); ;
        Assert.Null(value);
    }

    [Fact]
    public async Task CanPrerender_ComponentWithParameters_ServerPrerenderedMode()
    {
        // Arrange
        var viewContext = GetViewContext();
        var writer = new StringWriter();
        var protector = _dataprotectorProvider.CreateProtector(ServerComponentSerializationSettings.DataProtectionProviderPurpose)
            .ToTimeLimitedDataProtector();

        // Act
        var result = await renderer.RenderComponentAsync(viewContext, typeof(GreetingComponent), RenderMode.ServerPrerendered, new { Name = "Daniel" });
        var content = HtmlContentUtilities.HtmlContentToString(result, HtmlEncoder.Default);
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
        Assert.Equal("Daniel", rawValue.GetString());

        var prerenderedContent = match.Groups["content"].Value;
        Assert.Equal("<p>Hello Daniel!</p>", prerenderedContent);

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
        var viewContext = GetViewContext();
        var writer = new StringWriter();
        var protector = _dataprotectorProvider.CreateProtector(ServerComponentSerializationSettings.DataProtectionProviderPurpose)
            .ToTimeLimitedDataProtector();

        // Act
        var result = await renderer.RenderComponentAsync(viewContext, typeof(GreetingComponent), RenderMode.ServerPrerendered, new { Name = (string)null });
        var content = HtmlContentUtilities.HtmlContentToString(result, HtmlEncoder.Default);
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
        var viewContext = GetViewContext();

        // Act & Assert
        var ex = await ExceptionAssert.ThrowsArgumentAsync(
            async () => await renderer.RenderComponentAsync(viewContext, typeof(GreetingComponent), default, new { Name = "Daniel" }),
            "renderMode",
            $"Unsupported RenderMode '{(RenderMode)default}'");
    }

    [Fact]
    public async Task RenderComponent_DoesNotInvokeOnAfterRenderInComponent()
    {
        // Arrange
        var viewContext = GetViewContext();

        // Act
        var state = new OnAfterRenderState();
        var result = await renderer.RenderComponentAsync(viewContext, typeof(OnAfterRenderComponent), RenderMode.Static, new { state });

        // Assert
        var content = HtmlContentUtilities.HtmlContentToString(result, HtmlEncoder.Default);
        Assert.Equal("<p>Hello</p>", content);
        Assert.False(state.OnAfterRenderRan);
    }

    [Fact]
    public async Task DisposableComponents_GetDisposedAfterScopeCompletes()
    {
        // Arrange
        var collection = CreateDefaultServiceCollection();
        collection.TryAddScoped<ComponentRenderer>();
        collection.TryAddScoped<StaticComponentRenderer>();
        collection.TryAddScoped<HtmlRenderer>();
        collection.TryAddSingleton(HtmlEncoder.Default);
        collection.TryAddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        collection.TryAddSingleton<ServerComponentSerializer>();
        collection.TryAddSingleton(_dataprotectorProvider);
        collection.TryAddSingleton<WebAssemblyComponentSerializer>();
        collection.TryAddScoped<IViewBufferScope, TestViewBufferScope>();

        var provider = collection.BuildServiceProvider();
        var scope = provider.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var scopedProvider = scope.ServiceProvider;
        var context = new DefaultHttpContext() { RequestServices = scopedProvider };
        var viewContext = GetViewContext(context);
        var renderer = scopedProvider.GetRequiredService<ComponentRenderer>();

        // Act
        var state = new AsyncDisposableState();
        var result = await renderer.RenderComponentAsync(viewContext, typeof(AsyncDisposableComponent), RenderMode.Static, new { state });

        // Assert
        var content = HtmlContentUtilities.HtmlContentToString(result, HtmlEncoder.Default);
        Assert.Equal("<p>Hello</p>", content);
        await ((IAsyncDisposable)scope).DisposeAsync();

        Assert.True(state.AsyncDisposableRan);
    }

    [Fact]
    public async Task CanCatch_ComponentWithSynchronousException()
    {
        // Arrange
        var viewContext = GetViewContext();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await renderer.RenderComponentAsync(
            viewContext,
            typeof(ExceptionComponent),
            RenderMode.Static,
            new
            {
                IsAsync = false
            }));

        // Assert
        Assert.Equal("Threw an exception synchronously", exception.Message);
    }

    [Fact]
    public async Task CanCatch_ComponentWithAsynchronousException()
    {
        // Arrange
        var viewContext = GetViewContext();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await renderer.RenderComponentAsync(
            viewContext,
            typeof(ExceptionComponent),
            RenderMode.Static,
            new
            {
                IsAsync = true
            }));

        // Assert
        Assert.Equal("Threw an exception asynchronously", exception.Message);
    }

    [Fact]
    public async Task Rendering_ComponentWithJsInteropThrows()
    {
        // Arrange
        var viewContext = GetViewContext();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await renderer.RenderComponentAsync(
            viewContext,
            typeof(ExceptionComponent),
            RenderMode.Static,
            new
            {
                JsInterop = true
            }
        ));

        // Assert
        Assert.Equal("JavaScript interop calls cannot be issued during server-side prerendering, " +
                "because the page has not yet loaded in the browser. Prerendered components must wrap any JavaScript " +
                "interop calls in conditional logic to ensure those interop calls are not attempted during prerendering.",
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
        var viewContext = GetViewContext(ctx);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await renderer.RenderComponentAsync(
            viewContext,
            typeof(RedirectComponent),
            RenderMode.Static,
            new
            {
                RedirectUri = "http://localhost/redirect"
            }));

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
        var viewContext = GetViewContext(ctx);

        // Act
        await renderer.RenderComponentAsync(
            viewContext,
            typeof(RedirectComponent),
            RenderMode.Static,
            new
            {
                RedirectUri = "http://localhost/redirect"
            });

        // Assert
        Assert.Equal(302, ctx.Response.StatusCode);
        Assert.Equal("http://localhost/redirect", ctx.Response.Headers.Location);
    }

    [Fact]
    public async Task CanRender_AsyncComponent()
    {
        // Arrange
        var viewContext = GetViewContext();
        var expectedContent = @"<table>
<thead>
<tr>
<th>Date</th>
<th>Summary</th>
<th>F</th>
<th>C</th>
</tr>
</thead>
<tbody>
<tr>
<td>06/05/2018</td>
<td>Freezing</td>
<td>33</td>
<td>33</td>
</tr>
<tr>
<td>07/05/2018</td>
<td>Bracing</td>
<td>57</td>
<td>57</td>
</tr>
<tr>
<td>08/05/2018</td>
<td>Freezing</td>
<td>9</td>
<td>9</td>
</tr>
<tr>
<td>09/05/2018</td>
<td>Balmy</td>
<td>4</td>
<td>4</td>
</tr>
<tr>
<td>10/05/2018</td>
<td>Chilly</td>
<td>29</td>
<td>29</td>
</tr>
</tbody>
</table>";

        // Act
        var result = await renderer.RenderComponentAsync(viewContext, typeof(AsyncComponent), RenderMode.Static, null);
        var content = HtmlContentUtilities.HtmlContentToString(result, HtmlEncoder.Default);

        // Assert
        Assert.Equal(expectedContent.Replace("\r\n", "\n"), content);
    }

    private ComponentRenderer GetComponentRenderer(IServiceProvider services = null)
    {
        var viewBufferScope = new TestViewBufferScope();
        return new ComponentRenderer(
            new StaticComponentRenderer(new HtmlRenderer(services ?? _services, NullLoggerFactory.Instance, viewBufferScope)),
            new ServerComponentSerializer(_dataprotectorProvider),
            viewBufferScope);
    }

    private ViewContext GetViewContext(HttpContext context = null)
    {
        context ??= new DefaultHttpContext();
        context.RequestServices ??= _services;
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("localhost");
        context.Request.PathBase = "/base";
        context.Request.Path = "/path";
        context.Request.QueryString = QueryString.FromUriComponent("?query=value");

        return new ViewContext { HttpContext = context };
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
        return services;
    }

    private class TestComponent : IComponent
    {
        private RenderHandle _renderHandle;

        public void Attach(RenderHandle renderHandle)
        {
            _renderHandle = renderHandle;
        }

        public Task SetParametersAsync(ParameterView parameters)
        {
            _renderHandle.Render(builder =>
            {
                var s = 0;
                builder.OpenElement(s++, "h1");
                builder.AddContent(s++, "Hello world!");
                builder.CloseElement();
            });
            return Task.CompletedTask;
        }
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

    private class GreetingComponent : ComponentBase
    {
        [Parameter] public string Name { get; set; }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var s = 0;
            base.BuildRenderTree(builder);
            builder.OpenElement(s++, "p");
            builder.AddContent(s++, $"Hello {Name ?? ("(null)")}!");
            builder.CloseElement();
        }
    }

    private class AsyncComponent : ComponentBase
    {
        private static readonly WeatherRow[] _weatherData = new[]
        {
                new WeatherRow
                {
                    DateFormatted = "06/05/2018",
                    TemperatureC = 1,
                    Summary = "Freezing",
                    TemperatureF = 33
                },
                new WeatherRow
                {
                    DateFormatted = "07/05/2018",
                    TemperatureC = 14,
                    Summary = "Bracing",
                    TemperatureF = 57
                },
                new WeatherRow
                {
                    DateFormatted = "08/05/2018",
                    TemperatureC = -13,
                    Summary = "Freezing",
                    TemperatureF = 9
                },
                new WeatherRow
                {
                    DateFormatted = "09/05/2018",
                    TemperatureC = -16,
                    Summary = "Balmy",
                    TemperatureF = 4
                },
                new WeatherRow
                {
                    DateFormatted = "10/05/2018",
                    TemperatureC = 2,
                    Summary = "Chilly",
                    TemperatureF = 29
                }
            };

        public class WeatherRow
        {
            public string DateFormatted { get; set; }
            public int TemperatureC { get; set; }
            public string Summary { get; set; }
            public int TemperatureF { get; set; }
        }

        public WeatherRow[] RowsToDisplay { get; set; }

        protected override async Task OnParametersSetAsync()
        {
            // Simulate an async workflow.
            await Task.Yield();
            RowsToDisplay = _weatherData;
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            base.BuildRenderTree(builder);
            var s = 0;
            builder.OpenElement(s++, "table");
            builder.AddMarkupContent(s++, "\n");
            builder.OpenElement(s++, "thead");
            builder.AddMarkupContent(s++, "\n");
            builder.OpenElement(s++, "tr");
            builder.AddMarkupContent(s++, "\n");

            builder.OpenElement(s++, "th");
            builder.AddContent(s++, "Date");
            builder.CloseElement();
            builder.AddMarkupContent(s++, "\n");

            builder.OpenElement(s++, "th");
            builder.AddContent(s++, "Summary");
            builder.CloseElement();
            builder.AddMarkupContent(s++, "\n");

            builder.OpenElement(s++, "th");
            builder.AddContent(s++, "F");
            builder.CloseElement();
            builder.AddMarkupContent(s++, "\n");

            builder.OpenElement(s++, "th");
            builder.AddContent(s++, "C");
            builder.CloseElement();
            builder.AddMarkupContent(s++, "\n");

            builder.CloseElement();
            builder.AddMarkupContent(s++, "\n");
            builder.CloseElement();
            builder.AddMarkupContent(s++, "\n");
            builder.OpenElement(s++, "tbody");
            builder.AddMarkupContent(s++, "\n");
            if (RowsToDisplay != null)
            {
                var s2 = s;
                foreach (var element in RowsToDisplay)
                {
                    s = s2;
                    builder.OpenElement(s++, "tr");
                    builder.AddMarkupContent(s++, "\n");

                    builder.OpenElement(s++, "td");
                    builder.AddContent(s++, element.DateFormatted);
                    builder.CloseElement();
                    builder.AddMarkupContent(s++, "\n");

                    builder.OpenElement(s++, "td");
                    builder.AddContent(s++, element.Summary);
                    builder.CloseElement();
                    builder.AddMarkupContent(s++, "\n");

                    builder.OpenElement(s++, "td");
                    builder.AddContent(s++, element.TemperatureF);
                    builder.CloseElement();
                    builder.AddMarkupContent(s++, "\n");

                    builder.OpenElement(s++, "td");
                    builder.AddContent(s++, element.TemperatureF);
                    builder.CloseElement();
                    builder.AddMarkupContent(s++, "\n");

                    builder.CloseElement();
                    builder.AddMarkupContent(s++, "\n");
                }
            }

            builder.CloseElement();
            builder.AddMarkupContent(s++, "\n");

            builder.CloseElement();
        }
    }
}
