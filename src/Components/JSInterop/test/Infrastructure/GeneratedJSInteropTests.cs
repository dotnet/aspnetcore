// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.JSInterop.Infrastructure;

public class GeneratedJSInteropTests
{
    [Fact]
    public async Task InvokesRegisteredCallbacksWithApplicationValueTypeMetadata()
    {
        var runtime = CreateRuntime();
        runtime.GeneratedInterop.RegisterStatic("App", "Double", 1, (r, _, args) =>
        {
            var value = args[0].Deserialize(GeneratedJSInterop.Metadata<GeneratedInteropValue>(r));
            return ValueTask.FromResult<string?>(JsonSerializer.Serialize(
                new GeneratedInteropValue(value.Number * 2), GeneratedJSInterop.Metadata<GeneratedInteropValue>(r)));
        });

        var result = await runtime.GeneratedInterop.Invoke(runtime, new("App", "Double", 0, "1"), """[{"number":21}]""", synchronous: true);

        Assert.Equal("""{"number":42}""", result);
        Assert.Throws<ArgumentException>(() => runtime.GeneratedInterop.Invoke(runtime, new("App", "Missing", 0, "2"), "[]", synchronous: true));
        Assert.Throws<ArgumentException>(() => runtime.GeneratedInterop.Invoke(runtime, new("App", "Double", 0, "3"), "[]", synchronous: true));
        Assert.Throws<NotSupportedException>(() => GeneratedJSInterop.Metadata<UnregisteredPayload>(runtime));
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
            var bytes = args[0].Deserialize(GeneratedJSInterop.Metadata<byte[]>(r))!;
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
            => JsonSerializer.Serialize(await result, GeneratedJSInterop.Metadata<int>(runtime));
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

    [Fact]
    public async Task MatchesReflectionArgumentCountAndNullResultHandling()
    {
        var runtime = CreateRuntime();
        runtime.GeneratedInterop.RegisterStatic("App", "None", 0, (_, _, _) => default);
        runtime.GeneratedInterop.RegisterStatic("App", "One", 1, (_, _, _) => default);

        Assert.Null(await runtime.GeneratedInterop.Invoke(runtime, new("App", "None", 0, "1"), "[1,2]", synchronous: true));
        var tooMany = Assert.Throws<JsonException>(() => runtime.GeneratedInterop.Invoke(runtime, new("App", "One", 0, "2"), "[1,{}]", synchronous: true));
        Assert.Equal("Unexpected JSON token StartObject. Ensure that the call to `One' is supplied with exactly '1' parameters.", tooMany.Message);
        Assert.Throws<JsonException>(() => runtime.GeneratedInterop.Invoke(runtime, new("App", "One", 0, "3"), "null", synchronous: true));
        var tooFew = Assert.Throws<ArgumentException>(() => runtime.GeneratedInterop.Invoke(runtime, new("App", "One", 0, "4"), "[]", synchronous: true));
        Assert.Equal("The call to 'One' expects '1' parameters, but received '0'.", tooFew.Message);

        await runtime.GeneratedInterop.BeginInvoke(runtime, new("App", "None", 0, "5"), "[]");
        Assert.True(runtime.Result.Success);
        Assert.Equal("null", runtime.Result.ResultJson);
    }

    [Fact]
    public async Task ResolvesSharedRegistrationsAndBaseTypes()
    {
        var runtime = CreateRuntime();
        var assembly = Guid.NewGuid().ToString();
        GeneratedJSInterop.Shared.RegisterStatic(assembly, "Shared", 0, (_, _, _) => ValueTask.FromResult<string?>("1"));
        GeneratedJSInterop.Shared.RegisterInstance<SharedBaseProbe>("Base", 0, (_, target, _) => ValueTask.FromResult<string?>(target!.GetType().Name));
        runtime.GeneratedInterop.RegisterStatic(assembly, "Shared", 0, (_, _, _) => ValueTask.FromResult<string?>("2"));
        using var reference = DotNetObjectReference.Create(new SharedDerivedProbe());
        var id = runtime.TrackObjectReference(reference);

        Assert.Equal("2", await runtime.GeneratedInterop.Invoke(runtime, new(assembly, "Shared", 0, "1"), "[]", synchronous: true));
        Assert.Equal("1", await CreateRuntime().GeneratedInterop.Invoke(runtime, new(assembly, "Shared", 0, "2"), "[]", synchronous: true));
        Assert.Equal(nameof(SharedDerivedProbe), await runtime.GeneratedInterop.Invoke(runtime, new(null, "Base", id, "3"), "[]", synchronous: true));
        Assert.Throws<InvalidOperationException>(() => GeneratedJSInterop.Shared.RegisterStatic(assembly, "Shared", 0, (_, _, _) => default));
    }

    [Fact]
    public async Task HelpersMatchReflectionArgumentResultAndTaskSemantics()
    {
        var runtime = CreateRuntime();
        using var reference = JsonDocument.Parse("""{"__dotNetObject":1}""");

        var misuse = Assert.Throws<InvalidOperationException>(() => GeneratedJSInterop.ReadArgument<GeneratedInteropValue>(runtime, reference.RootElement, "Call", 1));
        Assert.Equal("In call to 'Call', parameter of type 'GeneratedInteropValue' at index 2 must be declared as type 'DotNetObjectRef<GeneratedInteropValue>' to receive the incoming value.", misuse.Message);
        Assert.Equal("""{"derived":2,"base":1}""", await GeneratedJSInterop.SerializeResult<GeneratedInteropBase>(runtime, new GeneratedInteropDerived { Base = 1, Derived = 2 }));
        Assert.Null(await GeneratedJSInterop.SerializeResult<GeneratedInteropBase?>(runtime, null));

        var fault = new InvalidOperationException("inner");
        var faulted = await Assert.ThrowsAsync<InvalidOperationException>(async () => await GeneratedJSInterop.SerializeResultAsync(runtime, Task.FromException<int>(new AggregateException(fault))));
        Assert.Same(fault, faulted);
        await Assert.ThrowsAsync<TaskCanceledException>(async () => await GeneratedJSInterop.CompleteAsync(Task.FromCanceled(new CancellationToken(canceled: true))));
        Assert.Equal("42", await GeneratedJSInterop.SerializeResultAsync(runtime, ValueTask.FromResult(42)));
        Assert.Null(await GeneratedJSInterop.CompleteAsync(ValueTask.CompletedTask));
    }

    [Fact]
    public async Task NonInvokableOverrideMarkerMatchesReflectionFailure()
    {
        var runtime = CreateRuntime();
        runtime.GeneratedInterop.RegisterInstance<HidingBaseProbe>("Run", 0, (_, _, _) => ValueTask.FromResult<string?>("base"));
        runtime.GeneratedInterop.RegisterNonInvokableInstance<HidingDerivedProbe>("Run");
        using var baseReference = DotNetObjectReference.Create(new HidingBaseProbe());
        using var derivedReference = DotNetObjectReference.Create(new HidingDerivedProbe());

        Assert.Equal("base", await runtime.GeneratedInterop.Invoke(runtime, new(null, "Run", runtime.TrackObjectReference(baseReference), "1"), "[]", synchronous: true));
        var hidden = Assert.Throws<ArgumentException>(() => runtime.GeneratedInterop.Invoke(runtime, new(null, "Run", runtime.TrackObjectReference(derivedReference), "2"), "[]", synchronous: true));
        Assert.Equal("The type 'HidingDerivedProbe' does not contain a public invokable method with [JSInvokableAttribute(\"Run\")].", hidden.Message);
        Assert.Throws<InvalidOperationException>(() => runtime.GeneratedInterop.RegisterNonInvokableInstance<HidingDerivedProbe>("Run"));
    }

    [Fact]
    public void SharedTypeInfoResolverHonorsResolversAddedAfterRuntimeCreation()
    {
        var runtime = new RecordingRuntime();
        runtime.JsonSerializerOptions.TypeInfoResolverChain.Clear();
        runtime.JsonSerializerOptions.TypeInfoResolverChain.Add(GeneratedJSInterop.SharedTypeInfoResolver);
        runtime.JsonSerializerOptions.MakeReadOnly();

        GeneratedJSInterop.AddTypeInfoResolver(GeneratedInteropLateJsonContext.Default);

        Assert.Equal("""{"late":1}""", JsonSerializer.Serialize(new GeneratedInteropLateValue(1), GeneratedJSInterop.Metadata<GeneratedInteropLateValue>(runtime)));
    }

    [Fact]
    public void ReferenceConvertersRegisteredAfterRuntimeCreationBindToEachRuntime()
    {
        var runtime = CreateRuntime();
        var otherRuntime = CreateRuntime();
        Assert.Null(GeneratedJSInterop.CreateReferenceConverter(typeof(LateReferenceProbe), runtime));

        GeneratedJSInterop.RegisterReferenceConverter<LateReferenceProbe>();

        var converter = Assert.IsType<DotNetObjectReferenceJsonConverter<LateReferenceProbe>>(
            GeneratedJSInterop.CreateReferenceConverter(typeof(LateReferenceProbe), runtime));
        Assert.Same(runtime, converter.JSRuntime);
        var otherConverter = Assert.IsType<DotNetObjectReferenceJsonConverter<LateReferenceProbe>>(
            GeneratedJSInterop.CreateReferenceConverter(typeof(LateReferenceProbe), otherRuntime));
        Assert.Same(otherRuntime, otherConverter.JSRuntime);
    }

    [Fact]
    public async Task InstanceProvidersRegisterOncePerRuntimeAndPreserveExplicitOverrides()
    {
        var runtime = CreateRuntime();
        var registrations = 0;
        using var first = DotNetObjectReference.Create(new ProviderProbe(1, () => Interlocked.Increment(ref registrations)));
        using var second = DotNetObjectReference.Create(new ProviderProbe(2, () => Interlocked.Increment(ref registrations)));
        var firstId = runtime.TrackObjectReference(first);
        var secondId = runtime.TrackObjectReference(second);
        var results = await Task.WhenAll(Enumerable.Range(0, 16).Select(index => Task.Run(async () =>
            await runtime.GeneratedInterop.Invoke(runtime, new(null, "Value", index % 2 == 0 ? firstId : secondId, null), "[]", synchronous: true))));

        Assert.Equal(Enumerable.Range(0, 16).Select(index => index % 2 == 0 ? "1" : "2"), results);
        Assert.Equal(1, registrations);
        var otherRuntime = CreateRuntime();
        using var other = DotNetObjectReference.Create(new ProviderProbe(2, () => Interlocked.Increment(ref registrations)));
        Assert.Equal("2", await otherRuntime.GeneratedInterop.Invoke(otherRuntime, new(null, "Value", otherRuntime.TrackObjectReference(other), null), "[]", synchronous: true));
        Assert.Equal(2, registrations);

        runtime.GeneratedInterop.RegisterInstance<ProviderProbe>("Value", 0, (_, _, _) => ValueTask.FromResult<string?>("3"));
        Assert.Equal("3", await runtime.GeneratedInterop.Invoke(runtime, new(null, "Value", firstId, null), "[]", synchronous: true));
        Assert.Throws<ArgumentException>(() => runtime.GeneratedInterop.Invoke(runtime, new(null, "Missing", firstId, null), "[]", synchronous: true));
        Assert.Equal(2, registrations);
    }

    [Fact]
    public async Task InstanceProviderFailuresPropagateDirectlyAndDoNotBlockExplicitRegistrations()
    {
        var runtime = CreateRuntime();
        var failure = new InvalidOperationException("registration failure");
        using var reference = DotNetObjectReference.Create(new ProviderProbe(1, () => throw failure));
        var id = runtime.TrackObjectReference(reference);

        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => runtime.GeneratedInterop.Invoke(runtime, new(null, "Value", id, null), "[]", synchronous: true)));
        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => runtime.GeneratedInterop.Invoke(runtime, new(null, "Value", id, null), "[]", synchronous: true)));
        runtime.GeneratedInterop.RegisterInstance<ProviderProbe>("Value", 0, (_, _, _) => ValueTask.FromResult<string?>("4"));
        Assert.Equal("4", await runtime.GeneratedInterop.Invoke(runtime, new(null, "Value", id, null), "[]", synchronous: true));
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

    private class SharedBaseProbe;

    private sealed class SharedDerivedProbe : SharedBaseProbe;

    private class HidingBaseProbe;

    private sealed class HidingDerivedProbe : HidingBaseProbe;

    private sealed class LateReferenceProbe;

    private sealed class ProviderProbe(int value, Action register) : GeneratedJSInterop.IInstanceRegistrationProvider
    {
        public void RegisterInstanceCallbacks(GeneratedJSInterop registrations)
        {
            register();
            registrations.RegisterInstance<ProviderProbe>("Value", 0, (_, target, _) =>
                ValueTask.FromResult<string?>(((ProviderProbe)target!).Value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        private int Value => value;
    }
}

internal class GeneratedInteropBase
{
    public int Base { get; set; }
}

internal sealed class GeneratedInteropDerived : GeneratedInteropBase
{
    public int Derived { get; set; }
}

internal readonly record struct GeneratedInteropValue(int Number);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(GeneratedInteropValue))]
[JsonSerializable(typeof(GeneratedInteropBase))]
[JsonSerializable(typeof(GeneratedInteropDerived))]
internal partial class GeneratedInteropTestJsonContext : JsonSerializerContext;

internal readonly record struct GeneratedInteropLateValue(int Late);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(GeneratedInteropLateValue))]
internal partial class GeneratedInteropLateJsonContext : JsonSerializerContext;
