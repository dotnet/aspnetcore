// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal
{
    public class LibuvOutputConsumer
    {
        private readonly LibuvThread _thread;
        private readonly UvStreamHandle _socket;
        private readonly string _connectionId;
        private readonly ILibuvTrace _log;
        private readonly IPipeReader _pipe;

        public LibuvOutputConsumer(
            IPipeReader pipe,
            LibuvThread thread,
            UvStreamHandle socket,
            string connectionId,
            ILibuvTrace log)
        {
            _pipe = pipe;
            _thread = thread;
            _socket = socket;
            _connectionId = connectionId;
            _log = log;

            _pipe.OnWriterCompleted(OnWriterCompleted, this);
        }

        public async Task WriteOutputAsync()
        {
            var pool = _thread.WriteReqPool;

            while (true)
            {
                ReadResult result;

                try
                {
                    result = await _pipe.ReadAsync();
                }
                catch
                {
                    // Handled in OnWriterCompleted
                    return;
                }

                var buffer = result.Buffer;
                var consumed = buffer.End;

                try
                {
                    if (result.IsCancelled)
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
                        }
                    }
                    else if (result.IsCompleted)
                    {
                        break;
                    }
                }
                finally
                {
                    _pipe.Advance(consumed);
                }
            }
        }

        private static void OnWriterCompleted(Exception ex, object state)
        {
            // Cut off writes if the writer is completed with an error. If a write request is pending, this will cancel it.
            if (ex != null)
            {
                var libuvOutputConsumer = (LibuvOutputConsumer)state;
                libuvOutputConsumer._socket.Dispose();
            }
        }

        private void LogWriteInfo(int status, Exception error)
        {
            if (error == null)
            {
                _log.ConnectionWriteCallback(_connectionId, status);
            }
            else
            {
                // Log connection resets at a lower (Debug) level.
                if (status == LibuvConstants.ECONNRESET)
                {
                    _log.ConnectionReset(_connectionId);
                }
                else
                {
                    _log.ConnectionError(_connectionId, error);
                }
            }
        }
    }
}
