// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Endpoints.Tests.TestComponents;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Microsoft.AspNetCore.Components.Endpoints.Tests;
public class RazorComponentEndpointInvokerTest
{
    [ConditionalTheory]
    [RemoteExecutionSupported]
    [InlineData("navigate", false, false)]
    [InlineData("navigate", false, true)]
    [InlineData("navigate", true, false)]
    [InlineData("navigate", true, true)]
    [InlineData("throw", false, false)]
    [InlineData("throw", false, true)]
    [InlineData("throw", true, false)]
    [InlineData("throw", true, true)]
    [InlineData("not-found", false, false)]
    [InlineData("not-found", false, true)]
    [InlineData("not-found", true, false)]
    [InlineData("not-found", true, true)]
    public void Invoker_StreamingResponseDuringFlush_WritesTerminalTemplateAfterHtml(string operation, bool enhanced, bool isPost)
    {
        var options = new RemoteInvokeOptions();
        options.RuntimeConfigurationOptions.Add(
            "Microsoft.AspNetCore.Components.Endpoints.NavigationManager.DisableThrowNavigationException",
            (operation != "throw").ToString().ToLowerInvariant());

        using var remoteHandle = RemoteExecutor.Invoke(static async (operation, enhancedString, isPostString) =>
        {
            var enhanced = bool.Parse(enhancedString);
            var state = new StreamingNavigationState(operation, bool.Parse(isPostString));
            await using var services = new ServiceCollection().AddRazorComponents()
                .Services.AddAntiforgery()
                .AddSingleton(state)
                .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
                .AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment())
                .AddSingleton<IHostEnvironment>(sp => sp.GetRequiredService<IWebHostEnvironment>())
                .AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider())
                .BuildServiceProvider();
            await using var renderer = new EndpointHtmlRenderer(services, NullLoggerFactory.Instance);
            var invoker = new RazorComponentEndpointInvoker(renderer, NullLogger<RazorComponentEndpointInvoker>.Instance);
            var context = CreateRedirectingPageContext(services, typeof(StreamingNavigationPage));
            context.Request.Method = state.IsPost ? "POST" : "GET";
            if (enhanced)
            {
                context.Request.Headers.Accept = "text/html; blazor-enhanced-nav=on";
            }
            if (state.IsPost)
            {
                context.Request.ContentType = "application/x-www-form-urlencoded";
                var form = Encoding.UTF8.GetBytes("_handler=submit-form");
                context.Request.Body = new MemoryStream(form);
                context.Request.ContentLength = form.Length;
            }
            context.Items[nameof(StatusCodePagesOptions)] = "/not-found";

            await using var responseStream = new GatedResponseStream(state);
            context.Response.Body = responseStream;
            var responseFeature = new Mock<IHttpResponseFeature>();
            responseFeature.SetupProperty(feature => feature.StatusCode, StatusCodes.Status200OK);
            responseFeature.Setup(feature => feature.Headers).Returns(context.Response.Headers);
            responseFeature.Setup(feature => feature.HasStarted).Returns(() => responseStream.HasStarted);
            context.Features.Set(responseFeature.Object);

            var renderTask = invoker.Render(context);
            try
            {
                await state.WriteStarted.Task.WaitAsync(TimeSpan.FromSeconds(30));
                Assert.True(context.Response.HasStarted);
                await renderer.Dispatcher.InvokeAsync(() => state.ContinueRendering.SetResult());
                await state.NavigationRequested.Task.WaitAsync(TimeSpan.FromSeconds(30));
            }
            finally
            {
                state.ReleaseWrite.TrySetResult();
            }
            await renderTask.WaitAsync(TimeSpan.FromSeconds(30));

            var body = Encoding.UTF8.GetString(responseStream.ToArray());
            Assert.Contains($"<p>{state.Content}</p>", body);
            var templateType = operation == "not-found" ? "not-found" : "redirection";
            var template = $"<blazor-ssr><template type=\"{templateType}\"";
            var templateIndex = body.IndexOf(template, StringComparison.Ordinal);
            Assert.True(templateIndex > body.LastIndexOf("</html>", StringComparison.Ordinal),
                "The terminal template must follow the complete HTML document.");
            Assert.Equal(templateIndex, body.LastIndexOf(template, StringComparison.Ordinal));
            var attributes = (state.IsPost ? " from=\"form-post\"" : "")
                + (enhanced || operation == "not-found" ? " enhanced=\"true\"" : "");
            Assert.Contains($"{template}{attributes}>_framework/opaque-redirect?url=", body);
            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
            if (enhanced)
            {
                var framing = $"<!--{context.Response.Headers["ssr-framing"]}-->";
                Assert.True(body.LastIndexOf(framing, StringComparison.Ordinal) < templateIndex);
            }
        }, operation, enhanced.ToString(), isPost.ToString(), options);
    }

    private sealed class StreamingNavigationState(string operation, bool isPost)
    {
        public string Operation { get; } = operation;
        public bool IsPost { get; } = isPost;
        public string Content { get; } = new('x', 70000);
        public TaskCompletionSource WriteStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ContinueRendering { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource NavigationRequested { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseWrite { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    [StreamRendering]
    private sealed class StreamingNavigationPage : ComponentBase
    {
        [Inject] public NavigationManager Navigation { get; set; } = default!;
        [Inject] public StreamingNavigationState State { get; set; } = default!;

        protected override Task OnInitializedAsync()
            => State.IsPost ? Task.CompletedTask : NavigateAsync();

        private async Task NavigateAsync()
        {
            await State.ContinueRendering.Task;
            try
            {
                if (State.Operation == "not-found")
                {
                    Navigation.NotFound();
                }
                else
                {
                    Navigation.NavigateTo("/redirected");
                }
            }
            finally
            {
                State.NavigationRequested.SetResult();
            }
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "html");
            builder.OpenElement(1, "form");
            builder.AddAttribute(2, "method", "post");
            builder.AddAttribute(3, "onsubmit", EventCallback.Factory.Create(this, NavigateAsync));
            builder.AddNamedEvent("onsubmit", "submit-form");
            builder.CloseElement();
            builder.OpenElement(4, "p");
            builder.AddContent(5, State.Content);
            builder.CloseElement();
            builder.CloseElement();
        }
    }

    private sealed class GatedResponseStream(StreamingNavigationState state) : MemoryStream
    {
        public bool HasStarted { get; private set; }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var firstWrite = !HasStarted;
            HasStarted = true;
            await base.WriteAsync(buffer, cancellationToken);
            if (firstWrite)
            {
                state.WriteStarted.SetResult();
                await state.ReleaseWrite.Task;
            }
        }
    }

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

    // Never gets initialized: the preceding sibling redirects while the parent's children are being created.
    private sealed class UninitializedSibling : ComponentBase;

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

    [Fact]
    public async Task Invoker_HandlesHeadRequestAsync()
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
                new ComponentTypeMetadata(typeof(SimpleComponent)),
                new RootComponentMetadata(typeof(SimpleComponent)),
                new ConfiguredRenderModesMetadata(Array.Empty<IComponentRenderMode>())),
            "test"));
        context.Request.Method = "HEAD";
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("localhost");
        context.Request.Path = "/";
        context.Response.Body = new MemoryStream();
        context.RequestServices = services;

        // Act
        await invoker.Render(context);

        // Assert
        // HEAD requests should execute the full request like GET, returning 200 OK with headers.
        // The HTTP server (Kestrel) handles suppressing the response body for HEAD requests.
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal("text/html; charset=utf-8", context.Response.ContentType);
    }

    [Fact]
    public async Task Invoker_PostReturns400_WhenAntiforgeryValidationFeatureIsInvalid()
    {
        var services = new ServiceCollection().AddRazorComponents()
                        .Services.AddAntiforgery()
                        .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
                        .AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment())
                        .BuildServiceProvider();

        var invoker = new RazorComponentEndpointInvoker(
            new EndpointHtmlRenderer(services, NullLoggerFactory.Instance),
            NullLogger<RazorComponentEndpointInvoker>.Instance);

        var context = BuildPostContext(services, "name=alice");
        context.Features.Set<IAntiforgeryValidationFeature>(new InvalidAntiforgeryValidationFeature());

        await invoker.Render(context);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Contains("antiforgery token", await ReadBody(context), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Invoker_PostDoesNotFailAntiforgery_WhenAntiforgeryValidationFeatureIsValid()
    {
        var services = new ServiceCollection().AddRazorComponents()
                        .Services.AddAntiforgery()
                        .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
                        .AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment())
                        .BuildServiceProvider();

        var invoker = new RazorComponentEndpointInvoker(
            new EndpointHtmlRenderer(services, NullLoggerFactory.Instance),
            NullLogger<RazorComponentEndpointInvoker>.Instance);

        var context = BuildPostContext(services, "name=alice");
        context.Features.Set<IAntiforgeryValidationFeature>(new ValidAntiforgeryValidationFeature());

        await invoker.Render(context);

        Assert.DoesNotContain("antiforgery token", await ReadBody(context), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Invoker_PostDoesNotFailAntiforgery_WhenNoAntiforgeryValidationFeatureIsSet()
    {
        var services = new ServiceCollection().AddRazorComponents()
                        .Services.AddAntiforgery()
                        .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
                        .AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment())
                        .BuildServiceProvider();

        var invoker = new RazorComponentEndpointInvoker(
            new EndpointHtmlRenderer(services, NullLoggerFactory.Instance),
            NullLogger<RazorComponentEndpointInvoker>.Instance);

        var context = BuildPostContext(services, "name=alice");

        await invoker.Render(context);

        Assert.DoesNotContain("antiforgery token", await ReadBody(context), StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string> ReadBody(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }

    private static DefaultHttpContext BuildPostContext(IServiceProvider services, string formBody)
    {
        var context = new DefaultHttpContext();
        context.SetEndpoint(new RouteEndpoint(
            ctx => Task.CompletedTask,
            RoutePatternFactory.Parse("/"),
            0,
            new EndpointMetadataCollection(
                new ComponentTypeMetadata(typeof(SimpleComponent)),
                new RootComponentMetadata(typeof(SimpleComponent)),
                new ConfiguredRenderModesMetadata(Array.Empty<IComponentRenderMode>())),
            "test"));
        context.Request.Method = "POST";
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("localhost");
        context.Request.Path = "/";
        context.Request.ContentType = "application/x-www-form-urlencoded";
        var body = Encoding.UTF8.GetBytes(formBody);
        context.Request.Body = new MemoryStream(body);
        context.Request.ContentLength = body.Length;
        context.Response.Body = new MemoryStream();
        context.RequestServices = services;
        return context;
    }

    private sealed class InvalidAntiforgeryValidationFeature : IAntiforgeryValidationFeature
    {
        public bool IsValid => false;
        public Exception? Error { get; } = new AntiforgeryValidationException("invalid");
    }

    private sealed class ValidAntiforgeryValidationFeature : IAntiforgeryValidationFeature
    {
        public bool IsValid => true;
        public Exception? Error => null;
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
