// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Xunit;

namespace Microsoft.AspNetCore.Components.Platform.Tests;

public class LockManagerTest
{
    [Fact]
    public async Task Locks_IsStableAndNotAcquiredBeforeUse()
    {
        var jsRuntime = new RecordingJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        var navigator = provider.GetRequiredService<IBrowserPlatform>().Window.Navigator;

        Assert.Same(navigator.Locks, navigator.Locks);
        Assert.Equal(0, jsRuntime.GetValueCallCount);
    }

    [Fact]
    public async Task Locks_IsSharedWithinAScopeAndDistinctAcrossScopes()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IJSRuntime>(new RecordingJSRuntime());
        services.AddBrowserPlatform();
        await using var provider = services.BuildServiceProvider();
        await using var firstScope = provider.CreateAsyncScope();
        await using var secondScope = provider.CreateAsyncScope();

        var first = firstScope.ServiceProvider.GetRequiredService<IBrowserPlatform>().Window.Navigator.Locks;

        Assert.Same(first, firstScope.ServiceProvider.GetRequiredService<IBrowserPlatform>().Window.Navigator.Locks);
        Assert.NotSame(first, secondScope.ServiceProvider.GetRequiredService<IBrowserPlatform>().Window.Navigator.Locks);
    }

    [Fact]
    public async Task QueryAsync_CallsQueryAndAcquiresReferenceOnce()
    {
        var jsRuntime = new RecordingJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        var locks = provider.GetRequiredService<IBrowserPlatform>().Window.Navigator.Locks;

        var snapshot = await locks.QueryAsync();
        await locks.QueryAsync();

        var held = Assert.Single(snapshot.Held!.Value);
        Assert.Equal(("resource", LockMode.Exclusive, "client"), (held.Name, held.Mode, held.ClientId));
        Assert.Empty(snapshot.Pending!.Value);
        Assert.Equal(1, jsRuntime.GetValueCallCount);
        Assert.Equal("navigator.locks", jsRuntime.RequestedProperty);
        Assert.Equal(["query", "query"], jsRuntime.ObjectReference.Invocations.Select(invocation => invocation.Identifier));
        Assert.All(jsRuntime.ObjectReference.Invocations, invocation => Assert.Empty(invocation.Args));
    }

    [Fact]
    public async Task QueryAsync_SharesPendingAcquisitionBetweenCallers()
    {
        var pending = new TaskCompletionSource<IJSObjectReference>();
        var jsRuntime = new RecordingJSRuntime { PendingNextGetValue = pending };
        await using var provider = CreateServiceProvider(jsRuntime);
        var locks = provider.GetRequiredService<IBrowserPlatform>().Window.Navigator.Locks;

        var first = locks.QueryAsync().AsTask();
        var second = locks.QueryAsync().AsTask();
        pending.SetResult(jsRuntime.ObjectReference);
        await Task.WhenAll(first, second);

        Assert.Equal(1, jsRuntime.GetValueCallCount);
        Assert.Equal(2, jsRuntime.ObjectReference.Invocations.Count);
    }

    [Fact]
    public async Task QueryAsync_ReacquiresReferenceAfterAcquisitionIsCanceled()
    {
        var jsRuntime = new RecordingJSRuntime { CancelNextGetValue = true };
        await using var provider = CreateServiceProvider(jsRuntime);
        var locks = provider.GetRequiredService<IBrowserPlatform>().Window.Navigator.Locks;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => locks.QueryAsync().AsTask());

        await locks.QueryAsync();
        Assert.Equal(2, jsRuntime.GetValueCallCount);
    }

    [Fact]
    public async Task QueryAsync_ThrowsNotSupportedExceptionWhenTheBrowserHasNoLockManager()
    {
        // JS interop throws this for a missing navigator.locks, such as on an insecure page.
        var missingProperty = new JSException("The property 'locks' is not defined or is not readable.");
        var jsRuntime = new RecordingJSRuntime { GetValueException = missingProperty };
        var provider = CreateServiceProvider(jsRuntime);
        var locks = provider.GetRequiredService<IBrowserPlatform>().Window.Navigator.Locks;

        var exception = await Assert.ThrowsAsync<NotSupportedException>(() => locks.QueryAsync().AsTask());
        // Disposal must not throw for the failed acquisition.
        await provider.DisposeAsync();

        Assert.Same(missingProperty, exception.InnerException);
        Assert.Empty(jsRuntime.ObjectReference.Invocations);
    }

    [Fact]
    public async Task QueryAsync_ThrowsNotSupportedExceptionWhenLockManagerIsNull()
    {
        var jsRuntime = new RecordingJSRuntime { ReturnNullReference = true };
        var provider = CreateServiceProvider(jsRuntime);
        var locks = provider.GetRequiredService<IBrowserPlatform>().Window.Navigator.Locks;

        await Assert.ThrowsAsync<NotSupportedException>(() => locks.QueryAsync().AsTask());
        // The failed acquisition is not kept, and disposal must not throw for it.
        await Assert.ThrowsAsync<NotSupportedException>(() => locks.QueryAsync().AsTask());
        await provider.DisposeAsync();

        Assert.Equal(2, jsRuntime.GetValueCallCount);
        Assert.Empty(jsRuntime.ObjectReference.Invocations);
    }

    [Fact]
    public async Task QueryAsync_PassesThroughOtherAcquisitionFailuresAndReacquires()
    {
        var disconnected = new JSDisconnectedException("The circuit is disconnected.");
        var jsRuntime = new RecordingJSRuntime { GetValueException = disconnected };
        await using var provider = CreateServiceProvider(jsRuntime);
        var locks = provider.GetRequiredService<IBrowserPlatform>().Window.Navigator.Locks;

        var exception = await Assert.ThrowsAsync<JSDisconnectedException>(() => locks.QueryAsync().AsTask());
        jsRuntime.GetValueException = null;

        Assert.Same(disconnected, exception);
        await locks.QueryAsync();
        Assert.Equal(2, jsRuntime.GetValueCallCount);
    }

    [Fact]
    public async Task ScopeDisposal_DisposesTheAcquiredReference()
    {
        var jsRuntime = new RecordingJSRuntime();
        var provider = CreateServiceProvider(jsRuntime);

        await using (provider)
        {
            await provider.GetRequiredService<IBrowserPlatform>().Window.Navigator.Locks.QueryAsync();
            Assert.False(jsRuntime.ObjectReference.Disposed);
        }

        Assert.True(jsRuntime.ObjectReference.Disposed);
    }

    [Fact]
    public async Task ScopeDisposal_MakesLockManagerUnusable()
    {
        var jsRuntime = new RecordingJSRuntime();
        var provider = CreateServiceProvider(jsRuntime);
        var locks = provider.GetRequiredService<IBrowserPlatform>().Window.Navigator.Locks;
        await locks.QueryAsync();

        await provider.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => locks.QueryAsync().AsTask());
        await Assert.ThrowsAsync<ObjectDisposedException>(() => locks.RequestAsync("resource", _ => Task.CompletedTask).AsTask());
        Assert.Equal(1, jsRuntime.GetValueCallCount);
    }

    [Fact]
    public async Task ScopeDisposal_DoesNotWaitForPendingAcquisition()
    {
        var jsRuntime = new RecordingJSRuntime { PendingNextGetValue = new TaskCompletionSource<IJSObjectReference>() };
        var provider = CreateServiceProvider(jsRuntime);

        _ = provider.GetRequiredService<IBrowserPlatform>().Window.Navigator.Locks.QueryAsync();

        await provider.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(jsRuntime.ObjectReference.Disposed);
    }

    [Fact]
    public async Task RequestAsync_HoldsTheLockUntilTheCallbackCompletes()
    {
        var jsRuntime = new BrowserJSRuntime();
        jsRuntime.HoldCallsTo("request");
        await using var provider = CreateServiceProvider(jsRuntime);
        var locks = provider.GetRequiredService<IBrowserPlatform>().Window.Navigator.Locks;
        var callbackCompletion = new TaskCompletionSource();
        Lock? received = null;

        var request = locks.RequestAsync("resource", grantedLock =>
        {
            received = grantedLock;
            return callbackCompletion.Task;
        }).AsTask();
        // The browser grants the lock and waits for the promise of the callback.
        var callback = jsRuntime.InvokeLastCallbackAsync("""[{"name":"resource","mode":"exclusive"}]""");

        Assert.False(callback.IsCompleted);
        callbackCompletion.SetResult();
        Assert.True((await callback).Success);
        Assert.False(request.IsCompleted);
        jsRuntime.ReleaseHeldCalls("request");
        await request;

        // navigator.locks is object 1, the module is object 2, and the function is object 3.
        Assert.Equal(("resource", LockMode.Exclusive), (received!.Name, received.Mode));
        using var createCallbackArgs = JsonDocument.Parse(Assert.Single(jsRuntime.CallsTo("createCallback")).ArgsJson);
        Assert.Equal("""[{"name":null,"mode":null}]""", createCallbackArgs.RootElement[1].GetString());
        var call = Assert.Single(jsRuntime.CallsTo("request"));
        Assert.Equal((JSCallType.FunctionCall, 1, """["resource",{"__jsObjectId":3}]"""), (call.CallType, call.TargetInstanceId, call.ArgsJson));
        Assert.Equal(["[3]"], jsRuntime.CallsTo("DotNet.disposeJSObjectReferenceById").Select(dispose => dispose.ArgsJson));
    }

    [Theory]
    [InlineData(null, false, false, """{"mode":"exclusive","ifAvailable":false,"steal":false}""")]
    [InlineData("shared", true, false, """{"mode":"shared","ifAvailable":true,"steal":false}""")]
    [InlineData("exclusive", false, true, """{"mode":"exclusive","ifAvailable":false,"steal":true}""")]
    [InlineData("future-mode", false, false, """{"mode":"future-mode","ifAvailable":false,"steal":false}""")]
    public async Task RequestAsync_SendsTheOptions(string? mode, bool ifAvailable, bool steal, string expectedJson)
    {
        var jsRuntime = new BrowserJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        var options = new LockOptions { IfAvailable = ifAvailable, Steal = steal };
        if (mode is not null)
        {
            options.Mode = new LockMode(mode);
        }

        await provider.GetRequiredService<IBrowserPlatform>().Window.Navigator.Locks.RequestAsync("resource", options, _ => Task.CompletedTask);

        Assert.Equal($$"""["resource",{{expectedJson}},{"__jsObjectId":3}]""", Assert.Single(jsRuntime.CallsTo("request")).ArgsJson);
    }

    [Fact]
    public async Task RequestAsync_CallsTheCallbackWithNullWhenTheLockIsNotGranted()
    {
        var jsRuntime = new BrowserJSRuntime();
        jsRuntime.HoldCallsTo("request");
        await using var provider = CreateServiceProvider(jsRuntime);
        var locks = provider.GetRequiredService<IBrowserPlatform>().Window.Navigator.Locks;
        var calls = 0;
        Lock? received = new();

        var request = locks.RequestAsync("resource", new LockOptions { IfAvailable = true }, grantedLock =>
        {
            calls++;
            received = grantedLock;
            return Task.CompletedTask;
        }).AsTask();
        Assert.True((await jsRuntime.InvokeLastCallbackAsync("[null]")).Success);
        jsRuntime.ReleaseHeldCalls("request");
        await request;

        Assert.Equal(1, calls);
        Assert.Null(received);
    }

    [Fact]
    public async Task RequestAsync_ThrowsTheExceptionOfTheCallback()
    {
        var jsRuntime = new BrowserJSRuntime();
        jsRuntime.HoldCallsTo("request");
        await using var provider = CreateServiceProvider(jsRuntime);
        var locks = provider.GetRequiredService<IBrowserPlatform>().Window.Navigator.Locks;
        var callbackException = new InvalidOperationException("The work failed.");

        var request = locks.RequestAsync("resource", _ => throw callbackException).AsTask();
        // The rejected promise of the callback makes the browser release the lock and reject the request.
        Assert.False((await jsRuntime.InvokeLastCallbackAsync("""[{"name":"resource","mode":"exclusive"}]""")).Success);
        jsRuntime.ReleaseHeldCalls("request", succeeded: false);

        Assert.Same(callbackException, await Assert.ThrowsAsync<InvalidOperationException>(() => request));
        Assert.Equal(["[3]"], jsRuntime.CallsTo("DotNet.disposeJSObjectReferenceById").Select(dispose => dispose.ArgsJson));
    }

    [Fact]
    public async Task RequestAsync_PassesThroughARejectedRequestAndReleasesTheFunction()
    {
        // The browser rejects a request without calling the callback, such as for a name that starts with "-".
        var jsRuntime = new BrowserJSRuntime();
        jsRuntime.FailNextCallTo("request");
        await using var provider = CreateServiceProvider(jsRuntime);
        var locks = provider.GetRequiredService<IBrowserPlatform>().Window.Navigator.Locks;

        await Assert.ThrowsAsync<JSException>(() => locks.RequestAsync("-resource", _ => Task.CompletedTask).AsTask());

        Assert.Equal(["[3]"], jsRuntime.CallsTo("DotNet.disposeJSObjectReferenceById").Select(dispose => dispose.ArgsJson));
    }

    [Fact]
    public async Task RequestAsync_DoesNotSendTheDefaultLockMode()
    {
        var jsRuntime = new BrowserJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        var locks = provider.GetRequiredService<IBrowserPlatform>().Window.Navigator.Locks;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => locks.RequestAsync("resource", new LockOptions { Mode = default }, _ => Task.CompletedTask).AsTask());

        Assert.Empty(jsRuntime.CallsTo("request"));
        Assert.Equal(["[3]"], jsRuntime.CallsTo("DotNet.disposeJSObjectReferenceById").Select(dispose => dispose.ArgsJson));
    }

    [Fact]
    public async Task RequestAsync_ThrowsNotSupportedExceptionWithoutCreatingTheCallbackWhenTheBrowserHasNoLockManager()
    {
        var jsRuntime = new BrowserJSRuntime();
        jsRuntime.FailNextCallTo("navigator.locks");
        await using var provider = CreateServiceProvider(jsRuntime);
        var locks = provider.GetRequiredService<IBrowserPlatform>().Window.Navigator.Locks;

        var exception = await Assert.ThrowsAsync<NotSupportedException>(() => locks.RequestAsync("resource", _ => Task.CompletedTask).AsTask());

        Assert.IsType<JSException>(exception.InnerException);
        Assert.Empty(jsRuntime.CallsTo("createCallback"));
    }

    [Fact]
    public async Task RequestAsync_ThrowsForNullArguments()
    {
        await using var provider = CreateServiceProvider(new BrowserJSRuntime());
        var locks = provider.GetRequiredService<IBrowserPlatform>().Window.Navigator.Locks;

        await Assert.ThrowsAsync<ArgumentNullException>("name", () => locks.RequestAsync(null!, _ => Task.CompletedTask).AsTask());
        await Assert.ThrowsAsync<ArgumentNullException>("callback", () => locks.RequestAsync("resource", null!).AsTask());
        await Assert.ThrowsAsync<ArgumentNullException>("name", () => locks.RequestAsync(null!, new LockOptions(), _ => Task.CompletedTask).AsTask());
        await Assert.ThrowsAsync<ArgumentNullException>("options", () => locks.RequestAsync("resource", null!, _ => Task.CompletedTask).AsTask());
        await Assert.ThrowsAsync<ArgumentNullException>("callback", () => locks.RequestAsync("resource", new LockOptions(), null!).AsTask());
    }

    // A mock IJSRuntime returns .NET objects as they are, so the tests below run the real JSRuntime serializer.
    [Fact]
    public async Task QueryAsync_ReadsEveryMemberOfTheSnapshot()
    {
        var jsRuntime = new SerializingJSRuntime
        {
            QueryResult = """
                {
                    "held": [{ "name": "a", "mode": "exclusive", "clientId": "client-1" }],
                    "pending": [
                        { "name": "b", "mode": "shared", "clientId": "client-2" },
                        { "name": "c", "mode": "future-mode", "clientId": "client-3" }
                    ]
                }
                """,
        };
        await using var provider = CreateServiceProvider(jsRuntime);

        var snapshot = await provider.GetRequiredService<IBrowserPlatform>().Window.Navigator.Locks.QueryAsync();

        Assert.Equal(
            [("a", LockMode.Exclusive, "client-1")],
            snapshot.Held!.Value.Select(info => (info.Name, info.Mode, info.ClientId)));
        Assert.Equal(
            [("b", LockMode.Shared, "client-2"), ("c", new LockMode("future-mode"), "client-3")],
            snapshot.Pending!.Value.Select(info => (info.Name, info.Mode, info.ClientId)));
        Assert.Equal(["[]"], jsRuntime.ArgumentsSentTo("query"));
    }

    [Fact]
    public async Task QueryAsync_ReturnsNullForMissingMembers()
    {
        var jsRuntime = new SerializingJSRuntime { QueryResult = """{ "held": [{}] }""" };
        await using var provider = CreateServiceProvider(jsRuntime);

        var snapshot = await provider.GetRequiredService<IBrowserPlatform>().Window.Navigator.Locks.QueryAsync();

        var held = Assert.Single(snapshot.Held!.Value);
        Assert.Equal((null, null, null), (held.Name, held.Mode, held.ClientId));
        Assert.Null(snapshot.Pending);
    }

    [Fact]
    public async Task LockMode_SendsEachValueAsTheWebIdlString()
    {
        var jsRuntime = new SerializingJSRuntime();

        await jsRuntime.InvokeVoidAsync("send", LockMode.Shared);
        await jsRuntime.InvokeVoidAsync("send", LockMode.Exclusive);
        await jsRuntime.InvokeVoidAsync("send", new LockMode("future-mode"));

        Assert.Equal(["""["shared"]""", """["exclusive"]""", """["future-mode"]"""], jsRuntime.ArgumentsSentTo("send"));
    }

    [Fact]
    public async Task LockMode_DefaultHasNoValueAndIsNotSent()
    {
        var jsRuntime = new SerializingJSRuntime();
        var defaultValue = default(LockMode);

        Assert.Throws<InvalidOperationException>(() => defaultValue.Value);
        Assert.Equal("", defaultValue.ToString());
        Assert.True(defaultValue.Equals((object)default(LockMode)));
        Assert.True(defaultValue == new LockMode());
        Assert.Equal(0, defaultValue.GetHashCode());
        Assert.True(defaultValue != LockMode.Shared);
        Assert.True(defaultValue != LockMode.Exclusive);
        Assert.True(defaultValue != new LockMode(""));
        await Assert.ThrowsAsync<InvalidOperationException>(() => jsRuntime.InvokeVoidAsync("send", defaultValue).AsTask());
        Assert.Empty(jsRuntime.ArgumentsSentTo("send"));
    }

    [Fact]
    public void LockMode_ComparesByOrdinalString()
    {
        Assert.Equal(LockMode.Shared, new LockMode("shared"));
        Assert.True(LockMode.Shared == new LockMode("shared"));
        Assert.True(LockMode.Shared != new LockMode("Shared"));
        Assert.Equal(LockMode.Exclusive.GetHashCode(), new LockMode("exclusive").GetHashCode());
        Assert.True(LockMode.Exclusive.Equals((object)new LockMode("exclusive")));
        Assert.False(LockMode.Exclusive.Equals((object)"exclusive"));
        Assert.False(LockMode.Exclusive.Equals(null));
        Assert.Equal("exclusive", LockMode.Exclusive.ToString());
        Assert.Throws<ArgumentNullException>("value", () => new LockMode(null!));
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

        public bool ReturnNullReference { get; set; }

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

            // JS interop returns a null reference when navigator.locks exists and holds null or undefined.
            return ValueTask.FromResult(ReturnNullReference ? default! : (TValue)(object)ObjectReference);
        }
    }

    private sealed class RecordingJSObjectReference : IJSObjectReference
    {
        // Callers can resume concurrently after the shared acquisition completes.
        public ConcurrentQueue<Invocation> Invocations { get; } = new();

        public bool Disposed { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Invocations.Enqueue(new Invocation(identifier, args ?? []));

            object result = identifier switch
            {
                "query" => new LockManagerSnapshotPayload
                {
                    Held = [new LockInfo { Name = "resource", Mode = LockMode.Exclusive, ClientId = "client" }],
                    Pending = [],
                },
                _ => throw new InvalidOperationException($"Unexpected method '{identifier}'."),
            };

            return ValueTask.FromResult((TValue)result);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            throw new NotSupportedException();
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;

            return ValueTask.CompletedTask;
        }
    }

    private sealed record Invocation(string Identifier, object?[] Args);

    // Runs the real JSRuntime serializer and answers each call as the browser would.
    private sealed class SerializingJSRuntime : JSRuntime
    {
        private readonly List<(string Identifier, string? ArgsJson)> _calls = [];

        public string QueryResult { get; init; } = """{ "held": [], "pending": [] }""";

        public string[] ArgumentsSentTo(string identifier)
            => [.. _calls.Where(call => call.Identifier == identifier).Select(call => call.ArgsJson ?? string.Empty)];

        protected override void BeginInvokeJS(in JSInvocationInfo invocationInfo)
        {
            _calls.Add((invocationInfo.Identifier, invocationInfo.ArgsJson));

            var result = invocationInfo.Identifier switch
            {
                "navigator.locks" => """{"__jsObjectId":1}""",
                "query" => QueryResult,
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
