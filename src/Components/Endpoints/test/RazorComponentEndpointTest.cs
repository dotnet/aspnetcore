// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Infrastructure;
using System.Diagnostics;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class RazorComponentEndpointTest
{
    [Fact]
    public async Task CanRenderComponentStatically()
    {
        // Arrange
        var httpContext = GetTestHttpContext();
        var responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;

        // Act
        await RazorComponentEndpoint.RenderComponentToResponse(
            httpContext,
            RenderMode.Static,
            typeof(SimpleComponent),
            componentParameters: null,
            preventStreamingRendering: false);

        // Assert
        Assert.Equal("<h1>Hello from SimpleComponent</h1>", GetStringContent(responseBody));
    }

    [Fact]
    public async Task PerformsStreamingRendering()
    {
        // Arrange
        var tcs = new TaskCompletionSource();
        var httpContext = GetTestHttpContext();
        var responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;

        // Act/Assert 1: Emits the initial pre-quiescent output to the response
        var completionTask = RazorComponentEndpoint.RenderComponentToResponse(
            httpContext,
            RenderMode.Static,
            typeof(AsyncLoadingComponent),
            PropertyHelper.ObjectToDictionary(new { LoadingTask = tcs.Task }).AsReadOnly(),
            preventStreamingRendering: false);
        Assert.Equal("Loading task status: WaitingForActivation", GetStringContent(responseBody));

        // Assert 2: Result task remains incomplete for as long as the component's loading operation remains in flight
        // This keeps the HTTP response open
        await Task.Yield();
        Assert.False(completionTask.IsCompleted);

        // Act/Assert 3: When loading completes, it emits a streaming batch update and completes the response
        tcs.SetResult();
        await completionTask;
        Assert.Equal("Loading task status: WaitingForActivation<blazor-ssr><template blazor-component-id=\"2\">Loading task status: RanToCompletion</template></blazor-ssr>", GetStringContent(responseBody));
    }

    [Fact]
    public async Task WaitsForQuiescenceIfPreventStreamingRenderingIsTrue()
    {
        // Arrange
        var tcs = new TaskCompletionSource();
        var httpContext = GetTestHttpContext();
        var responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;

        // Act/Assert: Doesn't complete until loading finishes
        var completionTask = RazorComponentEndpoint.RenderComponentToResponse(
            httpContext,
            RenderMode.Static,
            typeof(AsyncLoadingComponent),
            PropertyHelper.ObjectToDictionary(new { LoadingTask = tcs.Task }).AsReadOnly(),
            preventStreamingRendering: true);
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
        var httpContext = GetTestHttpContext();
        var responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;

        // Act
        await RazorComponentEndpoint.RenderComponentToResponse(
            httpContext, RenderMode.Static, typeof(ComponentWithLayout),
            null, false);

        // Assert
        Assert.Equal("[TestParentLayout with content: [TestLayout with content: Page]]", GetStringContent(responseBody));
    }

    private static string GetStringContent(MemoryStream stream)
    {
        stream.Position = 0;
        return new StreamReader(stream).ReadToEnd();
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

    public static DefaultHttpContext GetTestHttpContext()
    {
        var serviceCollection = new ServiceCollection()
            .AddSingleton(new DiagnosticListener("test"))
            .AddSingleton<RazorComponentResultExecutor>()
            .AddSingleton<EndpointHtmlRenderer>()
            .AddSingleton<IComponentPrerenderer>(services => services.GetRequiredService<EndpointHtmlRenderer>())
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
