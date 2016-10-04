using System;
using System.Text;
using System.Threading.Tasks;
using Channels;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Sockets
{
    public class HttpConnectionDispatcher
    {
        private readonly ConnectionManager _manager;
        private readonly ChannelFactory _channelFactory;

        public HttpConnectionDispatcher(ConnectionManager manager, ChannelFactory factory)
        {
            _manager = manager;
            _channelFactory = factory;
        }

        public async Task Execute<TEndPoint>(string path, HttpContext context) where TEndPoint : EndPoint
        {
            if (context.Request.Path.StartsWithSegments(path + "/getid"))
            {
                await ProcessGetId(context);
            }
            else if (context.Request.Path.StartsWithSegments(path + "/send"))
            {
                await ProcessSend(context);
            }
            else
            {
                // Get the end point mapped to this http connection
                var endpoint = (EndPoint)context.RequestServices.GetRequiredService<TEndPoint>();
                var format =
                    string.Equals(context.Request.Query["format"], "binary", StringComparison.OrdinalIgnoreCase)
                        ? Format.Binary
                        : Format.Text;

                // Server sent events transport
                if (context.Request.Path.StartsWithSegments(path + "/sse"))
                {
                    // Get the connection state for the current http context
                    var state = GetOrCreateConnection(context);
                    state.Connection.User = context.User;
                    state.Connection.Metadata["transport"] = "sse";
                    state.Connection.Metadata.Format = format;

                    var sse = new ServerSentEvents(state.Connection);

                    await DoPersistentConnection(endpoint, sse, context, state.Connection);

                    _manager.RemoveConnection(state.Connection.ConnectionId);
                }
                else if (context.Request.Path.StartsWithSegments(path + "/ws"))
                {
                    // Get the connection state for the current http context
                    var state = GetOrCreateConnection(context);
                    state.Connection.User = context.User;
                    state.Connection.Metadata["transport"] = "websockets";
                    state.Connection.Metadata.Format = format;

                    // TODO: this is wrong. + how does the user add their own metadata based on HttpContext
                    var formatType = (string)context.Request.Query["formatType"];
                    state.Connection.Metadata["formatType"] = string.IsNullOrEmpty(formatType) ? "json" : formatType;

                    var ws = new WebSockets(state.Connection);

                    await DoPersistentConnection(endpoint, ws, context, state.Connection);

                    _manager.RemoveConnection(state.Connection.ConnectionId);
                }
                else if (context.Request.Path.StartsWithSegments(path + "/poll"))
                {
                    bool isNewConnection;
                    var state = GetOrCreateConnection(context, out isNewConnection);

                    // Mark the connection as active
                    state.Active = true;

                    Task endpointTask = null;

                    // Raise OnConnected for new connections only since polls happen all the time
                    if (isNewConnection)
                    {
                        state.Connection.Metadata["transport"] = "poll";
                        state.Connection.Metadata.Format = format;
                        state.Connection.User = context.User;

                        // REVIEW: This is super gross, this all needs to be cleaned up...
                        state.Close = async () =>
                        {
                            state.Connection.Channel.Dispose();

                            await endpointTask;

                            endpoint.Connections.Remove(state.Connection);
                        };

                        endpoint.Connections.Add(state.Connection);

                        endpointTask = endpoint.OnConnected(state.Connection);
                        state.Connection.Metadata["endpoint"] = endpointTask;
                    }
                    else
                    {
                        // Get the endpoint task from connection state
                        endpointTask = (Task)state.Connection.Metadata["endpoint"];
                    }

                    RegisterLongPollingDisconnect(context, state.Connection);

                    var longPolling = new LongPolling(state.Connection);

                    // Start the transport
                    var transportTask = longPolling.ProcessRequest(context);

                    var resultTask = await Task.WhenAny(endpointTask, transportTask);

                    if (resultTask == endpointTask)
                    {
                        // Notify the long polling transport to end
                        state.Connection.Channel.Dispose();

                        await transportTask;

                        endpoint.Connections.Remove(state.Connection);
                    }

                    // Mark the connection as inactive
                    state.LastSeen = DateTimeOffset.UtcNow;
                    state.Active = false;
                }
            }
        }

        private static async Task DoPersistentConnection(EndPoint endpoint,
                                                         IHttpTransport transport,
                                                         HttpContext context,
                                                         Connection connection)
        {
            // Register this transport for disconnect
            RegisterDisconnect(context, connection);

            endpoint.Connections.Add(connection);

            // Call into the end point passing the connection
            var endpointTask = endpoint.OnConnected(connection);

            // Start the transport
            var transportTask = transport.ProcessRequest(context);

            // Wait for any of them to end
            await Task.WhenAny(endpointTask, transportTask);

            // Kill the channel
            connection.Channel.Dispose();

            // Wait for both
            await Task.WhenAll(endpointTask, transportTask);

            endpoint.Connections.Remove(connection);
        }

        private static void RegisterLongPollingDisconnect(HttpContext context, Connection connection)
        {
            // For long polling, we need to end the transport but not the overall connection so we write 0 bytes
            context.RequestAborted.Register(state => ((HttpChannel)state).Output.WriteAsync(Span<byte>.Empty), connection.Channel);
        }

        private static void RegisterDisconnect(HttpContext context, Connection connection)
        {
            // We just kill the output writing as a signal to the transport that it is done
            context.RequestAborted.Register(state => ((HttpChannel)state).Output.CompleteWriter(), connection.Channel);
        }

        private Task ProcessGetId(HttpContext context)
        {
            // Reserve an id for this connection
            var state = _manager.ReserveConnection();

            // Get the bytes for the connection id
            var connectionIdBuffer = Encoding.UTF8.GetBytes(state.Connection.ConnectionId);

            // Write it out to the response with the right content length
            context.Response.ContentLength = connectionIdBuffer.Length;
            return context.Response.Body.WriteAsync(connectionIdBuffer, 0, connectionIdBuffer.Length);
        }

        private Task ProcessSend(HttpContext context)
        {
            var connectionId = context.Request.Query["id"];
            if (StringValues.IsNullOrEmpty(connectionId))
            {
                throw new InvalidOperationException("Missing connection id");
            }

            ConnectionState state;
            if (_manager.TryGetConnection(connectionId, out state))
            {
                // If we received an HTTP POST for the connection id and it's not an HttpChannel then fail.
                // You can't write to a TCP channel directly from here.
                var httpChannel = state.Connection.Channel as HttpChannel;

                if (httpChannel == null)
                {
                    throw new InvalidOperationException("No channel");
                }

                return context.Request.Body.CopyToAsync(httpChannel.Input);
            }

            throw new InvalidOperationException("Unknown connection id");
        }

        private ConnectionState GetOrCreateConnection(HttpContext context)
        {
            bool isNewConnection;
            return GetOrCreateConnection(context, out isNewConnection);
        }

        private ConnectionState GetOrCreateConnection(HttpContext context, out bool isNewConnection)
        {
            var connectionId = context.Request.Query["id"];
            ConnectionState connectionState;
            isNewConnection = false;

            // There's no connection id so this is a branch new connection
            if (StringValues.IsNullOrEmpty(connectionId))
            {
                isNewConnection = true;
                var channel = new HttpChannel(_channelFactory);
                connectionState = _manager.AddNewConnection(channel);
            }
            else
            {
                // REVIEW: Fail if not reserved? Reused an existing connection id?

                // There's a connection id
                if (!_manager.TryGetConnection(connectionId, out connectionState))
                {
                    throw new InvalidOperationException("Unknown connection id");
                }

                // Reserved connection, we need to provide a channel
                if (connectionState.Connection.Channel == null)
                {
                    isNewConnection = true;
                    connectionState.Connection.Channel = new HttpChannel(_channelFactory);
                    connectionState.Active = true;
                    connectionState.LastSeen = DateTimeOffset.UtcNow;
                }
            }

            return connectionState;
        }
    }
}
