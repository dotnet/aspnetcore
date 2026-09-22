using System;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;

if (RuntimeFeature.IsDynamicCodeSupported)
{
    throw new InvalidOperationException("This test must run under Native AOT.");
}

var failed = false;
foreach (var (method, expectedJson) in new[]
{
    (nameof(Callbacks.CompletedTask), "null"),
    (nameof(Callbacks.AsyncTask), "null"),
    (nameof(Callbacks.CompletedValueTask), "null"),
    (nameof(Callbacks.AsyncValueTask), "null"),
    (nameof(Callbacks.CompletedTaskOfInt), "42"),
    (nameof(Callbacks.AsyncTaskOfInt), "42"),
    (nameof(Callbacks.CompletedValueTaskOfInt), "42"),
    (nameof(Callbacks.AsyncValueTaskOfInt), "42"),
})
{
    using var runtime = new TestJSRuntime();
    using var reference = DotNetObjectReference.Create(new Callbacks());
    var objectId = runtime.Register(reference);
    try
    {
        DotNetDispatcher.BeginInvokeDotNet(runtime, new DotNetInvocationInfo(null, method, objectId, method), "[]");
        var result = await runtime.Completion.Task.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        if (!result.Success || result.ResultJson != expectedJson)
        {
            throw new InvalidOperationException($"Expected {expectedJson}, got {result.ResultJson}: {result.Exception}");
        }

        Console.WriteLine($"PASS {method}: {result.ResultJson}");
    }
    catch (Exception exception)
    {
        failed = true;
        Console.WriteLine($"FAIL {method}: {exception}");
    }
}

return failed ? 1 : 100;

public sealed class Callbacks
{
    [JSInvokable]
    public Task CompletedTask() => Task.CompletedTask;

    [JSInvokable]
    public async Task AsyncTask()
    {
        await Task.Yield();
    }

    [JSInvokable]
    public ValueTask CompletedValueTask() => ValueTask.CompletedTask;

    [JSInvokable]
    public async ValueTask AsyncValueTask()
    {
        await Task.Yield();
    }

    [JSInvokable]
    public Task<int> CompletedTaskOfInt() => Task.FromResult(42);

    [JSInvokable]
    public async Task<int> AsyncTaskOfInt()
    {
        await Task.Yield();
        return 42;
    }

    [JSInvokable]
    public ValueTask<int> CompletedValueTaskOfInt() => ValueTask.FromResult(42);

    [JSInvokable]
    public async ValueTask<int> AsyncValueTaskOfInt()
    {
        await Task.Yield();
        return 42;
    }
}

internal sealed class TestJSRuntime : JSRuntime
{
    public TaskCompletionSource<DotNetInvocationResult> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public TestJSRuntime() => JsonSerializerOptions.TypeInfoResolver = TestJsonContext.Default;

    public long Register(DotNetObjectReference<Callbacks> reference)
    {
        var context = new TestJsonContext(JsonSerializerOptions);
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(reference, context.DotNetObjectReferenceCallbacks));
        return document.RootElement.GetProperty("__dotNetObject").GetInt64();
    }

    protected override void BeginInvokeJS(long taskId, string identifier, string argsJson, JSCallResultType resultType, long targetInstanceId)
        => throw new NotSupportedException();

    protected override void EndInvokeDotNet(DotNetInvocationInfo invocationInfo, in DotNetInvocationResult invocationResult)
    {
        if (invocationInfo.CallId is null || !Completion.TrySetResult(invocationResult))
        {
            throw new InvalidOperationException("Expected exactly one completion with a call ID.");
        }
    }
}

[JsonSerializable(typeof(object))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(DotNetObjectReference<Callbacks>))]
internal sealed partial class TestJsonContext : JsonSerializerContext;