// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Http;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Filter
{
    public class FilteredStreamAdapter : IDisposable
    {
        private readonly string _connectionId;
        private readonly Stream _filteredStream;
        private readonly IKestrelTrace _log;
        private readonly MemoryPool _memory;
        private MemoryPoolBlock _block;
        private bool _aborted = false;

        public FilteredStreamAdapter(
            string connectionId,
            Stream filteredStream,
            MemoryPool memory,
            IKestrelTrace logger,
            IThreadPool threadPool)
        {
            SocketInput = new SocketInput(memory, threadPool);
            SocketOutput = new StreamSocketOutput(connectionId, filteredStream, memory, logger);

            _connectionId = connectionId;
            _log = logger;
            _filteredStream = filteredStream;
            _memory = memory;
        }

        public SocketInput SocketInput { get; private set; }

        public ISocketOutput SocketOutput { get; private set; }

        public Task ReadInputAsync()
        {
            _block = _memory.Lease();
            // Use pooled block for copy
            return FilterInputAsync(_block).ContinueWith((task, state) =>
            {
                ((FilteredStreamAdapter)state).OnStreamClose(task);
            }, this);
        }

        public void Abort()
        {
            _aborted = true;
        }

        public void Dispose()
        {
            SocketInput.Dispose();
        }
        
        private async Task FilterInputAsync(MemoryPoolBlock block)
        {
            int bytesRead;
            while ((bytesRead = await _filteredStream.ReadAsync(block.Array, block.Data.Offset, block.Data.Count)) != 0)
            {
                SocketInput.IncomingData(block.Array, block.Data.Offset, bytesRead);
            }
        }

        private void OnStreamClose(Task copyAsyncTask)
        {
            _memory.Return(_block);

            if (copyAsyncTask.IsFaulted)
            {
                SocketInput.AbortAwaiting();
                _log.LogError(0, copyAsyncTask.Exception, "FilteredStreamAdapter.CopyToAsync");
            }
            else if (copyAsyncTask.IsCanceled)
            {
                SocketInput.AbortAwaiting();
                _log.LogError("FilteredStreamAdapter.CopyToAsync canceled.");
            }
            else if (_aborted)
            {
                SocketInput.AbortAwaiting();
            }

            try
            {
                SocketInput.IncomingFin();
            }
            catch (Exception ex)
            {
                _log.LogError(0, ex, "FilteredStreamAdapter.OnStreamClose");
            }
        }
    }
}
