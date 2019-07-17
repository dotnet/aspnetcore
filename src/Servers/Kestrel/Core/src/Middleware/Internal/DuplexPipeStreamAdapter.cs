// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    /// <summary>
    /// A helper for wrapping a Stream decorator from an <see cref="IDuplexPipe"/>.
    /// </summary>
    /// <typeparam name="TStream"></typeparam>
    internal class DuplexPipeStreamAdapter<TStream> : DuplexPipeStream, IDuplexPipe where TStream : Stream
    {
        private readonly Pipe _input;
        private Task _inputTask;
        private bool _disposed;
        private readonly object _disposeLock = new object();
        private readonly int _minAllocBufferSize;

        public DuplexPipeStreamAdapter(IDuplexPipe duplexPipe, Func<Stream, TStream> createStream) :
            this(duplexPipe, new StreamPipeReaderOptions(leaveOpen: true), new StreamPipeWriterOptions(leaveOpen: true), createStream)
        {
        }

        public DuplexPipeStreamAdapter(IDuplexPipe duplexPipe, StreamPipeReaderOptions readerOptions, StreamPipeWriterOptions writerOptions, Func<Stream, TStream> createStream) :
            base(duplexPipe.Input, duplexPipe.Output, throwOnCancelled: true)
        {
            Stream = createStream(this);

            var inputOptions = new PipeOptions(pool: readerOptions.Pool,
                readerScheduler: PipeScheduler.ThreadPool,
                writerScheduler: PipeScheduler.Inline,
                pauseWriterThreshold: 1,
                resumeWriterThreshold: 1,
                minimumSegmentSize: readerOptions.Pool.GetMinimumSegmentSize(),
                useSynchronizationContext: false);

            _minAllocBufferSize = writerOptions.MinimumBufferSize;

            _input = new Pipe(inputOptions);
            Output = PipeWriter.Create(Stream, writerOptions);
        }

        public TStream Stream { get; }

        public PipeReader Input
        {
            get
            {
                if (_inputTask == null)
                {
                    _inputTask = ReadInputAsync();
                }

                return _input.Reader;
            }
        }

        public PipeWriter Output { get; }

        public override async ValueTask DisposeAsync()
        {
            lock (_disposeLock)
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;
            }

            _input.Reader.Complete();
            Output.Complete();

            CancelPendingRead();

            if (_inputTask != null)
            {
                await _inputTask;
            }
        }

        protected override void Dispose(bool disposing)
        {
            throw new NotSupportedException();
        }

        private async Task ReadInputAsync()
        {
            Exception error = null;
            try
            {
                while (true)
                {
                    var outputBuffer = _input.Writer.GetMemory(_minAllocBufferSize);

                    var bytesRead = await Stream.ReadAsync(outputBuffer);
                    _input.Writer.Advance(bytesRead);

                    if (bytesRead == 0)
                    {
                        // FIN
                        break;
                    }

                    var result = await _input.Writer.FlushAsync();

                    if (result.IsCompleted)
                    {
                        // flushResult should not be canceled.
                        break;
                    }
                }

            }
            catch (OperationCanceledException ex)
            {
                // Propagate the exception if it's ConnectionAbortedException	
                error = ex as ConnectionAbortedException;
            }
            catch (Exception ex)
            {
                // Don't rethrow the exception. It should be handled by the Pipeline consumer.	
                error = ex;
            }
            finally
            {
                _input.Writer.Complete(error);
            }
        }
    }
}

