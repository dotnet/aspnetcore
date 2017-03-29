using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Networking;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure
{
    public class WriteReqPool
    {
        private const int _maxPooledWriteReqs = 1024;

        private readonly KestrelThread _thread;
        private readonly Queue<UvWriteReq> _pool = new Queue<UvWriteReq>(_maxPooledWriteReqs);
        private readonly IKestrelTrace _log;
        private bool _disposed;

        public WriteReqPool(KestrelThread thread, IKestrelTrace log)
        {
            _thread = thread;
            _log = log;
        }

        public UvWriteReq Allocate()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            UvWriteReq req;
            if (_pool.Count > 0)
            {
                req = _pool.Dequeue();
            }
            else
            {
                req = new UvWriteReq(_log);
                req.Init(_thread.Loop);
            }

            return req;
        }

        public void Return(UvWriteReq req)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            if (_pool.Count < _maxPooledWriteReqs)
            {
                _pool.Enqueue(req);
            }
            else
            {
                req.Dispose();
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                while (_pool.Count > 0)
                {
                    _pool.Dequeue().Dispose();
                }
            }
        }
    }
}
