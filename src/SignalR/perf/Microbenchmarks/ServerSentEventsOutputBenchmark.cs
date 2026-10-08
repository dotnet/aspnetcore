// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http.Connections;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks;

public class ServerSentEventsOutputBenchmark
{
    private ReadOnlySequence<byte> _payload;
    private byte[] _expected;
    private OutputStream _output;

    [Params(false, true)]
    public bool ForceAsync;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _payload = ServerSentEventsFormattingBenchmark.CreatePayload(4096, 16, segmented: true, out _expected);
        _output = new OutputStream(_expected.Length, ForceAsync);
        WriteMessage().GetAwaiter().GetResult();
        VerifyOutput();
    }

    [Benchmark]
    public Task WriteMessage()
    {
        _output.Reset();
        return ServerSentEventsMessageFormatter.WriteMessageAsync(_payload, _output, default);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        try
        {
            VerifyOutput();
        }
        finally
        {
            _output.Dispose();
        }
    }

    private void VerifyOutput()
    {
        if (!_output.Written.Span.SequenceEqual(_expected))
        {
            throw new InvalidOperationException("The output stream did not receive the expected SSE payload.");
        }

        if (ForceAsync && _output.AsynchronousWrites == 0)
        {
            throw new InvalidOperationException("The asynchronous output path was not exercised.");
        }
    }

    // Wrapping MemoryStream prevents CopyToAsync from bypassing our asynchronous writes.
    private sealed class OutputStream(int capacity, bool forceAsync) : Stream
    {
        private readonly MemoryStream _buffer = new(capacity);

        public ReadOnlyMemory<byte> Written => _buffer.GetBuffer().AsMemory(0, (int)_buffer.Length);
        public int AsynchronousWrites { get; private set; }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => _buffer.Length;
        public override long Position
        {
            get => _buffer.Position;
            set => throw new NotSupportedException();
        }

        public void Reset()
        {
            _buffer.Position = 0;
            _buffer.SetLength(0);
            AsynchronousWrites = 0;
        }

        public override void Flush() => _buffer.Flush();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => _buffer.Write(buffer, offset, count);

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (forceAsync)
            {
                AsynchronousWrites++;
                await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
            }

            cancellationToken.ThrowIfCancellationRequested();
            _buffer.Write(buffer, offset, count);
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (forceAsync)
            {
                AsynchronousWrites++;
                await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
            }

            cancellationToken.ThrowIfCancellationRequested();
            _buffer.Write(buffer.Span);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _buffer.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
