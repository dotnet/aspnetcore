// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

public class RazorComponentResultTest
{
    [Fact]
    public async Task CanRenderComponentStatically()
    {
        // Arrange
        var result = new RazorComponentResult<SimpleComponent>();
        var httpContext = GetTestHttpContext();
        var responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;
        var actionContext = new ActionContext(httpContext, new AspNetCore.Routing.RouteData(), new Abstractions.ActionDescriptor());

        // Act
        await result.ExecuteResultAsync(actionContext);

        // Assert
        responseBody.Position = 0;
        var responseHtml = new StreamReader(responseBody).ReadToEnd();
        Assert.Equal("<h1>Hello from SimpleComponent</h1>", responseHtml);
        Assert.Equal("text/html; charset=utf-8", httpContext.Response.ContentType);
        Assert.Equal(200, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task CanSetStatusCodeAndContentType()
    {
        // Arrange
        var result = new RazorComponentResult<SimpleComponent>
        {
            StatusCode = 123,
            ContentType = "application/test-content-type",
        };
        var httpContext = GetTestHttpContext();
        var responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;
        var actionContext = new ActionContext(httpContext, new AspNetCore.Routing.RouteData(), new Abstractions.ActionDescriptor());

        // Act
        await result.ExecuteResultAsync(actionContext);

        // Assert
        responseBody.Position = 0;
        var responseHtml = new StreamReader(responseBody).ReadToEnd();
        Assert.Equal("<h1>Hello from SimpleComponent</h1>", responseHtml);
        Assert.Equal("application/test-content-type", httpContext.Response.ContentType);
        Assert.Equal(123, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task WaitsForQuiescence()
    {
        // Arrange
        var tcs = new TaskCompletionSource();
        var result = new RazorComponentResult<AsyncLoadingComponent>
        {
            Parameters = new { LoadingTask = tcs.Task }
        };
        var httpContext = GetTestHttpContext();
        var responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;
        var actionContext = new ActionContext(httpContext, new AspNetCore.Routing.RouteData(), new Abstractions.ActionDescriptor());

        // Act/Assert: Doesn't complete until loading finishes
        var completionTask = result.ExecuteResultAsync(actionContext);
        await Task.Yield();
        Assert.False(completionTask.IsCompleted);

        // Act/Assert: Does complete when loading finishes
        tcs.SetResult();
        await completionTask;
        responseBody.Position = 0;
        var responseHtml = new StreamReader(responseBody).ReadToEnd();
        Assert.Equal("Loading task status: RanToCompletion", responseHtml);
    }

    private static DefaultHttpContext GetTestHttpContext()
    {
        var serviceCollection = new ServiceCollection()
            .AddSingleton(new DiagnosticListener("test"))
            .AddSingleton<IHttpResponseStreamWriterFactory, TestHttpResponseStreamWriterFactory>()
            .AddSingleton<RazorComponentResultExecutor>()
            .AddSingleton<ComponentPrerenderer>()
            .AddSingleton<NavigationManager, FakeNavigationManager>()
            .AddSingleton<ServerComponentSerializer>()
            .AddSingleton<ComponentStatePersistenceManager>()
            .AddSingleton<HtmlRenderer>()
            .AddSingleton<IDataProtectionProvider, FakeDataProtectionProvider>()
            .AddLogging();
        return new DefaultHttpContext { RequestServices = serviceCollection.BuildServiceProvider() };
    }

    class SimpleComponent : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "h1");
            builder.AddContent(1, "Hello from SimpleComponent");
            builder.CloseElement();
        }
    }

    class AsyncLoadingComponent : ComponentBase
    {
        [Parameter] public Task LoadingTask { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await LoadingTask;
            await Task.Delay(100); // Doesn't strictly make any difference to the test, but clarifies that arbitrary async stuff could happen here
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
            => builder.AddContent(0, $"Loading task status: {LoadingTask.Status}");
    }

    class FakeDataProtectionProvider : IDataProtectionProvider
    {
        public IDataProtector CreateProtector(string purpose)
            => new FakeDataProtector();

        class FakeDataProtector : IDataProtector
        {
            public IDataProtector CreateProtector(string purpose) => throw new NotImplementedException();
            public byte[] Protect(byte[] plaintext) => throw new NotImplementedException();
            public byte[] Unprotect(byte[] protectedData) => throw new NotImplementedException();
        }
    }

    class FakeNavigationManager : NavigationManager, IHostEnvironmentNavigationManager
    {
        public new void Initialize(string baseUri, string uri) { }
    }
}
