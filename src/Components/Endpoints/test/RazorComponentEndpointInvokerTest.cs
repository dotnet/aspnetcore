// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Components.Endpoints.Tests;
public class RazorComponentEndpointInvokerTest
{
    [Fact]
    public async Task Invoker_RedirectWhileInitializingChildren_DoesNotWriteUninitializedComponentFrames()
    {
        var services = CreateRedirectingPageServices();
        await using var renderer = new EndpointHtmlRenderer(services, NullLoggerFactory.Instance);
        var invoker = new RazorComponentEndpointInvoker(renderer, NullLogger<RazorComponentEndpointInvoker>.Instance);

        var context = CreateRedirectingPageContext(services, typeof(RedirectingPage));
        context.Request.Method = "GET";

        await invoker.Render(context);

        // The redirect abandons the diff after the new render tree was already swapped in, leaving the sibling
        // that follows the redirecting child uninitialized. The abandoned output must be discarded rather than
        // written, because that sibling still carries the default component id and walking it re-enters the root.
        Assert.Equal(StatusCodes.Status302Found, context.Response.StatusCode);
        Assert.Equal("https://localhost/redirected", context.Response.Headers.Location);
        Assert.Empty(await ReadBody(context));
    }

    [Fact]
    public async Task Invoker_RedirectWhileHandlingFormPost_DoesNotWriteUninitializedComponentFrames()
    {
        var services = CreateRedirectingPageServices();
        await using var renderer = new EndpointHtmlRenderer(services, NullLoggerFactory.Instance);
        var invoker = new RazorComponentEndpointInvoker(renderer, NullLogger<RazorComponentEndpointInvoker>.Instance);

        var context = CreateRedirectingPageContext(services, typeof(RedirectingFormPage));
        context.Request.Method = "POST";
        context.Request.ContentType = "application/x-www-form-urlencoded";
        var form = Encoding.UTF8.GetBytes($"_handler={RedirectingFormPage.FormName}");
        context.Request.Body = new MemoryStream(form);
        context.Request.ContentLength = form.Length;

        await invoker.Render(context);

        // Post/Redirect/Get: handling the submit event re-renders the page, and a child added by that render
        // redirects while initializing, so the same abandoned tree has to be discarded on the form post path.
        Assert.Equal(StatusCodes.Status302Found, context.Response.StatusCode);
        Assert.Equal("https://localhost/redirected", context.Response.Headers.Location);
        Assert.Empty(await ReadBody(context));
    }

    private static ServiceProvider CreateRedirectingPageServices()
        => new ServiceCollection().AddRazorComponents()
            .Services.AddAntiforgery()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
            .AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment())
            .AddSingleton<IHostEnvironment>(sp => sp.GetRequiredService<IWebHostEnvironment>())
            .AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider())
            .BuildServiceProvider();

    private static DefaultHttpContext CreateRedirectingPageContext(
        IServiceProvider services,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type pageType)
    {
        var context = new DefaultHttpContext { RequestServices = services };
        context.SetEndpoint(new RouteEndpoint(
            _ => Task.CompletedTask,
            RoutePatternFactory.Parse("/"),
            0,
            new EndpointMetadataCollection(
                new ComponentTypeMetadata(pageType),
                new RootComponentMetadata(pageType),
                new ConfiguredRenderModesMetadata([])),
            "redirecting-page"));
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("localhost");
        context.Request.Path = "/";
        context.Response.Body = new MemoryStream();
        return context;
    }

    private sealed class RedirectingPage : ComponentBase
    {
        private bool _loaded;

        protected override async Task OnInitializedAsync()
        {
            // Yield so the children below are added by a later re-render. A redirect during the initial
            // synchronous render propagates out of BeginRenderingComponent instead of abandoning a diff.
            await Task.Yield();
            _loaded = true;
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (_loaded)
            {
                AddRedirectingChildren(builder, sequence: 0);
            }
        }
    }

    private sealed class RedirectingFormPage : ComponentBase
    {
        public const string FormName = "submit-form";

        private bool _submitted;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "form");
            builder.AddAttribute(1, "method", "post");
            builder.AddAttribute(2, "onsubmit", EventCallback.Factory.Create(this, () => { _submitted = true; }));
            builder.AddNamedEvent("onsubmit", FormName);
            builder.CloseElement();

            if (_submitted)
            {
                AddRedirectingChildren(builder, sequence: 3);
            }
        }
    }

    private static void AddRedirectingChildren(RenderTreeBuilder builder, int sequence)
    {
        builder.OpenComponent<RedirectingChild>(sequence);
        builder.CloseComponent();
        builder.OpenComponent<UninitializedSibling>(sequence + 1);
        builder.CloseComponent();
    }

    private sealed class RedirectingChild : ComponentBase
    {
        [Inject] public NavigationManager Navigation { get; set; } = default!;

        protected override void OnInitialized()
            => Navigation.NavigateTo("/redirected");
    }

    private sealed class UninitializedSibling : ComponentBase;

    private static async Task<string> ReadBody(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }

    [Fact]
    public async Task Invoker_RejectsPostRequestsWithNonFormDataContentTypesAsync()
    {
        // Arrange
        var services = new ServiceCollection().AddRazorComponents()
                        .Services.AddAntiforgery()
                        .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
                        .AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment())
                        .BuildServiceProvider();

        var invoker = new RazorComponentEndpointInvoker(
            new EndpointHtmlRenderer(
                services,
                NullLoggerFactory.Instance),
            NullLogger<RazorComponentEndpointInvoker>.Instance);

        var context = new DefaultHttpContext();
        context.SetEndpoint(new RouteEndpoint(
            ctx => Task.CompletedTask,
            RoutePatternFactory.Parse("/"),
            0,
            new EndpointMetadataCollection(
                // These don't matter
                new ComponentTypeMetadata(typeof(AuthorizeView)),
                new RootComponentMetadata(typeof(AuthorizeView))),
            "test"));
        context.Request.Method = "POST";
        context.Request.ContentType = "application/json";
        context.RequestServices = services;

        // Act
        await invoker.Render(context);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    private class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IFileProvider WebRootFileProvider { get => new NullFileProvider(); set => throw new NotImplementedException(); }
        public string EnvironmentName { get => "Development"; set => throw new NotImplementedException(); }
        public string ApplicationName { get => "Test"; set => throw new NotImplementedException(); }
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}
