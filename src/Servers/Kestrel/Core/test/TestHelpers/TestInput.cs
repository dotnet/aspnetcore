// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Server.Kestrel.Https.Internal;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

class TestInput : IDisposable
{
    private readonly MemoryPool<byte> _memoryPool;

    public TestInput(KestrelTrace log = null, ITimeoutControl timeoutControl = null)
    {
        _memoryPool = TestMemoryPoolFactory.Create();
        var options = new PipeOptions(pool: _memoryPool, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false);
        var pair = DuplexPipe.CreateConnectionPair(options, options);
        Transport = pair.Transport;
        Application = pair.Application;

        var connectionContext = new TestConnectionContext();
        var metricsContext = TestContextFactory.CreateMetricsContext(connectionContext);

        var connectionFeatures = new FeatureCollection();
        connectionFeatures.Set<IConnectionLifetimeFeature>(new TestConnectionLifetimeFeature());
        connectionFeatures.Set<IConnectionMetricsContextFeature>(new TestConnectionMetricsContextFeature { MetricsContext = metricsContext });

        Http1ConnectionContext = TestContextFactory.CreateHttpConnectionContext(
            serviceContext: new TestServiceContext
            {
                Log = log ?? new KestrelTrace(NullLoggerFactory.Instance)
            },
            connectionContext: connectionContext,
            transport: Transport,
            timeoutControl: timeoutControl ?? new TestTimeoutControl(),
            memoryPool: _memoryPool,
            connectionFeatures: connectionFeatures,
            metricsContext: metricsContext);

        Http1Connection = new Http1Connection(Http1ConnectionContext);
        Http1Connection.HttpResponseControl = new TestHttpResponseControl();
        Http1Connection.Reset();
    }

    public IDuplexPipe Transport { get; }

    public IDuplexPipe Application { get; }

    public HttpConnectionContext Http1ConnectionContext { get; }

    public Http1Connection Http1Connection { get; set; }

    public void Add(string text)
    {
        var data = Encoding.ASCII.GetBytes(text);
        async Task Write() => await Application.Output.WriteAsync(data);
        Write().Wait();
    }

    public void Fin()
    {
        Application.Output.Complete();
    }

    public void Cancel()
    {
        Transport.Input.CancelPendingRead();
    }

    public void Dispose()
    {
        Application.Input.Complete();
        Application.Output.Complete();
        Transport.Input.Complete();
        Transport.Output.Complete();
        _memoryPool.Dispose();
    }
}

internal sealed class TestBodyControlFeature : IHttpBodyControlFeature
{
    public bool AllowSynchronousIO { get; set; }
}

internal static class TestDuplexPipe
{
    public static IDuplexPipe Create() => new DuplexPipe(PipeReader.Create(Stream.Null), PipeWriter.Create(Stream.Null));
}

internal sealed class TestHttpResponseControl : IHttpResponseControl
{
    private byte[] _memory = new byte[4096];

    public Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask<FlushResult>> WritePipeAsyncCallback { get; set; }
        = (_, _) => new ValueTask<FlushResult>(new FlushResult());

    public long UnflushedBytes { get; set; }

    public ValueTask<FlushResult> ProduceContinueAsync() => new ValueTask<FlushResult>(new FlushResult());

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        if (_memory.Length < sizeHint)
        {
            _memory = new byte[sizeHint];
        }

        return _memory;
    }

    public Span<byte> GetSpan(int sizeHint = 0) => GetMemory(sizeHint).Span;

    public void Advance(int bytes)
    {
    }

    public ValueTask<FlushResult> FlushPipeAsync(CancellationToken cancellationToken) => new ValueTask<FlushResult>(new FlushResult());

    public ValueTask<FlushResult> WritePipeAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken)
        => WritePipeAsyncCallback(source, cancellationToken);

    public void CancelPendingFlush()
    {
    }

    public Task CompleteAsync(Exception exception = null) => Task.CompletedTask;
}

internal sealed class TestTimeoutControl : ITimeoutControl
{
    public List<(TimeSpan Timeout, TimeoutReason Reason)> SetTimeoutCalls { get; } = new List<(TimeSpan Timeout, TimeoutReason Reason)>();
    public List<(TimeSpan Timeout, TimeoutReason Reason)> ResetTimeoutCalls { get; } = new List<(TimeSpan Timeout, TimeoutReason Reason)>();
    public List<MinDataRate> StartRequestBodyCalls { get; } = new List<MinDataRate>();

    public TimeoutReason TimerReason { get; set; }
    public int CancelTimeoutCount { get; private set; }
    public int InitializeHttp2Count { get; private set; }
    public int TickCount { get; private set; }
    public int StopRequestBodyCount { get; private set; }
    public int StartTimingReadCount { get; private set; }
    public int StopTimingReadCount { get; private set; }
    public int BytesReadCount { get; private set; }
    public int StartTimingWriteCount { get; private set; }
    public int StopTimingWriteCount { get; private set; }
    public int BytesWrittenToBufferCount { get; private set; }
    public Func<long, MinDataRate, long> GetResponseDrainDeadlineFunc { get; set; } = (_, _) => 0;

    public void SetTimeout(TimeSpan timeout, TimeoutReason timeoutReason)
    {
        SetTimeoutCalls.Add((timeout, timeoutReason));
    }

    public void ResetTimeout(TimeSpan timeout, TimeoutReason timeoutReason)
    {
        ResetTimeoutCalls.Add((timeout, timeoutReason));
    }

    public void CancelTimeout()
    {
        CancelTimeoutCount++;
    }

    public void InitializeHttp2(InputFlowControl connectionInputFlowControl)
    {
        InitializeHttp2Count++;
    }

    public void Tick(long timestamp)
    {
        TickCount++;
    }

    public void StartRequestBody(MinDataRate minRate)
    {
        StartRequestBodyCalls.Add(minRate);
    }

    public void StopRequestBody()
    {
        StopRequestBodyCount++;
    }

    public void StartTimingRead()
    {
        StartTimingReadCount++;
    }

    public void StopTimingRead()
    {
        StopTimingReadCount++;
    }

    public void BytesRead(long count)
    {
        BytesReadCount++;
    }

    public void StartTimingWrite()
    {
        StartTimingWriteCount++;
    }

    public void StopTimingWrite()
    {
        StopTimingWriteCount++;
    }

    public void BytesWrittenToBuffer(MinDataRate minRate, long count)
    {
        BytesWrittenToBufferCount++;
    }

    public long GetResponseDrainDeadline(long timestamp, MinDataRate minRate) => GetResponseDrainDeadlineFunc(timestamp, minRate);
}

internal sealed class TestHeartbeatHandler : IHeartbeatHandler
{
    public Action OnHeartbeatCallback { get; set; } = () => { };

    public int OnHeartbeatCount { get; private set; }

    public void OnHeartbeat()
    {
        OnHeartbeatCount++;
        OnHeartbeatCallback();
    }
}

internal class TestMessageBody : MessageBody
{
    public TestMessageBody()
        : base(null)
    {
    }

    public Func<CancellationToken, ValueTask<ReadResult>> ReadAsyncFunc { get; set; }
        = _ => new ValueTask<ReadResult>(new ReadResult(default, isCanceled: false, isCompleted: true));

    public int ConsumeAsyncCount { get; private set; }

    public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default) => ReadAsyncFunc(cancellationToken);

    public override bool TryRead(out ReadResult readResult)
    {
        readResult = default;
        return false;
    }

    public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
    {
    }

    public override void CancelPendingRead()
    {
    }

    public override void Complete(Exception exception)
    {
    }

    public override Task ConsumeAsync()
    {
        ConsumeAsyncCount++;
        return Task.CompletedTask;
    }
}

internal sealed class TestConnectionLifetimeFeature : IConnectionLifetimeFeature
{
    public CancellationToken ConnectionClosed { get; set; }

    public int AbortCount { get; private set; }

    public void Abort()
    {
        AbortCount++;
    }
}

internal sealed class TestPipeScheduler : PipeScheduler
{
    public override void Schedule(Action<object> action, object state)
    {
    }
}

internal sealed class TestStreamIdFeature : IStreamIdFeature
{
    public long StreamId { get; set; }
}

internal sealed class TestLoggerFactory : ILoggerFactory
{
    public List<string> Categories { get; } = new List<string>();

    public ILogger Logger { get; set; } = NullLogger.Instance;

    public void AddProvider(ILoggerProvider provider)
    {
    }

    public ILogger CreateLogger(string categoryName)
    {
        Categories.Add(categoryName);
        return Logger;
    }

    public void Dispose()
    {
    }
}
internal sealed class TestHttpOutputAborter : IHttpOutputAborter
{
    public int AbortCount { get; private set; }

    public int OnInputOrOutputCompletedCount { get; private set; }

    public void Abort(ConnectionAbortedException abortReason, ConnectionEndReason reason)
    {
        AbortCount++;
    }

    public void OnInputOrOutputCompleted()
    {
        OnInputOrOutputCompletedCount++;
    }
}

internal sealed class TestMinResponseDataRateFeature : IHttpMinResponseDataRateFeature
{
    public MinDataRate MinDataRate { get; set; }
}

internal sealed class TestMemoryPool : MemoryPool<byte>
{
    public override int MaxBufferSize => 4096;

    public override IMemoryOwner<byte> Rent(int minBufferSize = -1) => new TestMemoryOwner(Math.Max(minBufferSize, 4096));

    protected override void Dispose(bool disposing)
    {
    }

    private sealed class TestMemoryOwner : IMemoryOwner<byte>
    {
        private readonly int _size;

        public TestMemoryOwner(int size)
        {
            _size = size;
        }

        public Memory<byte> Memory
        {
            get
            {
                var memory = new byte[_size];
                memory.AsSpan().Fill(0xff);
                return memory;
            }
        }

        public void Dispose()
        {
        }
    }
}

internal sealed class TestDisposable : IDisposable
{
    public void Dispose()
    {
    }
}

