// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class RazorComponentResultTest
{
    [Fact]
    public void AcceptsNullParameters()
    {
        var result = new RazorComponentResult(typeof(SimpleComponent), null);
        Assert.NotNull(result.Parameters);
        Assert.Empty(result.Parameters);
    }

    [Fact]
    public void AcceptsDictionaryParameters()
    {
        var paramsDict = new Dictionary<string, object> { { "First", 123 } };
        var result = new RazorComponentResult(typeof(SimpleComponent), paramsDict);
        Assert.Equal(1, result.Parameters.Count);
        Assert.Equal(123, result.Parameters["First"]);
        Assert.Same(paramsDict, result.Parameters);
    }

    [Fact]
    public void AcceptsObjectParameters()
    {
        var result = new RazorComponentResult(typeof(SimpleComponent), new { Param1 = 123, Param2 = "Another" });
        Assert.Equal(2, result.Parameters.Count);
        Assert.Equal(123, result.Parameters["Param1"]);
        Assert.Equal("Another", result.Parameters["Param2"]);
    }

    [Fact]
    public async Task CanRenderComponentStatically()
    {
        // Arrange
        var result = new RazorComponentResult<SimpleComponent>();
        var httpContext = GetTestHttpContext();
        var responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        Assert.Equal("<h1>Hello from SimpleComponent</h1>", GetStringContent(responseBody));
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

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        Assert.Equal("<h1>Hello from SimpleComponent</h1>", GetStringContent(responseBody));
        Assert.Equal("application/test-content-type", httpContext.Response.ContentType);
        Assert.Equal(123, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task WaitsForQuiescence()
    {
        // Arrange
        var tcs = new TaskCompletionSource();
        var result = new RazorComponentResult<AsyncLoadingComponent>(new { LoadingTask = tcs.Task });
        var httpContext = GetTestHttpContext();
        var responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;

        // Act/Assert: Doesn't complete until loading finishes
        var completionTask = result.ExecuteAsync(httpContext);
        await Task.Yield();
        Assert.False(completionTask.IsCompleted);

        // Act/Assert: Does complete when loading finishes
        tcs.SetResult();
        await completionTask;
        Assert.Equal("Loading task status: RanToCompletion", GetStringContent(responseBody));
    }

    [Fact]
    public async Task SupportsLayouts()
    {
        // Arrange
        var result = new RazorComponentResult<ComponentWithLayout>();
        var httpContext = GetTestHttpContext();
        var responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;

        // Act
        await result.ExecuteAsync(httpContext);

        // Assert
        Assert.Equal("[TestParentLayout with content: [TestLayout with content: Page]]", GetStringContent(responseBody));
    }

    private static string GetStringContent(MemoryStream stream)
    {
        stream.Position = 0;
        return new StreamReader(stream).ReadToEnd();
    }

    private static DefaultHttpContext GetTestHttpContext()
    {
        var serviceCollection = new ServiceCollection()
            .AddSingleton(new DiagnosticListener("test"))
            .AddSingleton<RazorComponentResultExecutor>()
            .AddSingleton<IComponentPrerenderer, EndpointHtmlRenderer>()
            .AddSingleton<NavigationManager, FakeNavigationManager>()
            .AddSingleton<ServerComponentSerializer>()
            .AddSingleton<ComponentStatePersistenceManager>()
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

    [Layout(typeof(TestLayout))]
    class ComponentWithLayout : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
            => builder.AddContent(0, $"Page");
    }

    [Layout(typeof(TestParentLayout))]
    class TestLayout : LayoutComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, $"[{nameof(TestLayout)} with content: ");
            builder.AddContent(1, Body);
            builder.AddContent(2, "]");
        }
    }

    class TestParentLayout : LayoutComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, $"[{nameof(TestParentLayout)} with content: ");
            builder.AddContent(1, Body);
            builder.AddContent(2, "]");
        }
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
