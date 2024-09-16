// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.InternalTesting;

public class PassThroughConnectionMiddleware
{
    private readonly ConnectionDelegate _next;

    public PassThroughConnectionMiddleware(ConnectionDelegate next)
    {
        _next = next;
    }

    public Task OnConnectionAsync(ConnectionContext context)
    {
        context.Transport = new PassThroughDuplexPipe(context.Transport);
        return _next(context);
    }

    private class PassThroughDuplexPipe : IDuplexPipe
    {
        public PassThroughDuplexPipe(IDuplexPipe duplexPipe)
        {
            Input = new PassThroughPipeReader(duplexPipe.Input);
            Output = new PassThroughPipeWriter(duplexPipe.Output);
        }

        public PipeReader Input { get; }

        public PipeWriter Output { get; }

        private class PassThroughPipeWriter : PipeWriter
        {
            private readonly PipeWriter _output;

            public PassThroughPipeWriter(PipeWriter output)
            {
                _output = output;
            }

            public override void Advance(int bytes) => _output.Advance(bytes);

            public override void CancelPendingFlush() => _output.CancelPendingFlush();

            public override void Complete(Exception exception = null) => _output.Complete(exception);

            public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default) => _output.FlushAsync(cancellationToken);

            public override Memory<byte> GetMemory(int sizeHint = 0) => _output.GetMemory(sizeHint);

            public override Span<byte> GetSpan(int sizeHint = 0) => _output.GetSpan(sizeHint);

            public override Stream AsStream(bool leaveOpen = false) => throw new InvalidOperationException("Missing override");

            public override bool CanGetUnflushedBytes => throw new InvalidOperationException("Missing override");

            public override ValueTask CompleteAsync(Exception exception = null) => throw new InvalidOperationException("Missing override");

            protected override Task CopyFromAsync(Stream source, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Missing override");

#pragma warning disable CS0672 // Member overrides obsolete member
            public override void OnReaderCompleted(Action<Exception, object> callback, object state) => throw new InvalidOperationException("Missing override");
#pragma warning restore CS0672 // Member overrides obsolete member

            public override long UnflushedBytes => throw new InvalidOperationException("Missing override");

            public override ValueTask<FlushResult> WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Missing override");
        }

        private class PassThroughPipeReader : PipeReader
        {
            private readonly PipeReader _input;

            public PassThroughPipeReader(PipeReader input)
            {
                _input = input;
            }

            public override void AdvanceTo(SequencePosition consumed) => _input.AdvanceTo(consumed);

            public override void AdvanceTo(SequencePosition consumed, SequencePosition examined) => _input.AdvanceTo(consumed, examined);

            public override void CancelPendingRead() => _input.CancelPendingRead();

            public override void Complete(Exception exception = null) => _input.Complete(exception);

            public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default) => _input.ReadAsync(cancellationToken);

            public override bool TryRead(out ReadResult result) => _input.TryRead(out result);

            public override Stream AsStream(bool leaveOpen = false) => throw new InvalidOperationException("Missing override");

            public override ValueTask CompleteAsync(Exception exception = null) => throw new InvalidOperationException("Missing override");

            public override Task CopyToAsync(PipeWriter destination, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Missing override");

            public override Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Missing override");

#pragma warning disable CS0672 // Member overrides obsolete member
            public override void OnWriterCompleted(Action<Exception, object> callback, object state) => throw new InvalidOperationException("Missing override");
#pragma warning restore CS0672 // Member overrides obsolete member

            protected override ValueTask<ReadResult> ReadAtLeastAsyncCore(int minimumSize, CancellationToken cancellationToken) => throw new InvalidOperationException("Missing override");
        }
    }
}

public static class PassThroughConnectionMiddlewareExtensions
{
    public static TBuilder UsePassThrough<TBuilder>(this TBuilder builder) where TBuilder : IConnectionBuilder
    {
        builder.Use(next => new PassThroughConnectionMiddleware(next).OnConnectionAsync);
        return builder;
    }
}
