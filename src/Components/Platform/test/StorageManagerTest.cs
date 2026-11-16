// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Xunit;

namespace Microsoft.AspNetCore.Components.Platform.Tests;

public class StorageManagerTest
{
    [Fact]
    public async Task StorageManager_IsStableAndNotAcquiredBeforeUse()
    {
        var jsRuntime = new RecordingJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        var navigator = provider.GetRequiredService<IBrowserPlatform>().Window.Navigator;

        Assert.Same(navigator.Storage, navigator.Storage);
        Assert.Equal(0, jsRuntime.GetValueCallCount);
    }

    [Fact]
    public async Task StorageManager_ForwardsOperationsAndAcquiresReferenceOnce()
    {
        var jsRuntime = new RecordingJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        var storageManager = provider.GetRequiredService<IBrowserPlatform>().Window.Navigator.Storage;

        var persisted = await storageManager.PersistedAsync();
        var persist = await storageManager.PersistAsync();
        var estimate = await storageManager.EstimateAsync();

        Assert.False(persisted);
        Assert.True(persist);
        Assert.Same(jsRuntime.ObjectReference.Estimate, estimate);
        Assert.Equal(1, jsRuntime.GetValueCallCount);
        // A path from window, so the projection does not depend on the reference that Navigator holds.
        Assert.Equal("navigator.storage", jsRuntime.RequestedProperty);
        Assert.Equal(["persisted", "persist", "estimate"], jsRuntime.ObjectReference.Invocations);
    }

    [Fact]
    public async Task StorageManager_SharesPendingAcquisitionBetweenCallers()
    {
        var pending = new TaskCompletionSource<IJSObjectReference>();
        var jsRuntime = new RecordingJSRuntime { PendingNextGetValue = pending };
        await using var provider = CreateServiceProvider(jsRuntime);
        var storageManager = provider.GetRequiredService<IBrowserPlatform>().Window.Navigator.Storage;

        var first = storageManager.PersistedAsync().AsTask();
        var second = storageManager.EstimateAsync().AsTask();
        pending.SetResult(jsRuntime.ObjectReference);
        await Task.WhenAll(first, second);

        Assert.Equal(1, jsRuntime.GetValueCallCount);
        Assert.Equal(2, jsRuntime.ObjectReference.Invocations.Count);
    }

    [Fact]
    public async Task StorageManager_ReacquiresReferenceAfterAcquisitionIsCanceled()
    {
        var jsRuntime = new RecordingJSRuntime { CancelNextGetValue = true };
        await using var provider = CreateServiceProvider(jsRuntime);
        var storageManager = provider.GetRequiredService<IBrowserPlatform>().Window.Navigator.Storage;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => storageManager.PersistedAsync().AsTask());

        Assert.False(await storageManager.PersistedAsync());
        Assert.Equal(2, jsRuntime.GetValueCallCount);
    }

    [Fact]
    public async Task StorageManager_ThrowsNotSupportedExceptionWhenTheBrowserHasNoStorageManager()
    {
        // JS interop throws this for a missing navigator.storage, such as on a page that is not a secure context.
        var missingProperty = new JSException("The property 'storage' is not defined or is not readable.");
        var jsRuntime = new RecordingJSRuntime { GetValueException = missingProperty };
        var provider = CreateServiceProvider(jsRuntime);
        var storageManager = provider.GetRequiredService<IBrowserPlatform>().Window.Navigator.Storage;
        Func<Task>[] members =
        [
            () => storageManager.PersistedAsync().AsTask(),
            () => storageManager.PersistAsync().AsTask(),
            () => storageManager.EstimateAsync().AsTask(),
        ];

        foreach (var member in members)
        {
            var exception = await Assert.ThrowsAsync<NotSupportedException>(member);
            Assert.Same(missingProperty, exception.InnerException);
        }
        // Disposal must not throw for the failed acquisitions.
        await provider.DisposeAsync();

        Assert.Equal(members.Length, jsRuntime.GetValueCallCount);
        Assert.Empty(jsRuntime.ObjectReference.Invocations);
    }

    [Fact]
    public async Task StorageManager_ThrowsNotSupportedExceptionWhenStorageManagerIsNull()
    {
        var jsRuntime = new RecordingJSRuntime { StorageManagerNull = true };
        var provider = CreateServiceProvider(jsRuntime);
        var storageManager = provider.GetRequiredService<IBrowserPlatform>().Window.Navigator.Storage;

        await Assert.ThrowsAsync<NotSupportedException>(() => storageManager.PersistedAsync().AsTask());
        await Assert.ThrowsAsync<NotSupportedException>(() => storageManager.EstimateAsync().AsTask());
        // Disposal must not throw for the failed acquisitions.
        await provider.DisposeAsync();

        Assert.Equal(2, jsRuntime.GetValueCallCount);
        Assert.Empty(jsRuntime.ObjectReference.Invocations);
    }

    [Fact]
    public async Task StorageManager_PassesThroughOtherAcquisitionFailuresAndReacquires()
    {
        var disconnected = new JSDisconnectedException("The circuit is disconnected.");
        var jsRuntime = new RecordingJSRuntime { GetValueException = disconnected };
        await using var provider = CreateServiceProvider(jsRuntime);
        var storageManager = provider.GetRequiredService<IBrowserPlatform>().Window.Navigator.Storage;

        var exception = await Assert.ThrowsAsync<JSDisconnectedException>(() => storageManager.PersistedAsync().AsTask());
        jsRuntime.GetValueException = null;

        Assert.Same(disconnected, exception);
        Assert.False(await storageManager.PersistedAsync());
        Assert.Equal(2, jsRuntime.GetValueCallCount);
    }

    [Fact]
    public async Task ScopeDisposal_DisposesTheAcquiredReference()
    {
        var jsRuntime = new RecordingJSRuntime();
        var provider = CreateServiceProvider(jsRuntime);

        await using (provider)
        {
            await provider.GetRequiredService<IBrowserPlatform>().Window.Navigator.Storage.PersistedAsync();
            Assert.False(jsRuntime.ObjectReference.Disposed);
        }

        Assert.True(jsRuntime.ObjectReference.Disposed);
    }

    [Fact]
    public async Task ScopeDisposal_MakesStorageManagerUnusable()
    {
        var jsRuntime = new RecordingJSRuntime();
        var provider = CreateServiceProvider(jsRuntime);
        var storageManager = provider.GetRequiredService<IBrowserPlatform>().Window.Navigator.Storage;
        await storageManager.PersistedAsync();

        await provider.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => storageManager.PersistedAsync().AsTask());
        await Assert.ThrowsAsync<ObjectDisposedException>(() => storageManager.PersistAsync().AsTask());
        await Assert.ThrowsAsync<ObjectDisposedException>(() => storageManager.EstimateAsync().AsTask());
        Assert.Equal(1, jsRuntime.GetValueCallCount);
    }

    [Fact]
    public async Task ScopeDisposal_DoesNotWaitForPendingAcquisition()
    {
        var neverCompletes = new TaskCompletionSource<IJSObjectReference>();
        var jsRuntime = new RecordingJSRuntime { PendingNextGetValue = neverCompletes };
        var provider = CreateServiceProvider(jsRuntime);
        var storageManager = provider.GetRequiredService<IBrowserPlatform>().Window.Navigator.Storage;

        _ = storageManager.PersistedAsync();

        await provider.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(jsRuntime.ObjectReference.Disposed);
    }

    // A mock IJSRuntime returns .NET objects, so the tests below run the real deserializer on the JSON the browser sends.
    [Fact]
    public async Task EstimateAsync_ReadsUsageAndQuotaAndIgnoresOtherMembers()
    {
        // Chromium also returns usageDetails, which is not in the specification.
        var jsRuntime = new SerializingJSRuntime
        {
            EstimateResult = """{"quota":299977904947,"usage":4096,"usageDetails":{"indexedDB":4096}}""",
        };
        await using var provider = CreateServiceProvider(jsRuntime);
        var storageManager = provider.GetRequiredService<IBrowserPlatform>().Window.Navigator.Storage;

        var estimate = await storageManager.EstimateAsync();

        Assert.Equal((4096ul, 299977904947ul), (estimate.Usage, estimate.Quota));
    }

    [Fact]
    public async Task EstimateAsync_ReadsMissingMembersAsNull()
    {
        var jsRuntime = new SerializingJSRuntime { EstimateResult = "{}" };
        await using var provider = CreateServiceProvider(jsRuntime);
        var storageManager = provider.GetRequiredService<IBrowserPlatform>().Window.Navigator.Storage;

        var estimate = await storageManager.EstimateAsync();

        Assert.Null(estimate.Usage);
        Assert.Null(estimate.Quota);
    }

    private static ServiceProvider CreateServiceProvider(IJSRuntime jsRuntime)
    {
        var services = new ServiceCollection();
        services.AddSingleton(jsRuntime);
        services.AddBrowserPlatform();

        return services.BuildServiceProvider();
    }

    private sealed class RecordingJSRuntime : IJSRuntime
    {
        public RecordingJSObjectReference ObjectReference { get; } = new();

        public int GetValueCallCount { get; private set; }

        public string? RequestedProperty { get; private set; }

        public bool CancelNextGetValue { get; set; }

        public bool StorageManagerNull { get; set; }

        public Exception? GetValueException { get; set; }

        public TaskCompletionSource<IJSObjectReference>? PendingNextGetValue { get; set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            throw new NotSupportedException();
        }

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            throw new NotSupportedException();
        }

        public ValueTask<TValue> GetValueAsync<TValue>(string identifier)
        {
            GetValueCallCount++;
            RequestedProperty = identifier;

            if (CancelNextGetValue)
            {
                CancelNextGetValue = false;

                return ValueTask.FromCanceled<TValue>(new CancellationToken(canceled: true));
            }

            if (PendingNextGetValue is { } pending)
            {
                PendingNextGetValue = null;

                return new ValueTask<TValue>((Task<TValue>)(object)pending.Task);
            }

            if (GetValueException is { } exception)
            {
                return ValueTask.FromException<TValue>(exception);
            }

            // JS interop returns a null reference when navigator.storage exists and holds null or undefined.
            return ValueTask.FromResult(StorageManagerNull ? default! : (TValue)(object)ObjectReference);
        }
    }

    private sealed class RecordingJSObjectReference : IJSObjectReference
    {
        public StorageEstimate Estimate { get; } = new() { Usage = 1, Quota = 2 };

        public List<string> Invocations { get; } = [];

        public bool Disposed { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            return InvokeAsync<TValue>(identifier, CancellationToken.None, args);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            Assert.Empty(args ?? []);
            Invocations.Add(identifier);

            object result = identifier switch
            {
                "persisted" => false,
                "persist" => true,
                "estimate" => Estimate,
                _ => throw new InvalidOperationException($"Unexpected method '{identifier}'."),
            };

            return ValueTask.FromResult((TValue)result);
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;

            return ValueTask.CompletedTask;
        }
    }

    // Runs the real JSRuntime serializer and answers each call as the browser would.
    private sealed class SerializingJSRuntime : JSRuntime
    {
        public string EstimateResult { get; init; } = "{}";

        protected override void BeginInvokeJS(in JSInvocationInfo invocationInfo)
        {
            var result = invocationInfo.Identifier switch
            {
                "navigator.storage" => """{"__jsObjectId":1}""",
                "estimate" => EstimateResult,
                _ => "null",
            };

            if (invocationInfo.AsyncHandle != 0)
            {
                DotNetDispatcher.EndInvokeJS(this, $"[{invocationInfo.AsyncHandle},true,{result}]");
            }
        }

        protected override void BeginInvokeJS(long taskId, string identifier, string? argsJson, JSCallResultType resultType, long targetInstanceId)
            => throw new NotSupportedException();

        protected override void EndInvokeDotNet(DotNetInvocationInfo invocationInfo, in DotNetInvocationResult invocationResult)
            => throw new NotSupportedException();
    }
}
