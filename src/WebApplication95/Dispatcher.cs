using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Channels;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace WebApplication95
{
    public class Dispatcher
    {
        private readonly ConnectionManager _manager = new ConnectionManager();
        private readonly EndPoint _endpoint = new EndPoint();

        public async Task Execute(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/send"))
            {
                var connectionId = context.Request.Query["id"];

                if (StringValues.IsNullOrEmpty(connectionId))
                {
                    throw new InvalidOperationException("Missing connection id");
                }

                ConnectionState state;
                if (_manager.TryGetConnection(connectionId, out state))
                {
                    // Write the message length
                    await context.Request.Body.CopyToAsync(state.Connection.Input);
                }
            }
            else
            {
                var connectionId = _manager.GetConnectionId(context);

                // Outgoing channels
                if (context.Request.Path.StartsWithSegments("/sse"))
                {
                    ConnectionState state;
                    _manager.AddConnection(connectionId, out state);

                    var sse = new ServerSentEvents(state);

                    var ignore = _endpoint.OnConnected(state.Connection);

                    state.Connection.TransportType = TransportType.ServerSentEvents;

                    await sse.ProcessRequest(context);

                    state.Connection.Complete();

                    _manager.RemoveConnection(connectionId);
                }
                else if (context.Request.Path.StartsWithSegments("/ws"))
                {
                    ConnectionState state;
                    _manager.AddConnection(connectionId, out state);

                    var ws = new WebSockets(state);

                    var ignore = _endpoint.OnConnected(state.Connection);

                    state.Connection.TransportType = TransportType.WebSockets;

                    await ws.ProcessRequest(context);

                    state.Connection.Complete();

                    _manager.RemoveConnection(connectionId);
                }
                else if (context.Request.Path.StartsWithSegments("/poll"))
                {
                    ConnectionState state;
                    bool newConnection = false;
                    if (_manager.AddConnection(connectionId, out state))
                    {
                        newConnection = true;
                        var ignore = _endpoint.OnConnected(state.Connection);
                        state.Connection.TransportType = TransportType.LongPolling;
                    }

                    var longPolling = new LongPolling(state);

                    await longPolling.ProcessRequest(newConnection, context);

                    _manager.MarkConnectionDead(connectionId);
                }

            }
        }
    }

    public class EndPoint
    {
        private List<Connection> _connections = new List<Connection>();

        public virtual async Task OnConnected(Connection connection)
        {
            lock (_connections)
            {
                _connections.Add(connection);
            }

            // Echo server
            while (true)
            {
                var input = await connection.Input.ReadAsync();
                try
                {
                    if (input.IsEmpty && connection.Input.Reading.IsCompleted)
                    {
                        break;
                    }

                    List<Connection> connections = null;
                    lock (_connections)
                    {
                        connections = _connections;
                    }

                    foreach (var c in connections)
                    {
                        var output = c.Output.Alloc();
                        output.Append(ref input);
                        await output.FlushAsync();
                    }
                }
                finally
                {
                    connection.Input.Advance(input.End);
                }
            }

            lock (_connections)
            {
                _connections.Remove(connection);
            }
        }
    }
}
