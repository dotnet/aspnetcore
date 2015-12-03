// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Server.Kestrel.Http;
using Microsoft.AspNet.Server.Kestrel.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Server.Kestrel.Filter
{
    public class FilteredStreamAdapter
    {
        private readonly Stream _filteredStream;
        private readonly Stream _socketInputStream;
        private readonly IKestrelTrace _log;

        public FilteredStreamAdapter(
            Stream filteredStream,
            MemoryPool2 memory,
            IKestrelTrace logger)
        {
            SocketInput = new SocketInput(memory);
            SocketOutput = new StreamSocketOutput(filteredStream, memory);

            _log = logger;
            _filteredStream = filteredStream;
            _socketInputStream = new SocketInputStream(SocketInput);

            // Don't use 81920 byte buffer
            _filteredStream.CopyToAsync(_socketInputStream, 4096).ContinueWith((task, state) =>
            {
                ((FilteredStreamAdapter)state).OnStreamClose(task);
            }, this);
        }

        public SocketInput SocketInput { get; private set; }

        public ISocketOutput SocketOutput { get; private set; }

        private void OnStreamClose(Task copyAsyncTask)
        {
            if (copyAsyncTask.IsFaulted)
            {
                _log.LogError("FilteredStreamAdapter.CopyToAsync", copyAsyncTask.Exception);
            }
            else if (copyAsyncTask.IsCanceled)
            {
                _log.LogError("FilteredStreamAdapter.CopyToAsync canceled.");
            }

            try
            {
                _filteredStream.Dispose();
                _socketInputStream.Dispose();
            }
            catch (Exception ex)
            {
                _log.LogError("FilteredStreamAdapter.OnStreamClose", ex);
            }
        }
    }
}
