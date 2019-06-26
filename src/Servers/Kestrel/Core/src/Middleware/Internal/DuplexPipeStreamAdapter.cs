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
        private Task _outputTask;

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
            Output = new Pipe(outputOptions);
        }

        public ILogger Log { get; set; }

        public TStream Stream { get; }

        private Pipe Output { get; }

        public PipeReader Input { get; }
        
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
            Input.Complete();
            Output.Writer.Complete();

            if (_outputTask != null)
            {
                // Wait for the output task to complete, this ensures that we've copied
                // the application data to the underlying stream
                await _outputTask;
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
                Log?.LogError(0, ex, $"{GetType().Name}.{nameof(WriteOutputAsync)}");
            }
            finally
            {
                Output.Reader.Complete();
            }
        }
    }
}

