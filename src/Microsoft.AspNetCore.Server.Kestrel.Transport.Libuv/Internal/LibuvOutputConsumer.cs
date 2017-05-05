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
                    if (!buffer.IsEmpty)
                    {
                        var writeReq = pool.Allocate();

                        try
                        {
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

                    if (result.IsCancelled)
                    {
                        break;
                    }

                    if (buffer.IsEmpty && result.IsCompleted)
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
