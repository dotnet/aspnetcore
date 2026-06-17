// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RenderMode = Microsoft.AspNetCore.Components.Web.RenderMode;

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

public class PersistComponentStateTagHelperTest
{
    private static readonly IDataProtectionProvider _ephemeralProvider =
        new EphemeralDataProtectionProvider();
    private static readonly IDataProtector _protector =
        _ephemeralProvider.CreateProtector("Microsoft.AspNetCore.Components.Server.State");

    [Fact]
    public async Task ExecuteAsync_DoesNotPersistDataWhenNoPrerenderHappened()
    {
        // Arrange
        var tagHelper = new PersistComponentStateTagHelper
        {
            ViewContext = GetViewContext()
        };

        var context = GetTagHelperContext();
        var output = GetTagHelperOutput();

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        var content = HtmlContentUtilities.HtmlContentToString(output.Content);
        Assert.Empty(content);
        Assert.Null(output.TagName);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotRenderWebAssemblyStateWhenStateWasNotPersisted()
    {
        // Arrange
        var tagHelper = new PersistComponentStateTagHelper
        {
            ViewContext = GetViewContext(),
            PersistenceMode = PersistenceMode.WebAssembly
        };

        var context = GetTagHelperContext();
        var output = GetTagHelperOutput();

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        var content = HtmlContentUtilities.HtmlContentToString(output.Content);
        Assert.Empty(content);
        Assert.Null(output.TagName);
    }

    [Fact]
    public async Task ExecuteAsync_RendersWebAssemblyStateExplicitly()
    {
        // Arrange
        var tagHelper = new PersistComponentStateTagHelper
        {
            ViewContext = GetViewContext(),
            PersistenceMode = PersistenceMode.WebAssembly
        };

        var context = GetTagHelperContext();
        var output = GetTagHelperOutput();
        var manager = tagHelper.ViewContext.HttpContext.RequestServices.GetRequiredService<ComponentStatePersistenceManager>();

        // Act
        manager.State.RegisterOnPersisting(() =>
        {
            manager.State.PersistAsJson("state", "state value");
            return Task.CompletedTask;
        }, RenderMode.InteractiveWebAssembly);
        await tagHelper.ProcessAsync(context, output);

        // Assert
        var content = HtmlContentUtilities.HtmlContentToString(output.Content);
        Assert.Null(output.TagName);
        var message = content["<!--Blazor-WebAssembly-Component-State:".Length..^"-->".Length];
        Assert.True(message.Length > 0);
    }

    [Fact]
    public async Task ExecuteAsync_RendersWebAssemblyStateImplicitlyWhenAWebAssemblyComponentWasPrerendered()
    {
        // Arrange
        var tagHelper = new PersistComponentStateTagHelper
        {
            ViewContext = GetViewContext()
        };

        EndpointHtmlRenderer.UpdateSaveStateRenderMode(tagHelper.ViewContext.HttpContext, RenderMode.InteractiveWebAssembly);

        var context = GetTagHelperContext();
        var output = GetTagHelperOutput();
        var manager = tagHelper.ViewContext.HttpContext.RequestServices.GetRequiredService<ComponentStatePersistenceManager>();

        // Act
        manager.State.RegisterOnPersisting(() =>
        {
            manager.State.PersistAsJson("state", "state value");
            return Task.CompletedTask;
        }, RenderMode.InteractiveWebAssembly);
        await tagHelper.ProcessAsync(context, output);

        // Assert
        var content = HtmlContentUtilities.HtmlContentToString(output.Content);
        Assert.Null(output.TagName);
        var message = content["<!--Blazor-WebAssembly-Component-State:".Length..^"-->".Length];
        Assert.True(message.Length > 0);
    }

    [Fact]
    public async Task ExecuteAsync_RendersServerStateExplicitly()
    {
        // Arrange
        var tagHelper = new PersistComponentStateTagHelper
        {
            ViewContext = GetViewContext(),
            PersistenceMode = PersistenceMode.Server
        };

        var context = GetTagHelperContext();
        var output = GetTagHelperOutput();
        var manager = tagHelper.ViewContext.HttpContext.RequestServices.GetRequiredService<ComponentStatePersistenceManager>();

        // Act
        manager.State.RegisterOnPersisting(() =>
        {
            manager.State.PersistAsJson("state", "state value");
            return Task.CompletedTask;
        }, RenderMode.InteractiveServer);

        await tagHelper.ProcessAsync(context, output);

        // Assert
        var content = HtmlContentUtilities.HtmlContentToString(output.Content);
        Assert.NotEmpty(content);
        var payload = content["<!--Blazor-Server-Component-State:".Length..^"-->".Length];
        var message = _protector.Unprotect(payload);
        Assert.True(message.Length > 0);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotRenderServerStateWhenStateWasNotPersisted()
    {
        // Arrange
        var tagHelper = new PersistComponentStateTagHelper
        {
            ViewContext = GetViewContext(),
            PersistenceMode = PersistenceMode.Server
        };

        var context = GetTagHelperContext();
        var output = GetTagHelperOutput();

        // Act
        await tagHelper.ProcessAsync(context, output);

        // Assert
        var content = HtmlContentUtilities.HtmlContentToString(output.Content);
        Assert.Empty(content);
    }

    [Fact]
    public async Task ExecuteAsync_RendersServerStateImplicitlyWhenAServerComponentWasPrerendered()
    {
        // Arrange
        var tagHelper = new PersistComponentStateTagHelper
        {
            ViewContext = GetViewContext()
        };

        EndpointHtmlRenderer.UpdateSaveStateRenderMode(tagHelper.ViewContext.HttpContext, Components.Web.RenderMode.InteractiveServer);

        var context = GetTagHelperContext();
        var output = GetTagHelperOutput();
        var manager = tagHelper.ViewContext.HttpContext.RequestServices.GetRequiredService<ComponentStatePersistenceManager>();

        // Act
        manager.State.RegisterOnPersisting(() =>
        {
            manager.State.PersistAsJson("state", "state value");
            return Task.CompletedTask;
        }, RenderMode.InteractiveServer);

        await tagHelper.ProcessAsync(context, output);

        // Assert
        var content = HtmlContentUtilities.HtmlContentToString(output.Content);
        Assert.NotEmpty(content);
        var payload = content["<!--Blazor-Server-Component-State:".Length..^"-->".Length];
        var message = _protector.Unprotect(payload);
        Assert.True(message.Length > 0);
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsIfItCantInferThePersistMode()
    {
        // Arrange
        var tagHelper = new PersistComponentStateTagHelper
        {
            ViewContext = GetViewContext()
        };

        EndpointHtmlRenderer.UpdateSaveStateRenderMode(tagHelper.ViewContext.HttpContext, Components.Web.RenderMode.InteractiveServer);
        EndpointHtmlRenderer.UpdateSaveStateRenderMode(tagHelper.ViewContext.HttpContext, Components.Web.RenderMode.InteractiveWebAssembly);

        var context = GetTagHelperContext();
        var output = GetTagHelperOutput();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => tagHelper.ProcessAsync(context, output));
    }

    private static TagHelperContext GetTagHelperContext()
    {
        return new TagHelperContext(
            "persist-component-state",
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            Guid.NewGuid().ToString("N"));
    }

    private static TagHelperOutput GetTagHelperOutput()
    {
        return new TagHelperOutput(
            "persist-component-state",
            new TagHelperAttributeList(),
            (_, __) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
    }

    private ViewContext GetViewContext()
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection()
                .AddScoped(_ => Mock.Of<IDataProtectionProvider>(
                    x => x.CreateProtector(It.IsAny<string>()) == _protector))
                .AddLogging()
                .AddScoped<ComponentStatePersistenceManager>()
                .AddScoped<IComponentPrerenderer, EndpointHtmlRenderer>()
                .BuildServiceProvider(),
        };

        return new ViewContext { HttpContext = httpContext };
    }
}
