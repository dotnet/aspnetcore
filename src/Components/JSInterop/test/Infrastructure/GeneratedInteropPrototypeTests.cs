// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.JSInterop.Infrastructure;

public class GeneratedInteropPrototypeTests
{
    [Fact]
    public async Task InvokesRegisteredCallbacksWithApplicationValueTypeMetadata()
    {
        var runtime = CreateRuntime();
        runtime.GeneratedInterop.RegisterStatic("App", "Double", 1, (r, _, args) =>
        {
            var value = args[0].Deserialize(GeneratedInteropPrototype.Metadata<GeneratedInteropValue>(r));
            return ValueTask.FromResult<string?>(JsonSerializer.Serialize(
                new GeneratedInteropValue(value.Number * 2), GeneratedInteropPrototype.Metadata<GeneratedInteropValue>(r)));
        });

        var result = await runtime.GeneratedInterop.Invoke(runtime, new("App", "Double", 0, "1"), """[{"number":21}]""", synchronous: true);

        Assert.Equal("""{"number":42}""", result);
        Assert.Throws<ArgumentException>(() => runtime.GeneratedInterop.Invoke(runtime, new("App", "Missing", 0, "2"), "[]", synchronous: true));
        Assert.Throws<ArgumentException>(() => runtime.GeneratedInterop.Invoke(runtime, new("App", "Double", 0, "3"), "[]", synchronous: true));
        Assert.Throws<NotSupportedException>(() => GeneratedInteropPrototype.Metadata<UnregisteredPayload>(runtime));
    }

    [Fact]
    public async Task ConsumesByteArraysBeforeOverlappingAsyncCallsAndDisposesReference()
    {
        var runtime = CreateRuntime();
        using var reference = DotNetObjectReference.Create(new InstanceProbe());
        var id = runtime.TrackObjectReference(reference);
        var firstGate = new TaskCompletionSource();
        var secondGate = new TaskCompletionSource();
        var gates = new Queue<Task>([firstGate.Task, secondGate.Task]);
        runtime.GeneratedInterop.RegisterInstance<InstanceProbe>("BytesLater", 1, (r, target, args) =>
        {
            var bytes = args[0].Deserialize(GeneratedInteropPrototype.Metadata<byte[]>(r))!;
            return SerializeAfterAsync(((InstanceProbe)target!).BytesLater(bytes, gates.Dequeue()));
        }, isAsync: true);

        runtime.ReceiveByteArray(0, [1, 2]);
        var first = runtime.GeneratedInterop.Invoke(runtime, new(null, "BytesLater", id, "1"), """[{"__byte[]":0}]""", synchronous: false);
        Assert.Equal(0, runtime.ByteArraysToBeRevived.Count);
        runtime.ReceiveByteArray(0, [3, 4]);
        var second = runtime.GeneratedInterop.Invoke(runtime, new(null, "BytesLater", id, "2"), """[{"__byte[]":0}]""", synchronous: false);
        Assert.Equal(0, runtime.ByteArraysToBeRevived.Count);

        secondGate.SetResult();
        firstGate.SetResult();
        Assert.Equal("3", await first);
        Assert.Equal("7", await second);
        Assert.Throws<InvalidOperationException>(() => runtime.GeneratedInterop.Invoke(runtime, new(null, "BytesLater", id, "3"), "[]", synchronous: true));

        await runtime.GeneratedInterop.Invoke(runtime, new(null, "__Dispose", id, "4"), "[]", synchronous: false);
        Assert.Throws<ArgumentException>(() => runtime.GetObjectReference(id));

        async ValueTask<string?> SerializeAfterAsync(ValueTask<int> result)
            => JsonSerializer.Serialize(await result, GeneratedInteropPrototype.Metadata<int>(runtime));
    }

    [Fact]
    public async Task ReportsFaultsAndCancellationThroughInvocationResults()
    {
        var runtime = CreateRuntime();
        runtime.GeneratedInterop.RegisterStatic("App", "Fail", 0, (_, _, _) => throw new InvalidOperationException("test failure"));
        await runtime.GeneratedInterop.BeginInvoke(runtime, new("App", "Fail", 0, "1"), "[]");
        Assert.False(runtime.Result.Success);
        Assert.Equal("test failure", Assert.IsType<InvalidOperationException>(runtime.Result.Exception).Message);

        runtime.GeneratedInterop.RegisterStatic("App", "Cancel", 0, async (_, _, _) =>
        {
            await Task.FromCanceled(new CancellationToken(canceled: true));
            return "null";
        }, isAsync: true);
        await runtime.GeneratedInterop.BeginInvoke(runtime, new("App", "Cancel", 0, "2"), "[]");
        Assert.False(runtime.Result.Success);
        Assert.IsAssignableFrom<OperationCanceledException>(runtime.Result.Exception);
    }

    private static RecordingRuntime CreateRuntime()
    {
        var runtime = new RecordingRuntime();
        runtime.JsonSerializerOptions.TypeInfoResolverChain.Clear();
        runtime.JsonSerializerOptions.TypeInfoResolverChain.Add(JSInteropJsonTypeInfoResolver.Instance);
        runtime.JsonSerializerOptions.TypeInfoResolverChain.Add(GeneratedInteropJsonContext.Default);
        runtime.JsonSerializerOptions.TypeInfoResolverChain.Add(GeneratedInteropTestJsonContext.Default);
        runtime.JsonSerializerOptions.MakeReadOnly();

        return runtime;
    }

    private sealed class RecordingRuntime : TestJSRuntime
    {
        public DotNetInvocationResult Result { get; private set; }

        protected internal override void EndInvokeDotNet(DotNetInvocationInfo invocationInfo, in DotNetInvocationResult invocationResult)
        {
            Result = invocationResult;
        }
    }

    private sealed class InstanceProbe
    {
        public async ValueTask<int> BytesLater(byte[] bytes, Task gate)
        {
            await gate;
            return bytes.Sum(value => value);
        }
    }

    private sealed class UnregisteredPayload;
}

internal readonly record struct GeneratedInteropValue(int Number);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(GeneratedInteropValue))]
internal partial class GeneratedInteropTestJsonContext : JsonSerializerContext;
