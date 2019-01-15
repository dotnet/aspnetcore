// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal
{
    public class AdaptedPipeline : IDuplexPipe
    {
        private static readonly int MinAllocBufferSize = KestrelMemoryPool.MinimumSegmentSize / 2;

        private readonly IDuplexPipe _transport;

        public AdaptedPipeline(IDuplexPipe transport,
                               Pipe inputPipe,
                               Pipe outputPipe,
                               IKestrelTrace log)
        {
            _transport = transport;
            Input = inputPipe;
            Output = outputPipe;
            Log = log;
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

            await inputTask;
            await outputTask;
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
    }
}
