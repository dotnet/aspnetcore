// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Pipelines;

// Write only stream implementation for efficiently writing bytes from the request body
internal sealed class PipeWriterStream : Stream
{
    private long _length;
    private readonly PipeWriter _pipeWriter;

    public PipeWriterStream(PipeWriter pipeWriter)
    {
        _pipeWriter = pipeWriter;
    }

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => _length;

    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        _pipeWriter.Write(new ReadOnlySpan<byte>(buffer, offset, count));
        _length += count;
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return WriteCoreAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
    {
        return WriteCoreAsync(source, cancellationToken);
    }
#endif

    private ValueTask WriteCoreAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return new ValueTask(Task.FromCanceled(cancellationToken));
        }

        _length += source.Length;
        var task = _pipeWriter.WriteAsync(source, cancellationToken);
        if (task.IsCompletedSuccessfully)
        {
            // Cancellation can be triggered by PipeWriter.CancelPendingFlush
            if (task.Result.IsCanceled)
            {
                throw new OperationCanceledException();
            }
        }
        else
        {
            return WriteSlowAsync(task);
        }

        return default;

        static async ValueTask WriteSlowAsync(ValueTask<FlushResult> flushTask)
        {
            var flushResult = await flushTask.ConfigureAwait(false);

            // Cancellation can be triggered by PipeWriter.CancelPendingFlush
            if (flushResult.IsCanceled)
            {
                throw new OperationCanceledException();
            }
        }
    }

    public void Reset()
    {
        _length = 0;
    }
}
