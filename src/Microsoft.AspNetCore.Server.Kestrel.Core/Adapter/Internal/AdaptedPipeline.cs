// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal
{
    public class AdaptedPipeline
    {
        private const int MinAllocBufferSize = 2048;

        private readonly Stream _filteredStream;
        private readonly StreamSocketOutput _output;
        private readonly IKestrelTrace _trace;

        public AdaptedPipeline(
            Stream filteredStream,
            IPipe inputPipe,
            IPipe outputPipe,
            IKestrelTrace trace)
        {
            Input = inputPipe;
            _output = new StreamSocketOutput(filteredStream, outputPipe);
            _filteredStream = filteredStream;
            _trace = trace;
        }

        public IPipe Input { get; }

        public ISocketOutput Output => _output;

        public async Task RunAsync()
        {
            try
            {
                var inputTask = ReadInputAsync();
                var outputTask = _output.WriteOutputAsync();

                await inputTask;

                _output.Dispose();

                await outputTask;
            }
            catch (Exception ex)
            {
                // adaptedPipeline.RunAsync() shouldn't throw, unless filtered stream's WriteAsync throws.
                _trace.LogError(0, ex, $"{nameof(AdaptedPipeline)}.{nameof(RunAsync)}");
            }
        }

        private async Task ReadInputAsync()
        {
            int bytesRead;

            do
            {
                var block = Input.Writer.Alloc(MinAllocBufferSize);

                try
                {
                    var array = block.Buffer.GetArray();
                    try
                    {
                        bytesRead = await _filteredStream.ReadAsync(array.Array, array.Offset, array.Count);
                        block.Advance(bytesRead);
                    }
                    finally
                    {
                        await block.FlushAsync();
                    }
                }
                catch (Exception ex)
                {
                    Input.Writer.Complete(ex);

                    // Don't rethrow the exception. It should be handled by the Pipeline consumer.
                    return;
                }
            } while (bytesRead != 0);

            Input.Writer.Complete();
        }
    }
}
