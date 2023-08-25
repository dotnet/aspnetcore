// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.WebView;

public class WebViewManagerTests
{
    // Skips:
    // - Ubuntu is skipped due to this error:
    //       "Unable to load shared library 'Photino.Native' or one of its dependencies. In order to help diagnose
    //       loading problems, consider using a tool like strace."
    //   There's probably some way to make it work, but it's not currently a supported Blazor Hybrid scenario anyway
    // - macOS is skipped due to the test not being able to detect when the WebView is ready. There's probably an issue
    //   with the JS code sending a WebMessage to C# and not being sent properly or detected properly.
    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX,
        SkipReason = "On Helix/Ubuntu the native Photino assemblies can't be found, and on macOS it can't detect when the WebView is ready")]
    public async Task CanLaunchPhotinoWebViewAndClickButton()
    {
        var photinoTestProgramExePath = typeof(WebViewManagerTests).Assembly.Location;

        // This test launches this very test assembly as an executable so that the Photino UI window
        // can launch and be automated. See the comment in Program.Main() for more info.
        var photinoProcess = new Process()
        {
            StartInfo = new ProcessStartInfo
            {
                WorkingDirectory = Path.GetDirectoryName(photinoTestProgramExePath),
                FileName = "dotnet",
                Arguments = $"\"{photinoTestProgramExePath}\"",
                RedirectStandardOutput = true,
            },
        };

        photinoProcess.Start();

        var testProgramOutput = photinoProcess.StandardOutput.ReadToEnd();

        await photinoProcess.WaitForExitAsync().TimeoutAfter(TimeSpan.FromSeconds(30));

        // The test app reports its own results by calling Console.WriteLine(), so here we only need to verify that
        // the test internally believes it passed (and we trust it!).
        Assert.Contains($"Test passed? {true}", testProgramOutput);
    }

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
}
