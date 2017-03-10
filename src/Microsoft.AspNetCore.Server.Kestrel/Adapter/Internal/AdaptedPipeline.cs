// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Adapter.Internal
{
    public class AdaptedPipeline : IDisposable
    {
        private const int MinAllocBufferSize = 2048;

        private readonly Stream _filteredStream;
        private readonly StreamSocketOutput _output;

        public AdaptedPipeline(
            Stream filteredStream,
            IPipe inputPipe,
            IPipe outputPipe)
        {
            Input = inputPipe;
            _output = new StreamSocketOutput(filteredStream, outputPipe);

            _filteredStream = filteredStream;
        }

        public IPipe Input { get; }

        public ISocketOutput Output => _output;

        public void Dispose()
        {
            Input.Writer.Complete();
        }

        public async Task StartAsync()
        {
            var inputTask = ReadInputAsync();
            var outputTask = _output.WriteOutputAsync();

            var result = await Task.WhenAny(inputTask, outputTask);

            if (result == inputTask)
            {
                // Close output
                _output.Dispose();
            }
            else
            {
                // Close input
                Input.Writer.Complete();
            }

            await Task.WhenAll(inputTask, outputTask);
        }

        private async Task ReadInputAsync()
        {
            int bytesRead;

            do
            {
                var block = Input.Writer.Alloc(MinAllocBufferSize);

                try
                {
                    var array = block.Memory.GetArray();
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
