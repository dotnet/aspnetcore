// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks.Http2.FlowControl;

public class FlowControlBenchmark
{
    private readonly InputFlowControl _flowControl = new(1000, 10);
    private const int N = 100000;
    private const int Spin = 50;
    private const int ThreadsCount = 3;

    [IterationSetup]
    public void IterationSetup()
    {
        _flowControl.Reset();
    }

    [Benchmark]
    public async Task ThreadsAdvanceWithWindowUpdates()
    {
        var parallelThreads = new Task[ThreadsCount + 1];
        _flowControl.Reset();
        for (var i = 0; i < ThreadsCount; i++)
        {
            var advanceTask = Task.Factory.StartNew(() =>
            {
                for (int i = 0; i < N; i++)
                {
                    if (_flowControl.TryUpdateWindow(16, out _))
                    {
                        for (int j = 0; j < Spin; j++)
                        {
                        }
                    }
                }
            }, TaskCreationOptions.LongRunning);
            parallelThreads[i] = advanceTask;
            var tAdvance = Task.Factory.StartNew(() =>
            {
                for (var i = 0; i < N; i++)
                {
                    _flowControl.TryAdvance(1);
                }
                _flowControl.Abort();
            }, TaskCreationOptions.LongRunning);
            parallelThreads[ThreadsCount] = tAdvance;
        }

        await Task.WhenAll(parallelThreads);
    }
}
