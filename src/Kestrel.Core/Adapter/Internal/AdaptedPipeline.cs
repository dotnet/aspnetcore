// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using System.IO.Pipelines;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal
{
    public class AdaptedPipeline : IDuplexPipe
    {
        private const int MinAllocBufferSize = 2048;

        private readonly IDuplexPipe _transport;
        private readonly IDuplexPipe _application;

        public AdaptedPipeline(IDuplexPipe transport,
                               IDuplexPipe application,
                               Pipe inputPipe,
                               Pipe outputPipe)
        {
            _transport = transport;
            _application = application;
            Input = inputPipe;
            Output = outputPipe;
        }

        public Pipe Input { get; }

        public Pipe Output { get; }

        PipeReader IDuplexPipe.Input => Input.Reader;

        PipeWriter IDuplexPipe.Output => Output.Writer;

        public async Task RunAsync(Stream stream)
        {
            var inputTask = ReadInputAsync(stream);
            var outputTask = WriteOutputAsync(stream);

            await inputTask;
            await outputTask;
        }

        private async Task WriteOutputAsync(Stream stream)
        {
            Exception error = null;

            try
            {
                if (stream == null)
                {
                    return;
                }

                while (true)
                {
                    var result = await Output.Reader.ReadAsync();
                    var buffer = result.Buffer;

                    try
                    {
                        if (result.IsCancelled)
                        {
                            // Forward the cancellation to the transport pipe
                            _application.Input.CancelPendingRead();
                            break;
                        }

                        if (buffer.IsEmpty)
                        {
                            if (result.IsCompleted)
                            {
                                break;
                            }
                            await stream.FlushAsync();
                        }
                        else if (buffer.IsSingleSegment)
                        {
                            var array = buffer.First.GetArray();
                            await stream.WriteAsync(array.Array, array.Offset, array.Count);
                        }
                        else
                        {
                            foreach (var memory in buffer)
                            {
                                var array = memory.GetArray();
                                await stream.WriteAsync(array.Array, array.Offset, array.Count);
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
                error = ex;
            }
            finally
            {
                Output.Reader.Complete();
                _transport.Output.Complete();
            }
        }

        private async Task ReadInputAsync(Stream stream)
        {
            Exception error = null;

            try
            {
                if (stream == null)
                {
                    // REVIEW: Do we need an exception here?
                    return;
                }

                while (true)
                {

                    var outputBuffer = Input.Writer.GetMemory(MinAllocBufferSize);

                    var array = outputBuffer.GetArray();
                    try
                    {
                        var bytesRead = await stream.ReadAsync(array.Array, array.Offset, array.Count);
                        Input.Writer.Advance(bytesRead);

                        if (bytesRead == 0)
                        {
                            // FIN
                            break;
                        }
                    }
                    finally
                    {
                        Input.Writer.Commit();
                    }

                    var result = await Input.Writer.FlushAsync();

                    if (result.IsCompleted)
                    {
                        break;
                    }

                }
            }
            catch (Exception ex)
            {
                // Don't rethrow the exception. It should be handled by the Pipeline consumer.
                error = ex;
            }
            finally
            {
                Input.Writer.Complete(error);
                // The application could have ended the input pipe so complete
                // the transport pipe as well
                _transport.Input.Complete();
            }
        }

        public void Dispose()
        {
        }
    }
}
