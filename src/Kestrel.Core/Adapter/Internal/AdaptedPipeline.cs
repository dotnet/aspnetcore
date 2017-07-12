// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal
{
    public class AdaptedPipeline
    {
        private const int MinAllocBufferSize = 2048;

        private readonly IKestrelTrace _trace;
        private readonly IPipe _transportOutputPipe;
        private readonly IPipeReader _transportInputPipeReader;

        public AdaptedPipeline(IPipeReader transportInputPipeReader,
                               IPipe transportOutputPipe,
                               IPipe inputPipe,
                               IPipe outputPipe,
                               IKestrelTrace trace)
        {
            _transportInputPipeReader = transportInputPipeReader;
            _transportOutputPipe = transportOutputPipe;
            Input = inputPipe;
            Output = outputPipe;
            _trace = trace;
        }

        public IPipe Input { get; }

        public IPipe Output { get; }

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
                            _transportOutputPipe.Reader.CancelPendingRead();
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
                        else if (buffer.IsSingleSpan)
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
                        Output.Reader.Advance(buffer.End);
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
                _transportOutputPipe.Writer.Complete(error);
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

                    var outputBuffer = Input.Writer.Alloc(MinAllocBufferSize);

                    var array = outputBuffer.Buffer.GetArray();
                    try
                    {
                        var bytesRead = await stream.ReadAsync(array.Array, array.Offset, array.Count);
                        outputBuffer.Advance(bytesRead);

                        if (bytesRead == 0)
                        {
                            // FIN
                            break;
                        }
                    }
                    finally
                    {
                        outputBuffer.Commit();
                    }

                    var result = await outputBuffer.FlushAsync();

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
                _transportInputPipeReader.Complete();
            }
        }
    }
}