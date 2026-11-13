// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Xunit;

namespace Microsoft.AspNetCore.Components.Platform.Tests;

public class ScreenOrientationTest
{
    [Fact]
    public async Task Orientation_IsStableAndNotAcquiredBeforeUse()
    {
        var jsRuntime = new RecordingJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        var screen = provider.GetRequiredService<IBrowserPlatform>().Window.Screen;

        Assert.Same(screen.Orientation, screen.Orientation);
        Assert.Equal(0, jsRuntime.GetValueCallCount);
    }

    [Fact]
    public async Task Orientation_ReadsAttributesAndAcquiresReferenceOnce()
    {
        var jsRuntime = new RecordingJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        var orientation = provider.GetRequiredService<IBrowserPlatform>().Window.Screen.Orientation;

        Assert.Equal(OrientationType.PortraitSecondary, await orientation.GetTypeAsync());
        Assert.Equal((ushort)180, await orientation.GetAngleAsync());

        Assert.Equal(1, jsRuntime.GetValueCallCount);
        Assert.Equal("screen.orientation", jsRuntime.RequestedProperty);
        Assert.Equal(["type", "angle"], jsRuntime.ObjectReference.ReadProperties);
    }

    [Fact]
    public async Task Orientation_SharesPendingAcquisitionBetweenCallers()
    {
        var pending = new TaskCompletionSource<IJSObjectReference>();
        var jsRuntime = new RecordingJSRuntime { PendingNextGetValue = pending };
        await using var provider = CreateServiceProvider(jsRuntime);
        var orientation = provider.GetRequiredService<IBrowserPlatform>().Window.Screen.Orientation;

        var first = orientation.GetTypeAsync().AsTask();
        var second = orientation.GetAngleAsync().AsTask();
        pending.SetResult(jsRuntime.ObjectReference);
        await Task.WhenAll(first, second);

        Assert.Equal(1, jsRuntime.GetValueCallCount);
        Assert.Equal(2, jsRuntime.ObjectReference.ReadProperties.Count);
    }

    [Fact]
    public async Task Orientation_ReacquiresReferenceAfterAcquisitionIsCanceled()
    {
        var jsRuntime = new RecordingJSRuntime { CancelNextGetValue = true };
        await using var provider = CreateServiceProvider(jsRuntime);
        var orientation = provider.GetRequiredService<IBrowserPlatform>().Window.Screen.Orientation;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => orientation.GetAngleAsync().AsTask());

        Assert.Equal((ushort)180, await orientation.GetAngleAsync());
        Assert.Equal(2, jsRuntime.GetValueCallCount);
    }

    [Fact]
    public async Task Orientation_ThrowsNotSupportedExceptionWhenTheBrowserHasNoScreenOrientation()
    {
        // JS interop throws this for a missing screen.orientation, such as in an older browser.
        var missingProperty = new JSException("The property 'orientation' is not defined or is not readable.");
        var jsRuntime = new RecordingJSRuntime { GetValueException = missingProperty };
        var provider = CreateServiceProvider(jsRuntime);
        var orientation = provider.GetRequiredService<IBrowserPlatform>().Window.Screen.Orientation;
        Func<Task>[] members =
        [
            () => orientation.GetTypeAsync().AsTask(),
            () => orientation.GetAngleAsync().AsTask(),
            () => orientation.AddEventListenerAsync(ScreenOrientation.ChangeEvent, _ => Task.CompletedTask).AsTask(),
        ];

        foreach (var member in members)
        {
            var exception = await Assert.ThrowsAsync<NotSupportedException>(member);
            Assert.Same(missingProperty, exception.InnerException);
        }
        // Disposal must not throw for the failed acquisitions.
        await provider.DisposeAsync();

        Assert.Equal(members.Length, jsRuntime.GetValueCallCount);
        Assert.Empty(jsRuntime.ObjectReference.ReadProperties);
    }

    [Fact]
    public async Task Orientation_ThrowsNotSupportedExceptionWhenScreenOrientationIsNull()
    {
        var jsRuntime = new RecordingJSRuntime { ReturnNullReference = true };
        var provider = CreateServiceProvider(jsRuntime);
        var orientation = provider.GetRequiredService<IBrowserPlatform>().Window.Screen.Orientation;

        await Assert.ThrowsAsync<NotSupportedException>(() => orientation.GetTypeAsync().AsTask());
        await Assert.ThrowsAsync<NotSupportedException>(() => orientation.GetAngleAsync().AsTask());
        // Disposal must not throw for the failed acquisitions.
        await provider.DisposeAsync();

        Assert.Equal(2, jsRuntime.GetValueCallCount);
        Assert.Empty(jsRuntime.ObjectReference.ReadProperties);
    }

    [Fact]
    public async Task Orientation_PassesThroughOtherAcquisitionFailuresAndReacquires()
    {
        var disconnected = new JSDisconnectedException("The circuit is disconnected.");
        var jsRuntime = new RecordingJSRuntime { GetValueException = disconnected };
        await using var provider = CreateServiceProvider(jsRuntime);
        var orientation = provider.GetRequiredService<IBrowserPlatform>().Window.Screen.Orientation;

        var exception = await Assert.ThrowsAsync<JSDisconnectedException>(() => orientation.GetAngleAsync().AsTask());
        jsRuntime.GetValueException = null;

        Assert.Same(disconnected, exception);
        Assert.Equal((ushort)180, await orientation.GetAngleAsync());
        Assert.Equal(2, jsRuntime.GetValueCallCount);
    }

    [Fact]
    public async Task ScopeDisposal_DisposesTheAcquiredReference()
    {
        var jsRuntime = new RecordingJSRuntime();
        var provider = CreateServiceProvider(jsRuntime);

        await using (provider)
        {
            await provider.GetRequiredService<IBrowserPlatform>().Window.Screen.Orientation.GetAngleAsync();
            Assert.False(jsRuntime.ObjectReference.Disposed);
        }

        Assert.True(jsRuntime.ObjectReference.Disposed);
    }

    [Fact]
    public async Task ScopeDisposal_MakesOrientationUnusable()
    {
        var jsRuntime = new RecordingJSRuntime();
        var provider = CreateServiceProvider(jsRuntime);
        var orientation = provider.GetRequiredService<IBrowserPlatform>().Window.Screen.Orientation;
        await orientation.GetAngleAsync();

        await provider.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => orientation.GetAngleAsync().AsTask());
        Assert.Equal(1, jsRuntime.GetValueCallCount);
    }

    [Fact]
    public async Task ScopeDisposal_DoesNotWaitForPendingAcquisition()
    {
        var jsRuntime = new RecordingJSRuntime { PendingNextGetValue = new TaskCompletionSource<IJSObjectReference>() };
        var provider = CreateServiceProvider(jsRuntime);

        _ = provider.GetRequiredService<IBrowserPlatform>().Window.Screen.Orientation.GetAngleAsync();

        await provider.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(jsRuntime.ObjectReference.Disposed);
    }

    [Fact]
    public async Task AddEventListenerAsync_AddsAFunctionThatDeliversTheChangeEvent()
    {
        var jsRuntime = new BrowserJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        var orientation = provider.GetRequiredService<IBrowserPlatform>().Window.Screen.Orientation;
        Event? received = null;

        await orientation.AddEventListenerAsync(ScreenOrientation.ChangeEvent, changeEvent =>
        {
            received = changeEvent;
            return Task.CompletedTask;
        });
        var result = await jsRuntime.InvokeLastCallbackAsync("[{}]");

        // The screen orientation is object 1, the module is object 2, and the function is object 3.
        var acquisition = Assert.Single(jsRuntime.CallsTo("screen.orientation"));
        Assert.Equal(JSCallType.GetValue, acquisition.CallType);
        using var createCallbackArgs = JsonDocument.Parse(Assert.Single(jsRuntime.CallsTo("createCallback")).ArgsJson);
        Assert.Equal("[{}]", createCallbackArgs.RootElement[1].GetString());
        var addition = Assert.Single(jsRuntime.CallsTo("addEventListener"));
        Assert.Equal((JSCallType.FunctionCall, 1, """["change",{"__jsObjectId":3}]"""), (addition.CallType, addition.TargetInstanceId, addition.ArgsJson));
        Assert.True(result.Success, result.Exception?.ToString());
        Assert.NotNull(received);
    }

    [Fact]
    public async Task AddEventListenerAsync_KeepsListenersIndependent()
    {
        var jsRuntime = new BrowserJSRuntime();
        await using var provider = CreateServiceProvider(jsRuntime);
        var orientation = provider.GetRequiredService<IBrowserPlatform>().Window.Screen.Orientation;
        var calls = new List<string>();

        var first = await orientation.AddEventListenerAsync(ScreenOrientation.ChangeEvent, _ => { calls.Add("first"); return Task.CompletedTask; });
        await orientation.AddEventListenerAsync(ScreenOrientation.ChangeEvent, _ => { calls.Add("second"); return Task.CompletedTask; });
        await jsRuntime.InvokeCallbackAsync(0, "[{}]");
        await jsRuntime.InvokeCallbackAsync(1, "[{}]");
        await first.DisposeAsync();
        var afterRemoval = await jsRuntime.InvokeCallbackAsync(0, "[{}]");
        await jsRuntime.InvokeCallbackAsync(1, "[{}]");

        // The module is object 2, and the functions are objects 3 and 4.
        Assert.Equal(["first", "second", "second"], calls);
        Assert.False(afterRemoval.Success);
        var removal = Assert.Single(jsRuntime.CallsTo("removeEventListener"));
        Assert.Equal((1, """["change",{"__jsObjectId":3}]"""), (removal.TargetInstanceId, removal.ArgsJson));
    }

    [Fact]
    public async Task AddEventListenerAsync_ThrowsForNullArguments()
    {
        await using var provider = CreateServiceProvider(new BrowserJSRuntime());
        var orientation = provider.GetRequiredService<IBrowserPlatform>().Window.Screen.Orientation;

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => orientation.AddEventListenerAsync<Event>(null!, _ => Task.CompletedTask).AsTask());
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => orientation.AddEventListenerAsync(ScreenOrientation.ChangeEvent, null!).AsTask());
    }

    [Fact]
    public async Task ScopeDisposal_RemovesListeners()
    {
        var jsRuntime = new BrowserJSRuntime();
        var provider = CreateServiceProvider(jsRuntime);
        var orientation = provider.GetRequiredService<IBrowserPlatform>().Window.Screen.Orientation;
        var listener = await orientation.AddEventListenerAsync(ScreenOrientation.ChangeEvent, _ => Task.CompletedTask);

        await provider.DisposeAsync();
        await listener.DisposeAsync();

        // The function (3) goes first, then the screen orientation (1), then the module (2).
        Assert.Single(jsRuntime.CallsTo("removeEventListener"));
        Assert.Equal(
            ["[3]", "[1]", "[2]"],
            jsRuntime.CallsTo("DotNet.disposeJSObjectReferenceById").Select(call => call.ArgsJson));
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => orientation.AddEventListenerAsync(ScreenOrientation.ChangeEvent, _ => Task.CompletedTask).AsTask());
    }

    // A mock IJSRuntime returns .NET objects as they are, so the tests below run the real JSRuntime serializer.
    [Theory]
    [InlineData("portrait-primary")]
    [InlineData("portrait-secondary")]
    [InlineData("landscape-primary")]
    [InlineData("landscape-secondary")]
    [InlineData("future-value")]
    public async Task GetTypeAsync_ReadsTheStringFromTheBrowser(string type)
    {
        var jsRuntime = new SerializingJSRuntime { TypeResult = JsonSerializer.Serialize(type) };
        await using var provider = CreateServiceProvider(jsRuntime);

        var orientationType = await provider.GetRequiredService<IBrowserPlatform>().Window.Screen.Orientation.GetTypeAsync();

        Assert.Equal(new OrientationType(type), orientationType);
        Assert.Equal(type, orientationType.Value);
    }

    [Fact]
    public async Task OrientationType_SendsEachValueAsTheWebIdlString()
    {
        var jsRuntime = new SerializingJSRuntime();

        await jsRuntime.InvokeVoidAsync("send", OrientationType.PortraitPrimary);
        await jsRuntime.InvokeVoidAsync("send", OrientationType.PortraitSecondary);
        await jsRuntime.InvokeVoidAsync("send", OrientationType.LandscapePrimary);
        await jsRuntime.InvokeVoidAsync("send", OrientationType.LandscapeSecondary);
        await jsRuntime.InvokeVoidAsync("send", new OrientationType("future-value"));

        Assert.Equal(
            ["""["portrait-primary"]""", """["portrait-secondary"]""", """["landscape-primary"]""", """["landscape-secondary"]""", """["future-value"]"""],
            jsRuntime.ArgumentsSentTo("send"));
    }

    [Fact]
    public async Task OrientationType_DefaultHasNoValueAndIsNotSent()
    {
        var jsRuntime = new SerializingJSRuntime();
        var defaultValue = default(OrientationType);

        Assert.Throws<InvalidOperationException>(() => defaultValue.Value);
        Assert.Equal("", defaultValue.ToString());
        Assert.True(defaultValue.Equals((object)default(OrientationType)));
        Assert.True(defaultValue == new OrientationType());
        Assert.Equal(0, defaultValue.GetHashCode());
        Assert.True(defaultValue != OrientationType.PortraitPrimary);
        Assert.True(defaultValue != OrientationType.PortraitSecondary);
        Assert.True(defaultValue != OrientationType.LandscapePrimary);
        Assert.True(defaultValue != OrientationType.LandscapeSecondary);
        Assert.True(defaultValue != new OrientationType(""));
        await Assert.ThrowsAsync<InvalidOperationException>(() => jsRuntime.InvokeVoidAsync("send", defaultValue).AsTask());
        Assert.Empty(jsRuntime.ArgumentsSentTo("send"));
    }

    [Fact]
    public void OrientationType_ComparesByOrdinalString()
    {
        Assert.Equal(OrientationType.LandscapePrimary, new OrientationType("landscape-primary"));
        Assert.True(OrientationType.LandscapePrimary == new OrientationType("landscape-primary"));
        Assert.True(OrientationType.LandscapePrimary != new OrientationType("Landscape-Primary"));
        Assert.Equal(OrientationType.LandscapePrimary.GetHashCode(), new OrientationType("landscape-primary").GetHashCode());
        Assert.True(OrientationType.LandscapePrimary.Equals((object)new OrientationType("landscape-primary")));
        Assert.False(OrientationType.LandscapePrimary.Equals((object)"landscape-primary"));
        Assert.False(OrientationType.LandscapePrimary.Equals(null));
        Assert.Equal("portrait-secondary", OrientationType.PortraitSecondary.ToString());
        Assert.Throws<ArgumentNullException>("value", () => new OrientationType(null!));
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

            // JS interop returns a null reference when screen.orientation exists and holds null or undefined.
            return ValueTask.FromResult(ReturnNullReference ? default! : (TValue)(object)ObjectReference);
        }
    }

    private sealed class RecordingJSObjectReference : IJSObjectReference
    {
        public List<string> ReadProperties { get; } = [];

        public bool Disposed { get; private set; }

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
            ReadProperties.Add(identifier);

            object value = identifier switch
            {
                "type" => OrientationType.PortraitSecondary,
                "angle" => (ushort)180,
                _ => throw new InvalidOperationException($"Unexpected property '{identifier}'."),
            };

            return ValueTask.FromResult((TValue)value);
        }

        public ValueTask<TValue> GetValueAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
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
        private readonly List<(string Identifier, string? ArgsJson)> _calls = [];

        public string TypeResult { get; init; } = "null";

        public string[] ArgumentsSentTo(string identifier)
            => [.. _calls.Where(call => call.Identifier == identifier).Select(call => call.ArgsJson ?? string.Empty)];

        protected override void BeginInvokeJS(in JSInvocationInfo invocationInfo)
        {
            _calls.Add((invocationInfo.Identifier, invocationInfo.ArgsJson));

            var result = invocationInfo.Identifier switch
            {
                "screen.orientation" => """{"__jsObjectId":1}""",
                "type" => TypeResult,
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
