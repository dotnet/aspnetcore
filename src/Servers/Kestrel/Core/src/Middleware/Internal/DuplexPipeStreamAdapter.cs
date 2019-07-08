// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    /// <summary>
    /// A helper for wrapping a Stream decorator from an <see cref="IDuplexPipe"/>.
    /// </summary>
    /// <typeparam name="TStream"></typeparam>
    internal class DuplexPipeStreamAdapter<TStream> : DuplexPipeStream, IDuplexPipe where TStream : Stream
    {
        private readonly Pipe _output;
        private Task _outputTask;
        private bool _disposed;
        private readonly object _disposeLock = new object();

        public DuplexPipeStreamAdapter(IDuplexPipe duplexPipe, Func<Stream, TStream> createStream) :
            this(duplexPipe, new StreamPipeReaderOptions(leaveOpen: true), new StreamPipeWriterOptions(leaveOpen: true), createStream)
        {
        }

        public DuplexPipeStreamAdapter(IDuplexPipe duplexPipe, StreamPipeReaderOptions readerOptions, StreamPipeWriterOptions writerOptions, Func<Stream, TStream> createStream) :
            base(duplexPipe.Input, duplexPipe.Output)
        {
            Stream = createStream(this);

            var outputOptions = new PipeOptions(pool: writerOptions.Pool,
                                                readerScheduler: PipeScheduler.Inline,
                                                writerScheduler: PipeScheduler.Inline,
                                                pauseWriterThreshold: 1,
                                                resumeWriterThreshold: 1,
                                                minimumSegmentSize: writerOptions.MinimumBufferSize,
                                                useSynchronizationContext: false);

            Input = PipeReader.Create(Stream, readerOptions);

            // We're using a pipe here because the HTTP/2 stack in Kestrel currently makes assumptions
            // about when it is ok to write to the PipeWriter. This should be reverted back to PipeWriter.Create once
            // those patterns are fixed.
            _output = new Pipe(outputOptions);
        }

        public ILogger Log { get; set; }

        public TStream Stream { get; }

        public PipeReader Input { get; }

        public PipeWriter Output
        {
            get
            {
                if (_outputTask == null)
                {
                    _outputTask = WriteOutputAsync();
                }

                return _output.Writer;
            }
        }

        public override ValueTask DisposeAsync()
        {
            lock (_disposeLock)
            {
                if (_disposed)
                {
                    return default;
                }
                _disposed = true;
            }

            Input.Complete();
            _output.Writer.Complete();

            if (_outputTask == null || _outputTask.IsCompletedSuccessfully)
            {
                // Wait for the output task to complete, this ensures that we've copied
                // the application data to the underlying stream
                return default;
            }

            return new ValueTask(_outputTask);
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

