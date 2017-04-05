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
        private readonly LibuvConnection _connection;
        private readonly string _connectionId;
        private readonly ILibuvTrace _log;

        private readonly WriteReqPool _writeReqPool;
        private readonly IPipeReader _pipe;

        public LibuvOutputConsumer(
            IPipeReader pipe,
            LibuvThread thread,
            UvStreamHandle socket,
            LibuvConnection connection,
            string connectionId,
            ILibuvTrace log)
        {
            _pipe = pipe;
            // We need to have empty pipe at this moment so callback
            // get's scheduled
            _thread = thread;
            _socket = socket;
            _connection = connection;
            _connectionId = connectionId;
            _log = log;
            _writeReqPool = thread.WriteReqPool;
        }

        public async Task StartWrites()
        {
            while (true)
            {
                var result = await _pipe.ReadAsync();
                var buffer = result.Buffer;

                try
                {
                    if (!buffer.IsEmpty)
                    {
                        var writeReq = _writeReqPool.Allocate();
                        var writeResult = await writeReq.WriteAsync(_socket, buffer);
                        _writeReqPool.Return(writeReq);

                        OnWriteCompleted(writeResult.Status, writeResult.Error);
                    }

                    if (result.IsCancelled)
                    {
                        // Send a FIN
                        await ShutdownAsync();
                        // Ensure no data is written after uv_shutdown
                        break;
                    }

                    if (buffer.IsEmpty && result.IsCompleted)
                    {
                        break;
                    }
                }
                finally
                {
                    _pipe.Advance(result.Buffer.End);
                }
            }

            // We're done reading
            _pipe.Complete();

            // Close the connection
            _connection.Close();
        }

        private void OnWriteCompleted(int writeStatus, Exception writeError)
        {
            // Called inside _contextLock
            var status = writeStatus;
            var error = writeError;

            if (error != null)
            {
                // Abort the connection for any failed write
                // Queued on threadpool so get it in as first op.
                _connection.AbortAsync();
            }

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

        private Task ShutdownAsync()
        {
            var tcs = new TaskCompletionSource<object>();
            _log.ConnectionWriteFin(_connectionId);

            var shutdownReq = new UvShutdownReq(_log);
            shutdownReq.Init(_thread.Loop);
            shutdownReq.Shutdown(_socket, (req, status, state) =>
            {
                req.Dispose();
                _log.ConnectionWroteFin(_connectionId, status);

                tcs.TrySetResult(null);
            },
            this);

            return tcs.Task;
        }
    }
}
