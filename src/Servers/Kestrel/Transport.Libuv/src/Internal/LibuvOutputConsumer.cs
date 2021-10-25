// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal
{
    internal class LibuvOutputConsumer
    {
        private readonly LibuvThread _thread;
        private readonly UvStreamHandle _socket;
        private readonly string _connectionId;
        private readonly ILogger _log;
        private readonly PipeReader _pipe;

        public LibuvOutputConsumer(
            PipeReader pipe,
            LibuvThread thread,
            UvStreamHandle socket,
            string connectionId,
            ILogger log)
        {
            _pipe = pipe;
            _thread = thread;
            _socket = socket;
            _connectionId = connectionId;
            _log = log;
        }

        public async Task WriteOutputAsync()
        {
            var pool = _thread.WriteReqPool;

            while (true)
            {
                var result = await _pipe.ReadAsync();

                var buffer = result.Buffer;
                var consumed = buffer.End;

                try
                {
                    if (result.IsCanceled)
                    {
                        break;
                    }

                    if (!buffer.IsEmpty)
                    {
                        var writeReq = pool.Allocate();

                        try
                        {
                            if (_socket.IsClosed)
                            {
                                break;
                            }

                            var writeResult = await writeReq.WriteAsync(_socket, buffer);

                            LogWriteInfo(writeResult.Status, writeResult.Error);

                            if (writeResult.Error != null)
                            {
                                consumed = buffer.Start;
                                throw writeResult.Error;
                            }
                        }
                        finally
                        {
                            // Make sure we return the writeReq to the pool
                            pool.Return(writeReq);

                            // Null out writeReq so it doesn't get caught by CheckUvReqLeaks.
                            // It is rooted by a TestSink scope through Pipe continuations in
                            // ResponseTests.HttpsConnectionClosedWhenResponseDoesNotSatisfyMinimumDataRate
                            writeReq = null;
                        }
                    }

                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
                finally
                {
                    _pipe.AdvanceTo(consumed);
                }
            }
        }

        private void LogWriteInfo(int status, Exception error)
        {
            if (error == null)
            {
                LibuvTrace.ConnectionWriteCallback(_log, _connectionId, status);
            }
            else
            {
                // Log connection resets at a lower (Debug) level.
                if (status == LibuvConstants.ECANCELED)
                {
                    // Connection was aborted.
                }
                else if (LibuvConstants.IsConnectionReset(status))
                {
                    LibuvTrace.ConnectionReset(_log, _connectionId);
                }
                else
                {
                    LibuvTrace.ConnectionError(_log, _connectionId, error);
                }
            }
        }
    }
}
