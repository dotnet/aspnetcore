// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR.Features;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Features;

namespace Microsoft.AspNetCore.SignalR
{
    public class HubConnectionContext
    {
        private static Action<object> _abortedCallback = AbortConnection;

        private readonly ChannelWriter<HubMessage> _output;
        private readonly ConnectionContext _connectionContext;
        private readonly CancellationTokenSource _connectionAbortedTokenSource = new CancellationTokenSource();
        private readonly TaskCompletionSource<object> _abortCompletedTcs = new TaskCompletionSource<object>();

        public HubConnectionContext(ChannelWriter<HubMessage> output, ConnectionContext connectionContext)
        {
            _output = output;
            _connectionContext = connectionContext;
            ConnectionAbortedToken = _connectionAbortedTokenSource.Token;
        }

        private IHubFeature HubFeature => Features.Get<IHubFeature>();

        // Used by the HubEndPoint only
        internal ChannelReader<byte[]> Input => _connectionContext.Transport;

        internal ExceptionDispatchInfo AbortException { get; private set; }

        public virtual CancellationToken ConnectionAbortedToken { get; }

        public virtual string ConnectionId => _connectionContext.ConnectionId;

        public virtual ClaimsPrincipal User => Features.Get<IConnectionUserFeature>()?.User;

        public virtual IFeatureCollection Features => _connectionContext.Features;

        public virtual IDictionary<object, object> Metadata => _connectionContext.Metadata;

        public virtual HubProtocolReaderWriter ProtocolReaderWriter { get; set; }

        public virtual ChannelWriter<HubMessage> Output => _output;

        // Currently used only for streaming methods
        internal ConcurrentDictionary<string, CancellationTokenSource> ActiveRequestCancellationSources { get; } = new ConcurrentDictionary<string, CancellationTokenSource>();

        public virtual void Abort()
        {
            // If we already triggered the token then noop, this isn't thread safe but it's good enough
            // to avoid spawning a new task in the most common cases
            if (_connectionAbortedTokenSource.IsCancellationRequested)
            {
                return;
            }

            // We fire and forget since this can trigger user code to run
            Task.Factory.StartNew(_abortedCallback, this);
        }

        public string UserIdentifier { get; internal set; }

        internal void Abort(Exception exception)
        {
            AbortException = ExceptionDispatchInfo.Capture(exception);
            Abort();
        }

        // Used by the HubEndPoint only
        internal Task AbortAsync()
        {
            Abort();
            return _abortCompletedTcs.Task;
        }

        private static void AbortConnection(object state)
        {
            var connection = (HubConnectionContext)state;
            try
            {
                connection._connectionAbortedTokenSource.Cancel();

                // Communicate the fact that we're finished triggering abort callbacks
                connection._abortCompletedTcs.TrySetResult(null);
            }
            catch (Exception ex)
            {
                // TODO: Should we log if the cancellation callback fails? This is more preventative to make sure
                // we don't end up with an unobserved task
                connection._abortCompletedTcs.TrySetException(ex);
            }
        }
    }
}
