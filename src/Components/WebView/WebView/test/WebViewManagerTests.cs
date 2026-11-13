// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.WebView;

public class WebViewManagerTests
{
    [Fact]
    public async Task CanRenderRootComponentAsync()
    {
        // Arrange
        var services = RegisterTestServices().AddTestBlazorWebView().BuildServiceProvider();
        var fileProvider = new TestFileProvider();
        var webViewManager = new TestWebViewManager(services, fileProvider);
        await webViewManager.AddRootComponentAsync(typeof(MyComponent), "#app", ParameterView.Empty);

        // Act
        Assert.Empty(webViewManager.SentIpcMessages);
        webViewManager.ReceiveAttachPageMessage();

        // Assert
        Assert.Collection(webViewManager.SentIpcMessages,
            m => AssertHelpers.IsAttachWebRendererInteropMessage(m),
            m => AssertHelpers.IsAttachToDocumentMessage(m, 0, "#app"),
            m => AssertHelpers.IsRenderBatch(m));
    }

    [Fact]
    public async Task CanRenderRootComponent_AfterThePageIsAttachedAsync()
    {
        // Arrange
        var services = RegisterTestServices().AddTestBlazorWebView().BuildServiceProvider();
        var fileProvider = new TestFileProvider();
        var webViewManager = new TestWebViewManager(services, fileProvider);

        Assert.Empty(webViewManager.SentIpcMessages);
        webViewManager.ReceiveAttachPageMessage();

        // Act
        Assert.Collection(webViewManager.SentIpcMessages,
            m => AssertHelpers.IsAttachWebRendererInteropMessage(m));
        await webViewManager.AddRootComponentAsync(typeof(MyComponent), "#app", ParameterView.Empty);

        // Assert
        Assert.Collection(webViewManager.SentIpcMessages.Skip(1),
            m => AssertHelpers.IsAttachToDocumentMessage(m, 0, "#app"),
            m => AssertHelpers.IsRenderBatch(m));
    }

    [Fact]
    public async Task AttachingToNewPage_DisposesExistingScopeAsync()
    {
        // Arrange
        var services = RegisterTestServices().AddTestBlazorWebView().BuildServiceProvider();
        var fileProvider = new TestFileProvider();
        var webViewManager = new TestWebViewManager(services, fileProvider);
        await webViewManager.AddRootComponentAsync(typeof(MyComponent), "#app", ParameterView.Empty);
        var singleton = services.GetRequiredService<SingletonService>();

        // Act
        Assert.Empty(webViewManager.SentIpcMessages);
        webViewManager.ReceiveAttachPageMessage();
        webViewManager.ReceiveAttachPageMessage();

        // Assert
        Assert.Collection(webViewManager.SentIpcMessages,
            m => AssertHelpers.IsAttachWebRendererInteropMessage(m),
            m => AssertHelpers.IsAttachToDocumentMessage(m, 0, "#app"),
            m => AssertHelpers.IsRenderBatch(m),
            m => AssertHelpers.IsAttachWebRendererInteropMessage(m),
            m => AssertHelpers.IsAttachToDocumentMessage(m, 0, "#app"),
            m => AssertHelpers.IsRenderBatch(m));

        Assert.Equal(2, singleton.Services.Count);
        Assert.NotSame(singleton.Services[0], singleton.Services[1]);
    }

    [Fact]
    public async Task CanDisposeWebViewManagerWithAsyncDisposableServices()
    {
        // Arrange
        var services = RegisterTestServices()
            .AddTestBlazorWebView()
            .AddScoped<AsyncDisposableService>()
            .BuildServiceProvider();
        var fileProvider = new TestFileProvider();
        var webViewManager = new TestWebViewManager(services, fileProvider);
        await webViewManager.AddRootComponentAsync(typeof(MyComponentUsingScopedAsyncDisposableService), "#app", ParameterView.Empty);
        webViewManager.ReceiveAttachPageMessage();

        // Act
        await webViewManager.DisposeAsync();
    }

    [Fact]
    public async Task DisposingWebViewManager_MarksJSRuntimeAsDisconnected()
    {
        // Arrange
        var services = RegisterTestServices().AddTestBlazorWebView().BuildServiceProvider();
        var fileProvider = new TestFileProvider();
        var webViewManager = new TestWebViewManager(services, fileProvider);
        await webViewManager.AddRootComponentAsync(typeof(MyComponent), "#app", ParameterView.Empty);
        webViewManager.ReceiveAttachPageMessage();

        var jsRuntime = webViewManager.CurrentPageContext.JSRuntime;

        // Act
        await webViewManager.DisposeAsync();

        // Assert - After disposal, JS interop calls should throw JSDisconnectedException
        await Assert.ThrowsAsync<JSDisconnectedException>(async () =>
            await jsRuntime.InvokeAsync<string>("someFunction", Array.Empty<object>()));
    }

    [Fact]
    public async Task NavigatingToNewPage_MarksOldJSRuntimeAsDisconnected()
    {
        // Arrange
        var services = RegisterTestServices().AddTestBlazorWebView().BuildServiceProvider();
        var fileProvider = new TestFileProvider();
        var webViewManager = new TestWebViewManager(services, fileProvider);
        await webViewManager.AddRootComponentAsync(typeof(MyComponent), "#app", ParameterView.Empty);

        // Attach to first page
        webViewManager.ReceiveAttachPageMessage();

        var firstPageJSRuntime = webViewManager.CurrentPageContext.JSRuntime;

        // Act - Simulate navigation to a new page by attaching again
        webViewManager.ReceiveAttachPageMessage();

        // Assert - The first page's JS runtime should now be disconnected
        await Assert.ThrowsAsync<JSDisconnectedException>(async () =>
            await firstPageJSRuntime.InvokeAsync<string>("someFunction", Array.Empty<object>()));
    }

    [Fact]
    public async Task AddRootComponentsWithExistingSelector_Throws()
    {
        // Arrange
        const string arbitraryComponentSelector = "some_selector";
        var services = RegisterTestServices().AddTestBlazorWebView().BuildServiceProvider();
        var fileProvider = new TestFileProvider();
        var webViewManager = new TestWebViewManager(services, fileProvider);
        await webViewManager.AddRootComponentAsync(typeof(MyComponent), arbitraryComponentSelector, ParameterView.Empty);

        // Act & assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await webViewManager.AddRootComponentAsync(typeof(MyComponent), arbitraryComponentSelector, ParameterView.Empty));

        Assert.Equal($"There is already a root component with selector '{arbitraryComponentSelector}'.", ex.Message);
    }

    [Fact]
    public async Task CanRenderComponentWithInteractiveServerRenderMode()
    {
        // Arrange
        var services = RegisterTestServices().AddTestBlazorWebView().BuildServiceProvider();
        var fileProvider = new TestFileProvider();
        var webViewManager = new TestWebViewManager(services, fileProvider);
        await webViewManager.AddRootComponentAsync(typeof(MyComponentWithInteractiveServerChild), "#app", ParameterView.Empty);

        // Act - should not throw NotSupportedException
        webViewManager.ReceiveAttachPageMessage();

        // Assert
        Assert.Collection(webViewManager.SentIpcMessages,
            m => AssertHelpers.IsAttachWebRendererInteropMessage(m),
            m => AssertHelpers.IsAttachToDocumentMessage(m, 0, "#app"),
            m => AssertHelpers.IsRenderBatch(m));
    }

    [Fact]
    public async Task CanRenderComponentWithInteractiveWebAssemblyRenderMode()
    {
        // Arrange
        var services = RegisterTestServices().AddTestBlazorWebView().BuildServiceProvider();
        var fileProvider = new TestFileProvider();
        var webViewManager = new TestWebViewManager(services, fileProvider);
        await webViewManager.AddRootComponentAsync(typeof(MyComponentWithInteractiveWebAssemblyChild), "#app", ParameterView.Empty);

        // Act - should not throw NotSupportedException
        webViewManager.ReceiveAttachPageMessage();

        // Assert
        Assert.Collection(webViewManager.SentIpcMessages,
            m => AssertHelpers.IsAttachWebRendererInteropMessage(m),
            m => AssertHelpers.IsAttachToDocumentMessage(m, 0, "#app"),
            m => AssertHelpers.IsRenderBatch(m));
    }

    [Fact]
    public async Task CanRenderComponentWithInteractiveAutoRenderMode()
    {
        // Arrange
        var services = RegisterTestServices().AddTestBlazorWebView().BuildServiceProvider();
        var fileProvider = new TestFileProvider();
        var webViewManager = new TestWebViewManager(services, fileProvider);
        await webViewManager.AddRootComponentAsync(typeof(MyComponentWithInteractiveAutoChild), "#app", ParameterView.Empty);

        // Act - should not throw NotSupportedException
        webViewManager.ReceiveAttachPageMessage();

        // Assert
        Assert.Collection(webViewManager.SentIpcMessages,
            m => AssertHelpers.IsAttachWebRendererInteropMessage(m),
            m => AssertHelpers.IsAttachToDocumentMessage(m, 0, "#app"),
            m => AssertHelpers.IsRenderBatch(m));
    }

    [Fact]
    public async Task CanRenderComponentWithCustomRenderMode()
    {
        // Arrange
        var services = RegisterTestServices().AddTestBlazorWebView().BuildServiceProvider();
        var fileProvider = new TestFileProvider();
        var webViewManager = new TestWebViewManager(services, fileProvider);
        await webViewManager.AddRootComponentAsync(typeof(MyComponentWithCustomRenderModeChild), "#app", ParameterView.Empty);

        // Act - should not throw NotSupportedException even for unknown/custom render modes
        webViewManager.ReceiveAttachPageMessage();

        // Assert
        Assert.Collection(webViewManager.SentIpcMessages,
            m => AssertHelpers.IsAttachWebRendererInteropMessage(m),
            m => AssertHelpers.IsAttachToDocumentMessage(m, 0, "#app"),
            m => AssertHelpers.IsRenderBatch(m));
    }

    [Fact]
    public async Task DisposingWebViewManager_ReleasesCurrentPageContext()
    {
        await using var services = RegisterTestServices()
            .AddTestBlazorWebView()
            .BuildServiceProvider();

        await using var webViewManager = new TestWebViewManager(
            services,
            new TestFileProvider());

        await webViewManager.AddRootComponentAsync(
            typeof(MyComponent),
            "#app",
            ParameterView.Empty);

        webViewManager.ReceiveAttachPageMessage();

        Assert.NotNull(webViewManager.CurrentPageContext);

        var scopedService = Assert.Single(
            services.GetRequiredService<SingletonService>().Services);

        Assert.False(scopedService.Disposed);

        await webViewManager.DisposeAsync();

        Assert.True(scopedService.Disposed);
        Assert.Null(webViewManager.CurrentPageContext);
    }

    [Fact]
    public async Task DisposingWebViewManager_ReleasesCurrentPageContextWhenScopeDisposalThrows()
    {
        var expectedException = new InvalidOperationException("Scope disposal failed.");
        var scopedService = new ThrowingAsyncDisposableService(expectedException);
        await using var services = RegisterTestServices()
            .AddTestBlazorWebView()
            .AddScoped(_ => scopedService)
            .BuildServiceProvider();
        await using var webViewManager = new TestWebViewManager(services, new TestFileProvider());
        await webViewManager.AddRootComponentAsync(
            typeof(ComponentWithThrowingAsyncDisposableService),
            "#app",
            ParameterView.Empty);
        webViewManager.ReceiveAttachPageMessage();
        Assert.NotNull(webViewManager.CurrentPageContext);
        Assert.False(scopedService.Disposed);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => webViewManager.DisposeAsync().AsTask());

        Assert.Same(expectedException, exception);
        Assert.True(scopedService.Disposed);
        Assert.Null(webViewManager.CurrentPageContext);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DisposingWebViewManager_DoesNotAttachAnotherPage(bool attachmentStartedFirst)
    {
        var disposalEntered = new TaskCompletionSource();
        var releaseDisposal = new TaskCompletionSource();
        var createdServices = new List<GatedAsyncDisposableService>();
        await using var services = RegisterTestServices()
            .AddTestBlazorWebView()
            .AddScoped(_ =>
            {
                var service = new GatedAsyncDisposableService(disposalEntered, releaseDisposal.Task);
                createdServices.Add(service);
                return service;
            })
            .BuildServiceProvider();
        await using var webViewManager = new TestWebViewManager(services, new TestFileProvider());
        await webViewManager.AddRootComponentAsync(
            typeof(ComponentWithGatedAsyncDisposableService),
            "#app",
            ParameterView.Empty);
        webViewManager.ReceiveAttachPageMessage();
        var originalContext = webViewManager.CurrentPageContext;
        Assert.NotNull(originalContext);
        Assert.Single(createdServices);

        Task attachmentTask = Task.CompletedTask;
        Task disposalTask = Task.CompletedTask;
        try
        {
            if (attachmentStartedFirst)
            {
                attachmentTask = originalContext.Renderer.Dispatcher.InvokeAsync(
                    () => webViewManager.AttachToPageAsync("http://example/", "http://example/reloaded"));
            }
            else
            {
                disposalTask = webViewManager.DisposeAsync().AsTask();
            }

            Assert.True(disposalEntered.Task.IsCompletedSuccessfully);

            if (attachmentStartedFirst)
            {
                disposalTask = webViewManager.DisposeAsync().AsTask();
            }
            else
            {
                // Use the same incoming IPC path as a browser reload.
                webViewManager.ReceiveAttachPageMessage();
            }
        }
        finally
        {
            releaseDisposal.TrySetResult();
            await attachmentTask;
            await disposalTask;
        }

        Assert.Single(createdServices);
        Assert.True(createdServices[0].Disposed);
        Assert.Null(webViewManager.CurrentPageContext);
    }

    private sealed class ComponentWithGatedAsyncDisposableService : ComponentBase
    {
        [Inject]
        public GatedAsyncDisposableService Service { get; set; }
    }

    private sealed class GatedAsyncDisposableService(
        TaskCompletionSource disposalEntered,
        Task releaseDisposal) : IAsyncDisposable
    {
        public bool Disposed { get; private set; }

        public async ValueTask DisposeAsync()
        {
            disposalEntered.TrySetResult();
            await releaseDisposal;
            Disposed = true;
        }
    }

    private sealed class ComponentWithThrowingAsyncDisposableService : ComponentBase
    {
        [Inject]
        public ThrowingAsyncDisposableService Service { get; set; }
    }

    private sealed class ThrowingAsyncDisposableService(InvalidOperationException exception) : IAsyncDisposable
    {
        public bool Disposed { get; private set; }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.FromException(exception);
        }
    }

    private static IServiceCollection RegisterTestServices()
    {
        return new ServiceCollection().AddSingleton<SingletonService>().AddScoped<ScopedService>();
    }

    private class MyComponent : IComponent
    {
        private RenderHandle _handle;

        public void Attach(RenderHandle renderHandle)
        {
            _handle = renderHandle;
        }

        [Inject] public ScopedService MyScopedService { get; set; }

        public Task SetParametersAsync(ParameterView parameters)
        {
            _handle.Render(builder =>
            {
                builder.OpenElement(0, "p");
                builder.AddContent(1, "Hello world!");
                builder.CloseElement();
            });

            return Task.CompletedTask;
        }
    }

    private class MyComponentUsingScopedAsyncDisposableService : IComponent
    {
        private RenderHandle _handle;

        public void Attach(RenderHandle renderHandle)
        {
            _handle = renderHandle;
        }

        [Inject] public AsyncDisposableService MyAsyncDisposableService { get; set; }

        public Task SetParametersAsync(ParameterView parameters)
        {
            _handle.Render(builder =>
            {
                builder.OpenElement(0, "p");
                builder.AddContent(1, "Hello world!");
                builder.CloseElement();
            });

            return Task.CompletedTask;
        }
    }

    private class SingletonService
    {
        public List<ScopedService> Services { get; } = new();

        public void Add(ScopedService service)
        {
            Services.Add(service);
        }
    }

    private class ScopedService : IDisposable
    {
        public ScopedService(SingletonService singleton)
        {
            singleton.Add(this);
        }

        public bool Disposed { get; private set; }

        public void Dispose()
        {
            Disposed = true;
        }
    }

    public class AsyncDisposableService : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    // Components with render modes for testing ResolveComponentForRenderMode

    private class ChildComponent : IComponent
    {
        private RenderHandle _handle;

        public void Attach(RenderHandle renderHandle) => _handle = renderHandle;

        public Task SetParametersAsync(ParameterView parameters)
        {
            _handle.Render(builder =>
            {
                builder.AddContent(0, "Child content");
            });

            return Task.CompletedTask;
        }
    }

    private class MyComponentWithInteractiveServerChild : IComponent
    {
        private RenderHandle _handle;

        public void Attach(RenderHandle renderHandle) => _handle = renderHandle;

        public Task SetParametersAsync(ParameterView parameters)
        {
            _handle.Render(builder =>
            {
                builder.OpenComponent<ChildComponent>(0);
                builder.AddComponentRenderMode(RenderMode.InteractiveServer);
                builder.CloseComponent();
            });

            return Task.CompletedTask;
        }
    }

    private class MyComponentWithInteractiveWebAssemblyChild : IComponent
    {
        private RenderHandle _handle;

        public void Attach(RenderHandle renderHandle) => _handle = renderHandle;

        public Task SetParametersAsync(ParameterView parameters)
        {
            _handle.Render(builder =>
            {
                builder.OpenComponent<ChildComponent>(0);
                builder.AddComponentRenderMode(RenderMode.InteractiveWebAssembly);
                builder.CloseComponent();
            });

            return Task.CompletedTask;
        }
    }

    private class MyComponentWithInteractiveAutoChild : IComponent
    {
        private RenderHandle _handle;

        public void Attach(RenderHandle renderHandle) => _handle = renderHandle;

        public Task SetParametersAsync(ParameterView parameters)
        {
            _handle.Render(builder =>
            {
                builder.OpenComponent<ChildComponent>(0);
                builder.AddComponentRenderMode(RenderMode.InteractiveAuto);
                builder.CloseComponent();
            });

            return Task.CompletedTask;
        }
    }

    private class MyComponentWithCustomRenderModeChild : IComponent
    {
        private RenderHandle _handle;

        public void Attach(RenderHandle renderHandle) => _handle = renderHandle;

        public Task SetParametersAsync(ParameterView parameters)
        {
            _handle.Render(builder =>
            {
                builder.OpenComponent<ChildComponent>(0);
                builder.AddComponentRenderMode(new CustomRenderMode());
                builder.CloseComponent();
            });

            return Task.CompletedTask;
        }
    }

    private class CustomRenderMode : IComponentRenderMode { }
}
