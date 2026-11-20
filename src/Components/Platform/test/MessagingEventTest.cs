// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Xunit;

namespace Microsoft.AspNetCore.Components.Platform.Tests;

public class MessagingEventTest
{
    private const string Message = """[{"data":{"json":"{\"__jsObjectId\":\"data\",\"nested\":[null,true,42]}"},"origin":"https://example.test"}]""";

    [Fact]
    public void EventKeys_HaveNativeNamesAndAreStable()
    {
        Assert.Equal("message", MessagePort.MessageEvent.Name);
        Assert.Equal("messageerror", MessagePort.MessageErrorEvent.Name);
        Assert.Equal("message", BroadcastChannel.MessageEvent.Name);
        Assert.Equal("messageerror", BroadcastChannel.MessageErrorEvent.Name);
        Assert.Same(MessagePort.MessageEvent, MessagePort.MessageEvent);
        Assert.Same(BroadcastChannel.MessageErrorEvent, BroadcastChannel.MessageErrorEvent);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MessageErrorEvent_ProjectsNativeNullAndOrigin(bool broadcast)
    {
        var runtime = new BrowserJSRuntime();
        await using var provider = CreateProvider(runtime);
        var (target, add) = await CreateTargetAsync(provider, broadcast, messageError: true);
        await using var owned = target;
        MessageEvent? received = null;
        await using var listener = await add(message =>
        {
            received = message;
            return Task.CompletedTask;
        }, CancellationToken.None);

        await runtime.InvokeLastCallbackAsync("""[{"data":{"json":"null"},"origin":"https://example.test"}]""");

        Assert.NotNull(received);
        Assert.Equal(JsonValueKind.Null, received.Data.ValueKind);
        Assert.Equal("https://example.test", received.Origin);
        Assert.Contains("\"messageerror\"", Assert.Single(runtime.CallsTo("addEventListener")).ArgsJson);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RemovalFailure_IsLoggedAndDoesNotPreventTargetCleanup(bool broadcast)
    {
        var runtime = new BrowserJSRuntime();
        var logger = new RecordingLoggerFactory();
        await using var provider = CreateProvider(runtime, logger);
        var (target, add) = await CreateTargetAsync(provider, broadcast);
        await add(_ => Task.CompletedTask, CancellationToken.None);
        await add(_ => Task.CompletedTask, CancellationToken.None);
        runtime.FailNextCallTo("removeEventListener");

        await target.DisposeAsync();

        Assert.Equal(2, runtime.CallsTo("removeEventListener").Count());
        Assert.Single(runtime.CallsTo("close"));
        Assert.False((await runtime.InvokeCallbackAsync(0, Message)).Success);
        Assert.False((await runtime.InvokeCallbackAsync(1, Message)).Success);
        Assert.Equal("ListenerRemovalFailed", Assert.Single(logger.Entries).EventId.Name);
    }

    [Theory]
    [InlineData(false, "import")]
    [InlineData(true, "import")]
    [InlineData(false, "addEventListener")]
    [InlineData(true, "addEventListener")]
    public async Task CancellationDuringRegistration_RemovesListenerAndPreservesToken(bool broadcast, string heldCall)
    {
        var runtime = new BrowserJSRuntime();
        await using var provider = CreateProvider(runtime);
        var (target, add) = await CreateTargetAsync(provider, broadcast);
        await using var owned = target;
        using var lifetime = new CancellationTokenSource();
        runtime.HoldCallsTo(heldCall);
        var adding = add(_ => Task.CompletedTask, lifetime.Token).AsTask();

        lifetime.Cancel();
        runtime.ReleaseHeldCalls(heldCall);

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => adding);
        Assert.Equal(lifetime.Token, exception.CancellationToken);
        Assert.False((await runtime.InvokeLastCallbackAsync(Message)).Success);
    }

    [Fact]
    public async Task ScopeDisposal_PreservesRendererSynchronizationContextForCallerOwnedTargets()
    {
        var runtime = new BrowserJSRuntime { CompleteOnThreadPool = true };
        var provider = CreateProvider(runtime);
        var window = provider.GetRequiredService<IBrowserPlatform>().Window;
        var dispatcher = Dispatcher.CreateDefault();
        var context = await dispatcher.InvokeAsync(() => SynchronizationContext.Current);
        await using var target = await window.BroadcastChannel.CreateAsync("test");
        await dispatcher.InvokeAsync(async () =>
        {
            await window.AddEventListenerAsync(Window.StorageEvent, _ => Task.CompletedTask);
            await window.CookieStore.AddEventListenerAsync(CookieStore.ChangeEvent, _ => Task.CompletedTask);
            await target.AddEventListenerAsync(BroadcastChannel.MessageEvent, _ => Task.CompletedTask);
        });
        runtime.Calls.Clear();

        await dispatcher.InvokeAsync(() => provider.DisposeAsync().AsTask());

        Assert.Equal(3, runtime.CallsTo("removeEventListener").Count());
        Assert.All(runtime.CallsTo("removeEventListener"), call => Assert.Same(context, call.Context));
    }

    [Fact]
    public async Task ChannelDisposal_RemovesBothPortsListenersOnRendererSynchronizationContext()
    {
        var runtime = new BrowserJSRuntime { CompleteOnThreadPool = true };
        await using var provider = CreateProvider(runtime);
        await using var channel = await provider.GetRequiredService<IBrowserPlatform>().Window.MessageChannel.CreateAsync();
        var dispatcher = Dispatcher.CreateDefault();
        var context = await dispatcher.InvokeAsync(() => SynchronizationContext.Current);
        await dispatcher.InvokeAsync(async () =>
        {
            await channel.Port1.AddEventListenerAsync(MessagePort.MessageEvent, _ => Task.CompletedTask);
            await channel.Port2.AddEventListenerAsync(MessagePort.MessageEvent, _ => Task.CompletedTask);
        });
        runtime.Calls.Clear();

        await dispatcher.InvokeAsync(() => channel.DisposeAsync().AsTask());

        Assert.Equal(2, runtime.CallsTo("removeEventListener").Count());
        Assert.All(runtime.CallsTo("removeEventListener"), call => Assert.Same(context, call.Context));
    }

    [Fact]
    public async Task PortDisposal_DuringAcquisition_PreventsListenerCreation()
    {
        var runtime = new BrowserJSRuntime();
        await using var provider = CreateProvider(runtime);
        await using var channel = await provider.GetRequiredService<IBrowserPlatform>().Window.MessageChannel.CreateAsync();
        var port = channel.Port1;
        runtime.HoldCallsTo("port1");
        var adding = port.AddEventListenerAsync(MessagePort.MessageEvent, _ => Task.CompletedTask).AsTask();

        await channel.DisposeAsync();
        runtime.ReleaseHeldCalls("port1");

        await Assert.ThrowsAsync<ObjectDisposedException>(() => adding);
        Assert.Empty(runtime.CallsTo("createCallback"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Registrations_AreIndependentAndRetainJsonSnapshots(bool broadcast)
    {
        var runtime = new BrowserJSRuntime();
        await using var provider = CreateProvider(runtime);
        var (target, add) = await CreateTargetAsync(provider, broadcast);
        await using var owned = target;
        var received = new List<MessageEvent>();
        Task Listener(MessageEvent message)
        {
            received.Add(message);
            return Task.CompletedTask;
        }
        var first = await add(Listener, CancellationToken.None);
        await using var second = await add(Listener, CancellationToken.None);
        await runtime.InvokeCallbackAsync(0, Message);
        await runtime.InvokeCallbackAsync(1, Message);
        await first.DisposeAsync();
        await first.DisposeAsync();
        await runtime.InvokeCallbackAsync(1, Message);

        Assert.Equal(3, received.Count);
        Assert.All(received, message =>
        {
            Assert.Equal("data", message.Data.GetProperty("__jsObjectId").GetString());
            Assert.Equal(JsonValueKind.Null, message.Data.GetProperty("nested")[0].ValueKind);
            Assert.Equal("https://example.test", message.Origin);
        });
        Assert.Equal(2, runtime.CallsTo("addEventListener").Count());
        Assert.Single(runtime.CallsTo("removeEventListener"));
        Assert.False((await runtime.InvokeCallbackAsync(0, Message)).Success);
        Assert.Empty(runtime.CallsTo("start"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TargetDisposal_RemovesListenersAndIsIdempotent(bool broadcast)
    {
        var runtime = new BrowserJSRuntime();
        await using var provider = CreateProvider(runtime);
        var (target, add) = await CreateTargetAsync(provider, broadcast);
        var listener = await add(_ => Task.CompletedTask, CancellationToken.None);

        await target.DisposeAsync();
        await target.DisposeAsync();
        await listener.DisposeAsync();

        Assert.Single(runtime.CallsTo("removeEventListener"));
        Assert.False((await runtime.InvokeLastCallbackAsync(Message)).Success);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => add(_ => Task.CompletedTask, CancellationToken.None).AsTask());
        Assert.True(runtime.Calls.FindIndex(c => c.Identifier == "removeEventListener") <
            runtime.Calls.FindIndex(c => c.Identifier == "close"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ScopeDisposal_RemovesListenersOnCallerOwnedTargets(bool broadcast)
    {
        var runtime = new BrowserJSRuntime();
        var provider = CreateProvider(runtime);
        var (target, add) = await CreateTargetAsync(provider, broadcast);
        await add(_ => Task.CompletedTask, CancellationToken.None);

        await provider.DisposeAsync();

        Assert.Single(runtime.CallsTo("removeEventListener"));
        Assert.False((await runtime.InvokeLastCallbackAsync(Message)).Success);
        await target.DisposeAsync();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Cancellation_StopsOnlyItsRegistration(bool broadcast)
    {
        var runtime = new BrowserJSRuntime();
        await using var provider = CreateProvider(runtime);
        var (target, add) = await CreateTargetAsync(provider, broadcast);
        await using var owned = target;
        using var lifetime = new CancellationTokenSource();
        var calls = 0;
        await add(_ => { calls++; return Task.CompletedTask; }, lifetime.Token);
        await using var other = await add(_ => { calls++; return Task.CompletedTask; }, CancellationToken.None);

        lifetime.Cancel();
        await runtime.InvokeCallbackAsync(0, Message);
        await runtime.InvokeCallbackAsync(1, Message);
        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => add(_ => Task.CompletedTask, lifetime.Token).AsTask());

        Assert.Equal(1, calls);
        Assert.Equal(lifetime.Token, exception.CancellationToken);
        Assert.Equal(2, runtime.CallsTo("addEventListener").Count());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProjectionAndListenerFailures_AreLoggedWithoutRemovingRegistration(bool broadcast)
    {
        var runtime = new BrowserJSRuntime();
        var logger = new RecordingLoggerFactory();
        await using var provider = CreateProvider(runtime, logger);
        var (target, add) = await CreateTargetAsync(provider, broadcast);
        await using var owned = target;
        var calls = 0;
        await using var listener = await add(_ =>
        {
            if (++calls == 1)
            {
                throw new InvalidOperationException("Listener failed.");
            }
            return Task.CompletedTask;
        }, CancellationToken.None);

        await runtime.InvokeLastCallbackAsync("""[{"data":{"error":"Not JSON."},"origin":""}]""");
        await runtime.InvokeLastCallbackAsync(Message);
        await runtime.InvokeLastCallbackAsync(Message);

        Assert.Equal(2, calls);
        Assert.Equal(2, logger.Entries.Count);
        Assert.All(logger.Entries, entry => Assert.Equal((LogLevel.Error, "ListenerThrew"), (entry.Level, entry.EventId.Name)));
        Assert.IsType<JsonException>(logger.Entries[0].Exception);
        Assert.IsType<InvalidOperationException>(logger.Entries[1].Exception);
        Assert.Empty(runtime.CallsTo("removeEventListener"));
    }

    [Theory]
    [InlineData(false, "import")]
    [InlineData(true, "import")]
    [InlineData(false, "createCallback")]
    [InlineData(true, "createCallback")]
    public async Task TargetDisposal_DuringRegistration_PreventsLateAddition(bool broadcast, string heldCall)
    {
        var runtime = new BrowserJSRuntime();
        await using var provider = CreateProvider(runtime);
        var (target, add) = await CreateTargetAsync(provider, broadcast);
        runtime.HoldCallsTo(heldCall);
        var adding = add(_ => Task.CompletedTask, CancellationToken.None).AsTask();

        await target.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        runtime.ReleaseHeldCalls(heldCall);

        await Assert.ThrowsAsync<ObjectDisposedException>(() => adding);
        Assert.Empty(runtime.CallsTo("addEventListener"));
        Assert.False((await runtime.InvokeLastCallbackAsync(Message)).Success);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AddEventListenerAsync_PreservesRendererSynchronizationContext(bool broadcast)
    {
        var runtime = new BrowserJSRuntime { CompleteOnThreadPool = true };
        await using var provider = CreateProvider(runtime);
        var (target, add) = await CreateTargetAsync(provider, broadcast);
        await using var owned = target;
        var dispatcher = Dispatcher.CreateDefault();
        var context = await dispatcher.InvokeAsync(() => SynchronizationContext.Current);
        runtime.Calls.Clear();

        await dispatcher.InvokeAsync(async () => await add(_ => Task.CompletedTask, CancellationToken.None));

        Assert.NotNull(context);
        Assert.All(runtime.Calls, call => Assert.Same(context, call.Context));
    }

    private static async Task<(IAsyncDisposable Target, Func<Func<MessageEvent, Task>, CancellationToken, ValueTask<IAsyncDisposable>> Add)> CreateTargetAsync(
        ServiceProvider provider, bool broadcast, bool messageError = false)
    {
        var window = provider.GetRequiredService<IBrowserPlatform>().Window;
        if (broadcast)
        {
            var channel = await window.BroadcastChannel.CreateAsync("test");
            return (channel, (listener, token) => channel.AddEventListenerAsync(messageError ? BroadcastChannel.MessageErrorEvent : BroadcastChannel.MessageEvent, listener, token));
        }

        var ports = await window.MessageChannel.CreateAsync();
        var port = ports.Port1;
        return (ports, (listener, token) => port.AddEventListenerAsync(messageError ? MessagePort.MessageErrorEvent : MessagePort.MessageEvent, listener, token));
    }

    private static ServiceProvider CreateProvider(IJSRuntime runtime, ILoggerFactory? loggerFactory = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(runtime);
        services.AddBrowserPlatform();
        if (loggerFactory is not null)
        {
            services.AddSingleton(loggerFactory);
        }

        return services.BuildServiceProvider();
    }
}
