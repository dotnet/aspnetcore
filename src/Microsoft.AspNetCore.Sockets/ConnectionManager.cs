// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks.Channels;
using Microsoft.AspNetCore.Sockets.Internal;

namespace Microsoft.AspNetCore.Sockets
{
    public class ConnectionManager
    {
        private readonly ConcurrentDictionary<string, ConnectionState> _connections = new ConcurrentDictionary<string, ConnectionState>();
        private readonly Timer _timer;
        private readonly PipelineFactory _pipelineFactory;

        public ConnectionManager(PipelineFactory pipelineFactory)
        {
            _pipelineFactory = pipelineFactory;
            _timer = new Timer(Scan, this, 0, 1000);
        }

        public bool TryGetConnection(string id, out ConnectionState state)
        {
            return _connections.TryGetValue(id, out state);
        }

        public ConnectionState CreateConnection(ConnectionMode mode) =>
            mode == ConnectionMode.Streaming ? CreateStreamingConnection() : CreateMessagingConnection();

        public void RemoveConnection(string id)
        {
            ConnectionState state;
            _connections.TryRemove(id, out state);

            // Remove the connection completely
        }

        private static string MakeNewConnectionId()
        {
            // TODO: We need to sign and encyrpt this
            return Guid.NewGuid().ToString();
        }

        private static void Scan(object state)
        {
            ((ConnectionManager)state).Scan();
        }

        private void Scan()
        {
            // Scan the registered connections looking for ones that have timed out
            foreach (var c in _connections)
            {
                if (!c.Value.Active && (DateTimeOffset.UtcNow - c.Value.LastSeenUtc).TotalSeconds > 5)
                {
                    ConnectionState s;
                    if (_connections.TryRemove(c.Key, out s))
                    {
                        s?.Close();
                    }
                    else
                    {

                    }
                }
            }
        }

        public void CloseConnections()
        {
            // Stop firing the timer
            _timer.Dispose();

            foreach (var c in _connections)
            {
                ConnectionState s;
                if (_connections.TryRemove(c.Key, out s))
                {
                    // Longpolling connections should do this
                    if (s.Close != null)
                    {
                        s.Close();
                    }
                    else
                    {
                        s.Dispose();
                    }
                }
            }
        }

        private ConnectionState CreateMessagingConnection()
        {
            var id = MakeNewConnectionId();

            var transportToApplication = Channel.Create<Message>();
            var applicationToTransport = Channel.Create<Message>();

            var transportSide = new ChannelConnection<Message>(applicationToTransport, transportToApplication);
            var applicationSide = new ChannelConnection<Message>(transportToApplication, applicationToTransport);

            var state = new MessagingConnectionState(
                new MessagingConnection(id, applicationSide),
                transportSide);

            _connections.TryAdd(id, state);
            return state;
        }

        private ConnectionState CreateStreamingConnection()
        {
            var id = MakeNewConnectionId();

            var transportToApplication = _pipelineFactory.Create();
            var applicationToTransport = _pipelineFactory.Create();

            var transportSide = new PipelineConnection(applicationToTransport, transportToApplication);
            var applicationSide = new PipelineConnection(transportToApplication, applicationToTransport);

            var state = new StreamingConnectionState(
                new StreamingConnection(id, applicationSide),
                transportSide);

            _connections.TryAdd(id, state);
            return state;
        }
    }
}
