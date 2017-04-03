// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.SignalR
{
    public class Hub : Hub<IClientProxy>
    {
    }

    public class Hub<TClient> : IDisposable
    {
        private bool _disposed;
        private IHubConnectionContext<TClient> _clients;
        private HubCallerContext _context;
        private IGroupManager _groups;

        public IHubConnectionContext<TClient> Clients
        {
            get
            {
                CheckDisposed();
                return _clients;
            }
            set
            {
                CheckDisposed();
                _clients = value;
            }
        }

        public HubCallerContext Context
        {
            get
            {
                CheckDisposed();
                return _context;
            }
            set
            {
                CheckDisposed();
                _context = value;
            }
        }

        public IGroupManager Groups
        {
            get
            {
                CheckDisposed();
                return _groups;
            }
            set
            {
                CheckDisposed();
                _groups = value;
            }
        }

        public virtual Task OnConnectedAsync()
        {
            return TaskCache.CompletedTask;
        }

        public virtual Task OnDisconnectedAsync(Exception exception)
        {
            return TaskCache.CompletedTask;
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Dispose(true);

            _disposed = true;
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}
