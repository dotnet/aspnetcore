// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
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
        private Task _inputTask;
        private Task _outputTask;
        private int _minAllocBufferSize;

        public DuplexPipeStreamAdapter(IDuplexPipe duplexPipe, Func<Stream, TStream> createStream) :
            this(duplexPipe, new StreamPipeReaderOptions(leaveOpen: true), new StreamPipeWriterOptions(leaveOpen: true), createStream)
        {
        }

        public DuplexPipeStreamAdapter(IDuplexPipe duplexPipe, StreamPipeReaderOptions readerOptions, StreamPipeWriterOptions writerOptions, Func<Stream, TStream> createStream) :
            base(duplexPipe.Input, duplexPipe.Output, throwOnCancelled: true)
        {
            Stream = createStream(this);

            var inputOptions = new PipeOptions(pool: readerOptions.Pool,
                                               readerScheduler: PipeScheduler.Inline,
                                               writerScheduler: PipeScheduler.Inline,
                                               minimumSegmentSize: readerOptions.BufferSize,
                                               pauseWriterThreshold: 1,
                                               resumeWriterThreshold: 1,
                                               useSynchronizationContext: false);

            var outputOptions = new PipeOptions(pool: writerOptions.Pool,
                                                readerScheduler: PipeScheduler.Inline,
                                                writerScheduler: PipeScheduler.Inline,
                                                pauseWriterThreshold: 1,
                                                resumeWriterThreshold: 1,
                                                minimumSegmentSize: writerOptions.MinimumBufferSize,
                                                useSynchronizationContext: false);

            Input = new Pipe(inputOptions);
            Output = new Pipe(outputOptions);

            _minAllocBufferSize = readerOptions.MinimumReadSize;
        }

        public ILogger Log { get; private set; }

        public TStream Stream { get; }

        private Pipe Input { get; }

        private Pipe Output { get; }

        PipeReader IDuplexPipe.Input
        {
            get
            {
                if (_inputTask == null)
                {
                    _inputTask = ReadInputAsync();
                }
                return Input.Reader;
            }
        }

        PipeWriter IDuplexPipe.Output
        {
            get
            {
                if (_outputTask == null)
                {
                    _outputTask = WriteOutputAsync();
                }

                return Output.Writer;
            }
        }

        public override async ValueTask DisposeAsync()
        {
            Output.Writer.Complete();
            Input.Reader.Complete();


            if (_outputTask != null)
            {
                // Wait for the output task to complete, this ensures that we've copied
                // the application data to the underlying stream
                await _outputTask;
            }

            // Cancel the underlying stream so that the input task yields
            CancelPendingRead();

            if (_inputTask != null)
            {
                // The input task should yield now that we've cancelled it
                await _inputTask;
            }
        }

        private async Task WriteOutputAsync()
        {
            try
            {
                while (true)
                {
                    var result = await Output.Reader.ReadAsync();
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
                        Output.Reader.AdvanceTo(buffer.End);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError(0, ex, $"{GetType().Name}.{nameof(WriteOutputAsync)}");
            }
            finally
            {
                Output.Reader.Complete();
            }
        }

        private async Task ReadInputAsync()
        {
            Exception error = null;

            try
            {
                while (true)
                {
                    var outputBuffer = Input.Writer.GetMemory(_minAllocBufferSize);
                    var bytesRead = await Stream.ReadAsync(outputBuffer);
                    Input.Writer.Advance(bytesRead);

                    if (bytesRead == 0)
                    {
                        // FIN
                        break;
                    }

                    var result = await Input.Writer.FlushAsync();

                    if (result.IsCompleted)
                    {
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
                Input.Writer.Complete(error);
            }
        }
    }
}

