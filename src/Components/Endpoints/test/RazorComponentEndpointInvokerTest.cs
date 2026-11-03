// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Endpoints.Tests.TestComponents;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
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
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Invoker_CrossSiteFormFreePage_DoesNotRequireUserIdentifier(bool hasCookie, bool supportsInteractivity)
    {
        await using var services = CreateRedirectingPageServices();
        await using var renderer = new EndpointHtmlRenderer(services, NullLoggerFactory.Instance);
        var invoker = new RazorComponentEndpointInvoker(renderer, NullLogger<RazorComponentEndpointInvoker>.Instance);
        var context = CreateRedirectingPageContext(services, typeof(FormFreePage));
        context.Request.Method = "GET";
        context.Request.Headers["Sec-Fetch-Site"] = "cross-site";
        context.Items[MiddlewareInvokedKeys.Antiforgery] = true;
        if (supportsInteractivity)
        {
            context.SetEndpoint(new Endpoint(
                null,
                new EndpointMetadataCollection(
                    new ComponentTypeMetadata(typeof(FormFreePage)),
                    new RootComponentMetadata(typeof(FormFreePage)),
                    new ConfiguredRenderModesMetadata([RenderMode.InteractiveServer])),
                "form-free-page"));
        }
        if (hasCookie)
        {
            var cookieContext = new DefaultHttpContext { RequestServices = services };
            cookieContext.Request.Scheme = "https";
            services.GetRequiredService<IAntiforgery>().GetAndStoreTokens(cookieContext);
            context.Request.Headers.Cookie = cookieContext.Response.Headers.SetCookie.ToString().Split(';')[0];
        }
        context.User = new ClaimsPrincipal(new ClaimsIdentity(authenticationType: "test"));
        Assert.True(context.User.Identity!.IsAuthenticated);
        Assert.Null(context.User.Identity.Name);
        Assert.Empty(context.User.Claims);

        await invoker.Render(context);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Contains("Form-free page", await ReadBody(context));
        Assert.Equal(0, context.Response.Headers.SetCookie.Count);
        Assert.Null(services.GetRequiredService<AntiforgeryStateProvider>().GetAntiforgeryToken());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Invoker_CrossSiteForm_GeneratesTokensWhenRequired(bool hasUserIdentifier)
    {
        await using var services = CreateRedirectingPageServices();
        await using var renderer = new EndpointHtmlRenderer(services, NullLoggerFactory.Instance);
        var invoker = new RazorComponentEndpointInvoker(renderer, NullLogger<RazorComponentEndpointInvoker>.Instance);
        var context = CreateRedirectingPageContext(services, typeof(TokenPage));
        context.Request.Method = "GET";
        context.Request.Headers["Sec-Fetch-Site"] = "cross-site";
        context.Items[MiddlewareInvokedKeys.Antiforgery] = true;
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            hasUserIdentifier ? [new Claim(ClaimTypes.Name, "Test user")] : [],
            authenticationType: "test"));

        if (hasUserIdentifier)
        {
            await invoker.Render(context);

            Assert.Contains("__RequestVerificationToken", await ReadBody(context));
            Assert.NotEqual(0, context.Response.Headers.SetCookie.Count);
        }
        else
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => invoker.Render(context));
            Assert.Contains("IsAuthenticated", exception.Message);
        }
    }

    private sealed class FormFreePage : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
            => builder.AddContent(0, "Form-free page");
    }

    private sealed class TokenPage : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<AntiforgeryToken>(0);
            builder.CloseComponent();
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
