// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks;

public class StreamTrackerBenchmark
{
    private const string StreamId = "stream";
    private StreamTracker _streamTracker;
    private CompletionMessage _completion;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _streamTracker = new StreamTracker(streamBufferCapacity: 10);
        _completion = CompletionMessage.Empty(StreamId);
    }

    [Benchmark]
    public object AddAndCompleteStream()
    {
        // Mirrors a client-to-server stream argument: register the stream, then complete it.
        var reader = _streamTracker.AddStream(StreamId, typeof(string), typeof(ChannelReader<string>), _streamTracker.GetNextStreamOwner());
        _streamTracker.TryComplete(_completion);
        return reader;
    }
}
