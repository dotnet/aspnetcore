// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal
{
    internal class AdaptedPipeline : IDuplexPipe
    {
        private readonly int _minAllocBufferSize;

        private readonly IDuplexPipe _transport;

        public AdaptedPipeline(IDuplexPipe transport,
                               Pipe inputPipe,
                               Pipe outputPipe,
                               IKestrelTrace log,
                               int minAllocBufferSize)
        {
            _transport = transport;
            Input = inputPipe;
            Output = outputPipe;
            Log = log;
            _minAllocBufferSize = minAllocBufferSize;
        }

        public Pipe Input { get; }

        public Pipe Output { get; }

        public IKestrelTrace Log { get; }

        PipeReader IDuplexPipe.Input => Input.Reader;

        PipeWriter IDuplexPipe.Output => Output.Writer;

        public async Task RunAsync(Stream stream)
        {
            var inputTask = ReadInputAsync(stream);
            var outputTask = WriteOutputAsync(stream);

            await outputTask;
            // Yield the current request
            _transport.Input.CancelPendingRead();
            await inputTask;
        }

        public void Complete()
        {
            Output.Writer.Complete();
            Input.Reader.Complete();
        }

        private async Task WriteOutputAsync(Stream stream)
        {
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
                            await stream.WriteAsync(buffer.First);
                        }
                        else
                        {
                            foreach (var memory in buffer)
                            {
                                await stream.WriteAsync(memory);
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
                Log.LogError(0, ex, $"{nameof(AdaptedPipeline)}.{nameof(WriteOutputAsync)}");
            }
            finally
            {
                Output.Reader.Complete();
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
                    var outputBuffer = Input.Writer.GetMemory(_minAllocBufferSize);
                    var bytesRead = await stream.ReadAsync(outputBuffer);
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
