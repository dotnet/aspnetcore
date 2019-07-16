// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    /// <summary>
    /// A helper for wrapping a Stream decorator from an <see cref="IDuplexPipe"/>.
    /// </summary>
    /// <typeparam name="TStream"></typeparam>
    internal class DuplexPipeStreamAdapter<TStream> : DuplexPipeStream, IDuplexPipe where TStream : Stream
    {
        private readonly Pipe _input;
        private readonly Pipe _output;
        private Task _inputTask;
        private Task _outputTask;
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

            var outputOptions = new PipeOptions(pool: writerOptions.Pool,
                                                readerScheduler: PipeScheduler.Inline,
                                                writerScheduler: PipeScheduler.Inline,
                                                pauseWriterThreshold: 1,
                                                resumeWriterThreshold: 1,
                                                minimumSegmentSize: writerOptions.MinimumBufferSize,
                                                useSynchronizationContext: false);

            _minAllocBufferSize = writerOptions.MinimumBufferSize;

            _input = new Pipe(inputOptions);

            // We're using a pipe here because the HTTP/2 stack in Kestrel currently makes assumptions
            // about when it is ok to write to the PipeWriter. This should be reverted back to PipeWriter.Create once
            // those patterns are fixed.
            _output = new Pipe(outputOptions);
        }

        public ILogger Log { get; set; }

        public TStream Stream { get; }

        public PipeReader Input
        {
            get
            {
                if (_inputTask == null)
                {
                    RunAsync();
                }

                return _input.Reader;
            }
        }

        public PipeWriter Output
        {
            get
            {
                if (_outputTask == null)
                {
                    RunAsync();
                }

                return _output.Writer;
            }
        }

        public void RunAsync()
        {
            _inputTask = ReadInputAsync();
            _outputTask = WriteOutputAsync();
        }

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

            _output.Writer.Complete();
            _input.Reader.Complete();

            if (_outputTask == null)
            {
                return;
            }

            if (_outputTask != null)
            {
                await _outputTask;
            }
            
            CancelPendingRead();
            
            if (_inputTask != null)
            {
                await _inputTask;
            }
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

        private async Task WriteOutputAsync()
        {
            try
            {
                while (true)
                {
                    var result = await _output.Reader.ReadAsync();
                    var buffer = result.Buffer;

                    try
                    {
                        if (buffer.IsEmpty)
                        {
                            if (result.IsCompleted)
                            {
                                break;
                            }

                            await Stream.FlushAsync();
                        }
                        else if (buffer.IsSingleSegment)
                        {
                            await Stream.WriteAsync(buffer.First);
                        }
                        else
                        {
                            foreach (var memory in buffer)
                            {
                                await Stream.WriteAsync(memory);
                            }
                        }
                    }
                    finally
                    {
                        _output.Reader.AdvanceTo(buffer.End);
                    }
                }
            }
            catch (Exception ex)
            {
                Log?.LogCritical(0, ex, $"{GetType().Name}.{nameof(WriteOutputAsync)}");
            }
            finally
            {
                _output.Reader.Complete();
            }
        }
    }
}

