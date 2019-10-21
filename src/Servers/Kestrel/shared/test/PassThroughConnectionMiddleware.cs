// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Testing
{
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
                private PipeWriter _output;

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
            }

            private class PassThroughPipeReader : PipeReader
            {
                private PipeReader _input;

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
}
