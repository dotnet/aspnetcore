// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.JSInterop;
using Moq;

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting;

public class WebAssemblyHostTest
{
    [Fact]
    public void HostStartupValuesRejectsNullKeyBeforeInitialization()
    {
        var startupValues = new InteractiveHostStartupValues();

        Assert.Throws<ArgumentNullException>(() => startupValues.GetValue(null!));
    }

    [Fact]
    public async Task BuildCollectsStartupValuesAndInitializesNavigationBeforeReturning()
    {
        var jsMethods = new TestInternalJSImportMethods
        {
            HostStartupValuesJson =
                """{"document.baseURI":"https://www.example.com/awesome-part-that-will-be-truncated-in-tests","location.href":"https://www.example.com/awesome-part-that-will-be-truncated-in-tests/cool","custom.value":"expected"}""",
        };
        var builder = new WebAssemblyHostBuilder(jsMethods);
        builder.Services.AddSingleton(Mock.Of<IJSRuntime>());
        builder.Services.AddSingleton<IBrowserStartupValueProvider>(
            new TestBrowserStartupValueProvider("custom.value"));
        var host = builder.Build();
        var keys = JsonSerializer.Deserialize<string[]>(jsMethods.HostStartupValueKeysJson);
        Assert.Equal(["document.baseURI", "location.href", "custom.value"], keys);
        Assert.Equal(
            "expected",
            host.Services.GetRequiredService<IHostStartupValues>().GetRequired("custom.value"));
        var navigationManager = host.Services.GetRequiredService<NavigationManager>();
        Assert.Equal("https://www.example.com/", navigationManager.BaseUri);
        Assert.Equal(
            "https://www.example.com/awesome-part-that-will-be-truncated-in-tests/cool",
            navigationManager.Uri);
        Assert.Same(WebAssemblyNavigationManager.Instance, navigationManager);

        await host.DisposeAsync();
    }

    [Fact]
    public async Task BuildStartsAsyncInitializerAndRunAwaitsIt()
    {
        var initializerStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var continueInitializer = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = new List<string>();
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());
        builder.Services.AddSingleton(Mock.Of<IJSRuntime>());
        builder.Services.AddSingleton<IHostInitializer>(
            new TestHostInitializer(
                "async",
                0,
                calls,
                asyncCallback: _ =>
                {
                    initializerStarted.SetResult();
                    return continueInitializer.Task;
                }));

        var host = builder.Build();
        await initializerStarted.Task;
        var navigationManager = host.Services.GetRequiredService<NavigationManager>();
        using var cancellationTokenSource = new CancellationTokenSource();

        var runTask = host.RunAsyncCore(
            cancellationTokenSource.Token,
            new TestSatelliteResourcesLoader());

        Assert.False(runTask.IsCompleted);
        Assert.Equal("https://www.example.com/", navigationManager.BaseUri);
        continueInitializer.SetResult();
        await Task.Yield();
        cancellationTokenSource.Cancel();
        await runTask.TimeoutAfter(TimeSpan.FromSeconds(3));
        Assert.Equal(["async"], calls);
    }

    [Fact]
    public void BuildRejectsDuplicateBrowserStartupValueKeysBeforeJSImport()
    {
        var jsMethods = new TestInternalJSImportMethods();
        var builder = new WebAssemblyHostBuilder(jsMethods);
        builder.Services.AddSingleton<IBrowserStartupValueProvider>(
            new TestBrowserStartupValueProvider("duplicate.value"));
        builder.Services.AddSingleton<IBrowserStartupValueProvider>(
            new TestBrowserStartupValueProvider("duplicate.value"));
        var exception = Assert.Throws<InvalidOperationException>(builder.Build);

        Assert.Equal(
            "The browser startup value key 'duplicate.value' was provided more than once.",
            exception.Message);
        Assert.Empty(jsMethods.HostStartupValueKeysJson);
    }

    [Theory]
    [InlineData("""{"document.baseURI":"base","location.href":"uri","unexpected":"value"}""")]
    [InlineData("""{"document.baseURI":"base"}""")]
    [InlineData("""{"document.baseURI":42,"location.href":"uri"}""")]
    [InlineData("""{"document.baseURI":"first","document.baseURI":"second","location.href":"uri"}""")]
    public void BuildRejectsInvalidBrowserStartupValues(string startupValuesJson)
    {
        var jsMethods = new TestInternalJSImportMethods
        {
            HostStartupValuesJson = startupValuesJson,
        };
        var builder = new WebAssemblyHostBuilder(jsMethods);
        var exception = Assert.Throws<InvalidOperationException>(builder.Build);

        Assert.Equal("The browser returned invalid host startup values.", exception.Message);
    }

    [Fact]
    public async Task BuildRunsHostThenBrowserPhasesInOrder()
    {
        var calls = new List<string>();
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());
        builder.Services.AddSingleton(Mock.Of<IJSRuntime>());
        builder.Services.AddSingleton<IHostInitializer>(
            new TestHostInitializer("lower", -100, calls));
        builder.Services.AddSingleton<IHostInitializer>(
            new TestHostInitializer("middle", 0, calls));
        builder.Services.AddSingleton<IHostInitializer>(
            new TestHostInitializer("browser", 100, calls, browserPhase: true));
        var host = builder.Build();

        Assert.Equal(["lower", "middle", "browser"], calls);
        await host.DisposeAsync();
    }

    [Fact]
    public void BuildSurfacesSynchronousHostInitializerFailure()
    {
        var calls = new List<string>();
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());
        builder.Services.AddSingleton<IHostInitializer>(
            new TestHostInitializer("failure", -300, calls, exception: new InvalidOperationException("Initializer failed.")));
        builder.Services.AddSingleton<IHostInitializer>(
            new TestHostInitializer("not-run", -200, calls));
        var exception = Assert.Throws<InvalidOperationException>(builder.Build);

        Assert.Equal("Initializer failed.", exception.Message);
        Assert.Equal(["failure"], calls);
    }

    [Fact]
    public async Task BuildFailureCancelsInitializationAndDisposesCreatedServicesAsynchronously()
    {
        var cleanupFailure = new InvalidOperationException("Cleanup failed.");
        var scopedDisposable = new AsyncOnlyDisposableService(cleanupFailure);
        var singletonDisposable = new AsyncOnlyDisposableService();
        var failure = new InvalidOperationException("Initializer failed.");
        CancellationToken initializationToken = default;
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());
        builder.Services.AddScoped(_ => scopedDisposable);
        builder.Services.AddSingleton(_ => singletonDisposable);
        builder.Services.AddSingleton<IHostInitializer>(
            new TestHostInitializer(
                "failure",
                0,
                [],
                exception: failure,
                servicesCallback: (services, token) =>
                {
                    initializationToken = token;
                    _ = services.GetServices<AsyncOnlyDisposableService>().ToArray();
                }));

        var exception = Assert.Throws<InvalidOperationException>(builder.Build);

        Assert.Same(failure, exception);
        Assert.True(initializationToken.IsCancellationRequested);

        await scopedDisposable.DisposeStarted.Task.TimeoutAfter(TimeSpan.FromSeconds(3));
        Assert.False(singletonDisposable.DisposeStarted.Task.IsCompleted);
        scopedDisposable.ContinueDisposal();
        await scopedDisposable.DisposeCompleted.Task.TimeoutAfter(TimeSpan.FromSeconds(3));

        await singletonDisposable.DisposeStarted.Task.TimeoutAfter(TimeSpan.FromSeconds(3));
        singletonDisposable.ContinueDisposal();
        await singletonDisposable.DisposeCompleted.Task.TimeoutAfter(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void BuildRejectsBaseUriThatChangedSinceBuilderCreation()
    {
        var jsMethods = new TestInternalJSImportMethods();
        var builder = new WebAssemblyHostBuilder(jsMethods);
        jsMethods.HostStartupValuesJson =
            """{"document.baseURI":"https://www.example.com/other/","location.href":"https://www.example.com/other/page"}""";

        var exception = Assert.Throws<InvalidOperationException>(builder.Build);

        Assert.Equal("The browser base URI changed during host initialization.", exception.Message);
    }

    [Fact]
    public async Task RunCancellationCancelsBuildTimeInitialization()
    {
        var calls = new List<string>();
        using var cancellationTokenSource = new CancellationTokenSource();
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());
        var initializerStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        builder.Services.AddSingleton<IHostInitializer>(
            new TestHostInitializer(
                "canceled",
                -300,
                calls,
                asyncCallback: async token =>
                {
                    initializerStarted.SetResult();
                    await Task.Delay(Timeout.InfiniteTimeSpan, token);
                }));
        var host = builder.Build();
        await initializerStarted.Task;
        var runTask = host.RunAsyncCore(
            cancellationTokenSource.Token,
            new TestSatelliteResourcesLoader());

        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);
        Assert.Equal(["canceled"], calls);
        await host.DisposeAsync();
    }

    [Fact]
    public async Task DisposeCancelsAndObservesBuildTimeInitialization()
    {
        var initializerStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken initializationToken = default;
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());
        builder.Services.AddSingleton<IHostInitializer>(
            new TestHostInitializer(
                "initializer",
                0,
                [],
                asyncCallback: async token =>
                {
                    initializationToken = token;
                    initializerStarted.SetResult();
                    await Task.Delay(Timeout.InfiniteTimeSpan, token);
                }));

        var host = builder.Build();
        await initializerStarted.Task;

        await host.DisposeAsync();

        Assert.True(initializationToken.IsCancellationRequested);
    }

    [Fact]
    public async Task DisposeWaitsForCancellationIgnoringInitializationBeforeDisposingServices()
    {
        var initializerStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var continueInitializer = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken initializationToken = default;
        var hostedServiceResolved = false;
        var hostedService = new TestHostedService();
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());
        builder.Services.AddSingleton(Mock.Of<IJSRuntime>());
        builder.Services.AddSingleton<IHostInitializer>(
            new TestHostInitializer(
                "initializer",
                0,
                [],
                asyncCallback: token =>
                {
                    initializationToken = token;
                    initializerStarted.SetResult();
                    return continueInitializer.Task;
                }));
        builder.Services.AddScoped<IHostedService>(_ =>
        {
            hostedServiceResolved = true;
            return hostedService;
        });

        var host = builder.Build();
        await initializerStarted.Task;
        using var cancellationTokenSource = new CancellationTokenSource();
        var runTask = host.RunAsyncCore(
            cancellationTokenSource.Token,
            new TestSatelliteResourcesLoader());

        var disposeTask = host.DisposeAsync().AsTask();

        Assert.True(initializationToken.IsCancellationRequested);
        Assert.False(disposeTask.IsCompleted);
        Assert.False(hostedServiceResolved);
        Assert.False(hostedService.StartCalled);

        continueInitializer.SetResult();
        await disposeTask.TimeoutAfter(TimeSpan.FromSeconds(3));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);

        Assert.False(hostedServiceResolved);
        Assert.False(hostedService.StartCalled);

        cancellationTokenSource.Cancel();
    }

    [Fact]
    public async Task RunAndDisposeSurfaceAsynchronousInitializationFailure()
    {
        var failInitializer = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var failure = new InvalidOperationException("Initializer failed asynchronously.");
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());
        builder.Services.AddSingleton<IHostInitializer>(
            new TestHostInitializer(
                "initializer",
                0,
                [],
                asyncCallback: _ => failInitializer.Task));
        var host = builder.Build();
        var runTask = host.RunAsyncCore(CancellationToken.None, new TestSatelliteResourcesLoader());

        failInitializer.SetException(failure);

        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => runTask));
        Assert.Same(
            failure,
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await host.DisposeAsync()));
    }

    [Fact]
    public void HostEnvironmentBaseAddressIsNormalizedBeforeBuild()
    {
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());

        Assert.Equal("https://www.example.com/", builder.HostEnvironment.BaseAddress);
    }

    // This won't happen in the product code, but we need to be able to safely call RunAsync
    // to be able to test a few of the other details.
    [Fact]
    public async Task RunAsync_CanExitBasedOnCancellationToken()
    {
        // Arrange
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());
        builder.Services.AddSingleton(Mock.Of<IJSRuntime>());
        var host = builder.Build();
        var cultureProvider = new TestSatelliteResourcesLoader();

        var cts = new CancellationTokenSource();

        // Act
        var task = host.RunAsyncCore(cts.Token, cultureProvider);

        cts.Cancel();
        await task.TimeoutAfter(TimeSpan.FromSeconds(3));

        // Assert (does not throw)
    }

    [Fact]
    public async Task RunAsync_CallingTwiceCausesException()
    {
        // Arrange
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());
        builder.Services.AddSingleton(Mock.Of<IJSRuntime>());
        var host = builder.Build();
        var cultureProvider = new TestSatelliteResourcesLoader();

        var cts = new CancellationTokenSource();
        var task = host.RunAsyncCore(cts.Token, cultureProvider);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => host.RunAsyncCore(cts.Token));

        cts.Cancel();
        await task.TimeoutAfter(TimeSpan.FromSeconds(3));

        // Assert
        Assert.Equal("The host has already started.", ex.Message);
    }

    [Fact]
    public async Task DisposeAsync_CanDisposeAfterCallingRunAsync()
    {
        // Arrange
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());
        builder.Services.AddSingleton(Mock.Of<IJSRuntime>());
        builder.Services.AddSingleton<DisposableService>();
        var host = builder.Build();
        var cultureProvider = new TestSatelliteResourcesLoader();

        var disposable = host.Services.GetRequiredService<DisposableService>();

        var cts = new CancellationTokenSource();

        // Act
        await using (host)
        {
            var task = host.RunAsyncCore(cts.Token, cultureProvider);

            cts.Cancel();
            await task.TimeoutAfter(TimeSpan.FromSeconds(3));
        }

        // Assert
        Assert.Equal(1, disposable.DisposeCount);
    }

    [Fact]
    public async Task RunAsync_StartsHostedServices()
    {
        // Arrange
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());
        builder.Services.AddSingleton(Mock.Of<IJSRuntime>());
        
        var testHostedService = new TestHostedService();
        builder.Services.AddSingleton<IHostedService>(testHostedService);
        
        var host = builder.Build();
        var cultureProvider = new TestSatelliteResourcesLoader();

        var cts = new CancellationTokenSource();

        // Act
        var task = host.RunAsyncCore(cts.Token, cultureProvider);
        
        // Give hosted services time to start
        await Task.Delay(100);
        cts.Cancel();
        await task.TimeoutAfter(TimeSpan.FromSeconds(3));

        // Assert
        Assert.True(testHostedService.StartCalled);
        Assert.NotEqual(cts.Token, testHostedService.StartToken);
        Assert.True(testHostedService.StartToken.IsCancellationRequested);
    }

    [Fact]
    public async Task DisposeAsync_StopsHostedServices()
    {
        // Arrange
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());
        builder.Services.AddSingleton(Mock.Of<IJSRuntime>());
        
        var testHostedService1 = new TestHostedService();
        var testHostedService2 = new TestHostedService();
        builder.Services.AddSingleton<IHostedService>(testHostedService1);
        builder.Services.AddSingleton<IHostedService>(testHostedService2);
        
        var host = builder.Build();
        var cultureProvider = new TestSatelliteResourcesLoader();

        var cts = new CancellationTokenSource();

        // Start the host to initialize hosted services
        var runTask = host.RunAsyncCore(cts.Token, cultureProvider);
        await Task.Delay(100);

        // Act - dispose the host
        await host.DisposeAsync();
        cts.Cancel();
        await runTask.TimeoutAfter(TimeSpan.FromSeconds(3));

        // Assert
        Assert.True(testHostedService1.StartCalled);
        Assert.True(testHostedService1.StopCalled);
        Assert.True(testHostedService2.StartCalled);
        Assert.True(testHostedService2.StopCalled);
    }

    [Fact]
    public async Task DisposeAsync_HandlesHostedServiceStopErrors()
    {
        // Arrange
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());
        builder.Services.AddSingleton(Mock.Of<IJSRuntime>());
        
        var goodService = new TestHostedService();
        var faultyService = new FaultyHostedService();
        builder.Services.AddSingleton<IHostedService>(goodService);
        builder.Services.AddSingleton<IHostedService>(faultyService);
        
        var host = builder.Build();
        var cultureProvider = new TestSatelliteResourcesLoader();

        var cts = new CancellationTokenSource();

        // Start the host to initialize hosted services
        var runTask = host.RunAsyncCore(cts.Token, cultureProvider);
        await Task.Delay(100);

        // Act & Assert - dispose should not throw even if hosted service fails
        await host.DisposeAsync();
        cts.Cancel();
        await runTask.TimeoutAfter(TimeSpan.FromSeconds(3));

        Assert.True(goodService.StartCalled);
        Assert.True(goodService.StopCalled);
        Assert.True(faultyService.StartCalled);
        Assert.True(faultyService.StopCalled);
    }

    [Fact]
    public async Task RunAsync_SupportsAddHostedServiceExtension()
    {
        // Arrange
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());
        builder.Services.AddSingleton(Mock.Of<IJSRuntime>());
        
        // Test manual hosted service registration (equivalent to AddHostedService)
        builder.Services.AddSingleton<TestHostedService>();
        builder.Services.AddSingleton<IHostedService>(serviceProvider => serviceProvider.GetRequiredService<TestHostedService>());
        
        var host = builder.Build();
        var cultureProvider = new TestSatelliteResourcesLoader();

        var cts = new CancellationTokenSource();

        // Act
        var task = host.RunAsyncCore(cts.Token, cultureProvider);
        
        // Give hosted services time to start
        await Task.Delay(100);
        cts.Cancel();
        await task.TimeoutAfter(TimeSpan.FromSeconds(3));

        // Assert - verify the hosted service was started via service collection
        var hostedServices = host.Services.GetServices<IHostedService>();
        Assert.Single(hostedServices);
        
        var testService = hostedServices.First();
        Assert.IsType<TestHostedService>(testService);
        Assert.True(((TestHostedService)testService).StartCalled);
    }

    private sealed class TestBrowserStartupValueProvider(params string[] keys) : IBrowserStartupValueProvider
    {
        public IReadOnlyList<string> Keys { get; } = keys;
    }

    private class TestHostedService : IHostedService
    {
        public bool StartCalled { get; private set; }
        public bool StopCalled { get; private set; }
        public CancellationToken StartToken { get; private set; }
        public CancellationToken StopToken { get; private set; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            StartCalled = true;
            StartToken = cancellationToken;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            StopCalled = true;
            StopToken = cancellationToken;
            return Task.CompletedTask;
        }
    }

    private class FaultyHostedService : IHostedService
    {
        public bool StartCalled { get; private set; }
        public bool StopCalled { get; private set; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            StartCalled = true;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            StopCalled = true;
            throw new InvalidOperationException("Simulated hosted service stop error");
        }
    }

    private sealed class TestHostInitializer(
        string name,
        int order,
        List<string> calls,
        bool browserPhase = false,
        Exception exception = null,
        Action<CancellationToken> callback = null,
        Func<CancellationToken, Task> asyncCallback = null,
        Action<IServiceProvider, CancellationToken> servicesCallback = null) : IHostInitializer
    {
        public int Order => order;

        public Task InitializeHostAsync(IServiceProvider services, CancellationToken cancellationToken = default)
            => browserPhase ? Task.CompletedTask : Invoke(services, cancellationToken);

        public Task InitializeBrowserAsync(IServiceProvider services, CancellationToken cancellationToken = default)
            => browserPhase ? Invoke(services, cancellationToken) : Task.CompletedTask;

        private Task Invoke(IServiceProvider services, CancellationToken cancellationToken)
        {
            calls.Add(name);
            callback?.Invoke(cancellationToken);
            servicesCallback?.Invoke(services, cancellationToken);

            return exception is not null
                ? Task.FromException(exception)
                : asyncCallback?.Invoke(cancellationToken) ?? Task.CompletedTask;
        }
    }

    private sealed class AsyncOnlyDisposableService(Exception disposeException = null) : IAsyncDisposable
    {
        private readonly TaskCompletionSource _continueDisposal =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource DisposeStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource DisposeCompleted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void ContinueDisposal() => _continueDisposal.SetResult();

        public async ValueTask DisposeAsync()
        {
            DisposeStarted.SetResult();
            await _continueDisposal.Task;
            DisposeCompleted.SetResult();

            if (disposeException is not null)
            {
                throw disposeException;
            }
        }
    }

    private class DisposableService : IAsyncDisposable
    {
        public int DisposeCount { get; private set; }

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return new ValueTask(Task.CompletedTask);
        }
    }

    private class TestSatelliteResourcesLoader : WebAssemblyCultureProvider
    {
        internal TestSatelliteResourcesLoader()
            : base(CultureInfo.CurrentCulture)
        {
        }

        public override ValueTask LoadCurrentCultureResourcesAsync() => default;
    }
}
