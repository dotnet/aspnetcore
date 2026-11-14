// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.AspNetCore.Components.Performance;

// Measures completing a JS interop call (InvokeAsync<T> followed by EndInvokeJS),
// which is how results of JS calls are delivered to .NET in Blazor Server.
public class EndInvokeJSBenchmark
{
    private string _endInvokeArgs;

    [Params(64, 16 * 1024)]
    public int PayloadLength { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // A fresh runtime's first call always gets task ID 2.
        _endInvokeArgs = $"[2,true,\"{new string('a', PayloadLength)}\"]";
    }

    [Benchmark]
    public string InvokeAndComplete()
    {
        var runtime = new TestJSRuntime();
        var task = runtime.InvokeAsync<string>("someMethod").AsTask();
        DotNetDispatcher.EndInvokeJS(runtime, _endInvokeArgs);
        return task.Result;
    }

    private sealed class TestJSRuntime : JSRuntime
    {
        protected override void BeginInvokeJS(long taskId, string identifier, string argsJson, JSCallResultType resultType, long targetInstanceId)
        {
        }

        protected override void EndInvokeDotNet(DotNetInvocationInfo invocationInfo, in DotNetInvocationResult invocationResult)
        {
        }
    }
}
