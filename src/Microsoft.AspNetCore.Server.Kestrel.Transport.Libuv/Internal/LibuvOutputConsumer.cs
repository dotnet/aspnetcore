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

        private readonly WriteReqPool _writeReqPool;
        private readonly IPipeReader _pipe;

        public LibuvOutputConsumer(
            IPipeReader pipe,
            LibuvThread thread,
            UvStreamHandle socket,
            string connectionId,
            ILibuvTrace log)
        {
            _pipe = pipe;
            // We need to have empty pipe at this moment so callback
            // get's scheduled
            _thread = thread;
            _socket = socket;
            _connectionId = connectionId;
            _log = log;
            _writeReqPool = thread.WriteReqPool;
        }

        public async Task WriteOutputAsync()
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

                        LogWriteInfo(writeResult.Status, writeResult.Error);

                        if (writeResult.Error != null)
                        {
                            throw writeResult.Error;
                        }
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
