// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Filter.Internal
{
    public class FilteredStreamAdapter : IDisposable
    {
        private readonly Stream _filteredStream;

        public FilteredStreamAdapter(
            string connectionId,
            Stream filteredStream,
            MemoryPool memory,
            IKestrelTrace logger,
            IThreadPool threadPool,
            IBufferSizeControl bufferSizeControl)
        {
            SocketInput = new SocketInput(memory, threadPool, bufferSizeControl);
            SocketOutput = new StreamSocketOutput(connectionId, filteredStream, memory, logger);

            _filteredStream = filteredStream;
        }

        public SocketInput SocketInput { get; }

        public ISocketOutput SocketOutput { get; }

        public void Dispose()
        {
            SocketInput.Dispose();
        }

        public async Task ReadInputAsync()
        {
            int bytesRead;

            do
            {
                var block = SocketInput.IncomingStart();

                try
                {
                    var count = block.Data.Offset + block.Data.Count - block.End;
                    bytesRead = await _filteredStream.ReadAsync(block.Array, block.End, count);
                }
                catch (Exception ex)
                {
                    SocketInput.IncomingComplete(0, ex);
                    throw;
                }

                SocketInput.IncomingComplete(bytesRead, error: null);
            } while (bytesRead != 0);
        }
    }
}
