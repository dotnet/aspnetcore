// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.WebView.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;

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

    // ---------------------------------------------------------------------
    // Disposal / disconnection tests
    //
    // These tests exercise the JSDisconnectedException + IpcSender guards
    // added for dotnet/aspnetcore#66255 (parity with RemoteJSRuntime) and
    // dotnet/maui#34855 (WebView2WebViewManager disposal handling).
    //
    // Lifecycles:
    //   * WebViewJSRuntime.IsDisposed   - flipped per-PageContext on PageContext.DisposeAsync
    //   * IpcSender.IsDisposed          - flipped per-WebViewManager on DisposeAsyncCore
    //
    // After flipping:
    //   * BeginInvokeJS throws JSDisconnectedException (recoverable for JSObjectReference.DisposeAsync)
    //   * EndInvokeDotNet / SendByteArray silently no-op (stale responses to a gone JS side)
    //   * IpcReceiver drops incoming messages targeting the disposed runtime
    //   * IpcSender drops outbound dispatches and unhandled-exception notifications
    // ---------------------------------------------------------------------

    [Fact]
    public async Task JSInteropDuringComponentDispose_OnPageReload_SeesJSDisconnectedException()
    {
        var services = RegisterTestServices().AddTestBlazorWebView().BuildServiceProvider();
        var fileProvider = new TestFileProvider();
        var webViewManager = new TestWebViewManager(services, fileProvider);
        await webViewManager.AddRootComponentAsync(typeof(PerformJSInteropOnDisposeComponent), "#app", ParameterView.Empty);

        webViewManager.ReceiveAttachPageMessage();

        // Simulate a page reload: AttachToPageAsync disposes the previous PageContext
        // and creates a new one. Components implementing IAsyncDisposable that invoke
        // JS interop during disposal should observe JSDisconnectedException rather than
        // an unhandled JSException or an indefinite hang.
        webViewManager.ReceiveAttachPageMessage();

        var singleton = services.GetRequiredService<SingletonService>();
        Assert.Single(singleton.DisposedComponentExceptions);
        Assert.IsType<JSDisconnectedException>(singleton.DisposedComponentExceptions[0]);
    }

    [Fact]
    public async Task JSInteropAfterWebViewManagerDispose_SeesJSDisconnectedException()
    {
        var services = RegisterTestServices().AddTestBlazorWebView().BuildServiceProvider();
        var fileProvider = new TestFileProvider();
        var webViewManager = new TestWebViewManager(services, fileProvider);
        await webViewManager.AddRootComponentAsync(typeof(PerformJSInteropOnDisposeComponent), "#app", ParameterView.Empty);

        webViewManager.ReceiveAttachPageMessage();

        // Disposing the WebViewManager disposes the current PageContext, which marks
        // the JS runtime as disconnected. Components running in DisposeAsync see
        // JSDisconnectedException instead of crashing the host.
        await webViewManager.DisposeAsync();

        var singleton = services.GetRequiredService<SingletonService>();
        Assert.Single(singleton.DisposedComponentExceptions);
        Assert.IsType<JSDisconnectedException>(singleton.DisposedComponentExceptions[0]);
    }

    [Fact]
    public async Task PageContextDispose_MarksJSRuntimeAsDisconnected_BeforeRendererDispose()
    {
        // Direct unit test of the dispose ordering invariant: PageContext.DisposeAsync
        // MUST call JSRuntime.MarkAsDisconnected() *before* Renderer.DisposeAsync(), so
        // that any JS interop performed by IAsyncDisposable components during render
        // teardown sees JSDisconnectedException rather than queueing to a defunct page.
        var services = RegisterTestServices().AddTestBlazorWebView().BuildServiceProvider();
        var fileProvider = new TestFileProvider();
        var webViewManager = new TestWebViewManager(services, fileProvider);
        await webViewManager.AddRootComponentAsync(typeof(CaptureJSRuntimeComponent), "#app", ParameterView.Empty);

        webViewManager.ReceiveAttachPageMessage();

        var singleton = services.GetRequiredService<SingletonService>();
        var capturedRuntime = (WebViewJSRuntime)singleton.CapturedJSRuntime;
        Assert.False(capturedRuntime.IsDisposed);

        // Trigger a page reload to dispose the old PageContext (and hence the captured runtime).
        webViewManager.ReceiveAttachPageMessage();

        Assert.True(capturedRuntime.IsDisposed);

        // And direct BeginInvokeJS on the disposed runtime must throw the expected exception.
        var ex = await Assert.ThrowsAsync<JSDisconnectedException>(async () =>
            await capturedRuntime.InvokeAsync<string>("noOp", Array.Empty<object>()));
        Assert.Contains("WebView page has been disposed", ex.Message);
    }

    [Fact]
    public async Task EndInvokeDotNet_AfterRuntimeDisposed_DropsOutboundMessage()
    {
        // Drives a .NET-from-JS invocation via DotNetDispatcher.BeginInvokeDotNet, which
        // calls the [JSInvokable] method synchronously and then routes the result back
        // through runtime.EndInvokeDotNet. With the runtime marked disconnected first,
        // EndInvokeDotNet must drop the reply rather than dispatching an IPC message to
        // a page that's already gone.
        var services = RegisterTestServices().AddTestBlazorWebView().BuildServiceProvider();
        var fileProvider = new TestFileProvider();
        var webViewManager = new TestWebViewManager(services, fileProvider);
        await webViewManager.AddRootComponentAsync(typeof(CaptureJSRuntimeComponent), "#app", ParameterView.Empty);

        webViewManager.ReceiveAttachPageMessage();
        var singleton = services.GetRequiredService<SingletonService>();
        var capturedRuntime = (WebViewJSRuntime)singleton.CapturedJSRuntime;
        capturedRuntime.MarkAsDisconnected();

        var ipcMessagesBefore = webViewManager.SentIpcMessages.Count;

        DotNetDispatcher.BeginInvokeDotNet(
            capturedRuntime,
            new DotNetInvocationInfo(typeof(WebViewManagerTests).Assembly.GetName().Name, nameof(JSInvokableHelpers.ReturnGreeting), 0, callId: "test-call-1"),
            "[]");

        Assert.Equal(ipcMessagesBefore, webViewManager.SentIpcMessages.Count);
    }

    [Fact]
    public async Task SendByteArray_AfterRuntimeDisposed_DropsOutboundMessage()
    {
        // SendByteArray is a protected override on JSRuntime; there is no public driver
        // for it from outside the framework, so the test reaches it via reflection. The
        // guard pattern mirrors EndInvokeDotNet — a single `if (_isDisposed) return;` —
        // and dropping the chunk is the only safe behavior once the receiving JS context
        // is gone.
        var services = RegisterTestServices().AddTestBlazorWebView().BuildServiceProvider();
        var fileProvider = new TestFileProvider();
        var webViewManager = new TestWebViewManager(services, fileProvider);
        await webViewManager.AddRootComponentAsync(typeof(CaptureJSRuntimeComponent), "#app", ParameterView.Empty);

        webViewManager.ReceiveAttachPageMessage();
        var singleton = services.GetRequiredService<SingletonService>();
        var capturedRuntime = (WebViewJSRuntime)singleton.CapturedJSRuntime;
        capturedRuntime.MarkAsDisconnected();

        var ipcMessagesBefore = webViewManager.SentIpcMessages.Count;

        var sendByteArray = typeof(WebViewJSRuntime).GetMethod(
            "SendByteArray",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        sendByteArray.Invoke(capturedRuntime, new object[] { 7, new byte[] { 1, 2, 3 } });

        Assert.Equal(ipcMessagesBefore, webViewManager.SentIpcMessages.Count);
    }

    [Fact]
    public async Task IpcReceiver_AfterPageContextDisposed_DropsIncomingMessages()
    {
        // After WebViewManager.DisposeAsync, the current PageContext is disposed (its
        // JSRuntime.IsDisposed flips true) but the reference is still held. A stale
        // incoming IPC message - for example a JS-side handler firing during teardown -
        // must NOT be routed through DotNetDispatcher onto the disposed runtime/scope.
        var services = RegisterTestServices().AddTestBlazorWebView().BuildServiceProvider();
        var fileProvider = new TestFileProvider();
        var webViewManager = new TestWebViewManager(services, fileProvider);
        await webViewManager.AddRootComponentAsync(typeof(MyComponent), "#app", ParameterView.Empty);
        webViewManager.ReceiveAttachPageMessage();
        await webViewManager.DisposeAsync();

        // Sending any further IPC message must be a silent no-op (no exception thrown,
        // no further outbound traffic). The IpcSender is also disposed at this point,
        // so even if a message did get through, nothing would reach SendMessage.
        var ipcMessagesBefore = webViewManager.SentIpcMessages.Count;
        webViewManager.ReceiveIpcMessage(
            IpcCommon.IncomingMessageType.BeginInvokeDotNet,
            /* callId */ "stale-call",
            /* assemblyName */ typeof(WebViewManagerTests).Assembly.GetName().Name!,
            /* methodIdentifier */ nameof(JSInvokableHelpers.ReturnGreeting),
            /* dotNetObjectId */ 0L,
            /* argsJson */ "[]");

        Assert.Equal(ipcMessagesBefore, webViewManager.SentIpcMessages.Count);
    }

    [Fact]
    public void IpcSender_AfterDispose_DropsOutboundDispatches()
    {
        // Direct unit test of the IpcSender guard. Constructs an IpcSender with a
        // captured Action<string> delegate, disposes it, and verifies the delegate
        // is never invoked. Mirrors the alexdess NullReferenceException variant from
        // dotnet/maui#34855 where WebView2WebViewManager.SendMessage was reaching a
        // CoreWebView2 whose underlying control had been disposed.
        var sentMessages = new List<string>();
        var sender = new IpcSender(Dispatcher.CreateDefault(), sentMessages.Add);

        sender.Navigate("/home", new NavigationOptions());
        Assert.Single(sentMessages);

        sender.Dispose();
        Assert.True(sender.IsDisposed);

        // Every outbound entry point should now no-op.
        sender.Navigate("/about", new NavigationOptions());
        sender.Refresh(forceReload: true);
        sender.AttachToDocument(componentId: 0, "#app");
        sender.SendByteArray(id: 1, new byte[] { 1, 2, 3 });
        sender.SetHasLocationChangingListeners(true);
        sender.EndLocationChanging(callId: 0, shouldContinueNavigation: false);

        Assert.Single(sentMessages); // still only the pre-dispose Navigate
    }

    [Fact]
    public void IpcSender_AfterDispose_DropsNotifyUnhandledException()
    {
        // NotifyUnhandledException has its own dispatch path (uses _dispatcher.InvokeAsync
        // directly rather than DispatchMessageWithErrorHandling) and also re-throws via
        // ExceptionDispatchInfo to surface to AppDomain.UnhandledException. Both must
        // no-op once the WebView is disposed; otherwise a background-task race during
        // window close can crash the host (the original symptom in dotnet/maui#34855).
        var sentMessages = new List<string>();
        var sender = new IpcSender(Dispatcher.CreateDefault(), sentMessages.Add);
        sender.Dispose();

        // Should neither send a message nor rethrow.
        sender.NotifyUnhandledException(new InvalidOperationException("background task failed after webview gone"));

        Assert.Empty(sentMessages);
    }

    [Fact]
    public async Task AttachToPageAsync_AfterWebViewManagerDispose_DoesNotResurrectManager()
    {
        // A late AttachPage IPC message after WebViewManager.DisposeAsync must NOT
        // create a new PageContext, scope, renderer, or JS runtime. Without the guard,
        // the manager would resurrect itself with a new page graph built against an
        // already-disposed IpcSender (see dotnet/maui#34855 race with window close
        // happening mid-navigation).
        var services = RegisterTestServices().AddTestBlazorWebView().BuildServiceProvider();
        var fileProvider = new TestFileProvider();
        var webViewManager = new TestWebViewManager(services, fileProvider);
        await webViewManager.AddRootComponentAsync(typeof(MyComponent), "#app", ParameterView.Empty);
        webViewManager.ReceiveAttachPageMessage();
        await webViewManager.DisposeAsync();

        var ipcMessagesBefore = webViewManager.SentIpcMessages.Count;
        var scopesBefore = services.GetRequiredService<SingletonService>().Services.Count;

        // Simulate a late AttachPage arriving after disposal.
        webViewManager.ReceiveAttachPageMessage();

        // No new attach/render IPC traffic (because no new PageContext was constructed)
        // and no new scoped service instance was created.
        Assert.Equal(ipcMessagesBefore, webViewManager.SentIpcMessages.Count);
        Assert.Equal(scopesBefore, services.GetRequiredService<SingletonService>().Services.Count);
    }

    [Fact]
    public async Task TryDispatchAsync_AfterWebViewManagerDispose_ReturnsFalse()
    {
        // TryDispatchAsync used to only check whether _currentPageContext was non-null
        // and whether the captured reference still matched. After WebViewManager.DisposeAsync,
        // both can remain true (the field is not nulled out by disposal), so workItem
        // would run against a disposed scope. Guarding on _disposed prevents that.
        var services = RegisterTestServices().AddTestBlazorWebView().BuildServiceProvider();
        var fileProvider = new TestFileProvider();
        var webViewManager = new TestWebViewManager(services, fileProvider);
        await webViewManager.AddRootComponentAsync(typeof(MyComponent), "#app", ParameterView.Empty);
        webViewManager.ReceiveAttachPageMessage();
        await webViewManager.DisposeAsync();

        var workItemRan = false;
        var result = await webViewManager.TryDispatchAsync(_ => workItemRan = true);

        Assert.False(result);
        Assert.False(workItemRan);
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
        public List<Exception> DisposedComponentExceptions { get; } = new();
        public IJSRuntime CapturedJSRuntime { get; set; } = default!;

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

    private class PerformJSInteropOnDisposeComponent : IComponent, IAsyncDisposable
    {
        private RenderHandle _handle;

        [Inject] public IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] public SingletonService Singleton { get; set; } = default!;

        public void Attach(RenderHandle renderHandle)
        {
            _handle = renderHandle;
        }

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

        public async ValueTask DisposeAsync()
        {
            // Mirrors the user pattern from dotnet/maui#34855: a component that calls
            // JS interop in its disposal handler. After the fix, the call surfaces
            // JSDisconnectedException which the component can catch and log.
            try
            {
                await JSRuntime.InvokeVoidAsync("SomeJsCleanupCode");
            }
            catch (Exception ex)
            {
                Singleton.DisposedComponentExceptions.Add(ex);
            }
        }
    }

    private class CaptureJSRuntimeComponent : IComponent
    {
        private RenderHandle _handle;

        [Inject] public IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] public SingletonService Singleton { get; set; } = default!;

        public void Attach(RenderHandle renderHandle)
        {
            _handle = renderHandle;
        }

        public Task SetParametersAsync(ParameterView parameters)
        {
            Singleton.CapturedJSRuntime = JSRuntime;

            _handle.Render(builder =>
            {
                builder.OpenElement(0, "p");
                builder.AddContent(1, "Capture runtime");
                builder.CloseElement();
            });

            return Task.CompletedTask;
        }
    }

    public static class JSInvokableHelpers
    {
        [JSInvokable]
        public static string ReturnGreeting() => "hello";
    }
}
