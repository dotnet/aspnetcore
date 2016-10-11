using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SocketsSample.Hubs;

namespace SocketsSample
{
    public class HubEndpoint : RpcEndpoint, IHubConnectionContext
    {
        private readonly ILogger<HubEndpoint> _logger;
        private readonly IServiceProvider _serviceProvider;

        public HubEndpoint(ILogger<HubEndpoint> logger, ILogger<RpcEndpoint> jsonRpcLogger, IServiceProvider serviceProvider)
            : base(jsonRpcLogger, serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            All = new AllClientProxy(this);
        }

        public IClientProxy All { get; }

        public IClientProxy Client(string connectionId)
        {
            return new SingleClientProxy(this, connectionId);
        }

        protected override void Initialize(object endpoint)
        {
            ((Hub)endpoint).Clients = this;
            base.Initialize(endpoint);
        }

        protected override void DiscoverEndpoints()
        {
            // Register the chat hub
            RegisterRPCEndPoint(typeof(Chat));
        }

        private class AllClientProxy : IClientProxy
        {
            private readonly HubEndpoint _endPoint;

            public AllClientProxy(HubEndpoint endPoint)
            {
                _endPoint = endPoint;
            }

            public Task Invoke(string method, params object[] args)
            {
                // REVIEW: Thread safety
                var tasks = new List<Task>(_endPoint.Connections.Count);
                var message = new InvocationDescriptor
                {
                    Method = method,
                    Arguments = args
                };

                foreach (var connection in _endPoint.Connections)
                {

                    var invocationAdapter = _endPoint._serviceProvider.GetRequiredService<SocketFormatters>()
                        .GetInvocationAdapter((string)connection.Metadata["formatType"]);

                    tasks.Add(invocationAdapter.InvokeClientMethod(connection.Channel.GetStream(), message));
                }

                return Task.WhenAll(tasks);
            }
        }

        private class SingleClientProxy : IClientProxy
        {
            private readonly string _connectionId;
            private readonly HubEndpoint _endPoint;

            public SingleClientProxy(HubEndpoint endPoint, string connectionId)
            {
                _endPoint = endPoint;
                _connectionId = connectionId;
            }

            public Task Invoke(string method, params object[] args)
            {
                var connection = _endPoint.Connections[_connectionId];

                var invocationAdapter = _endPoint._serviceProvider.GetRequiredService<SocketFormatters>()
                    .GetInvocationAdapter((string)connection.Metadata["formatType"]);

                if (_endPoint._logger.IsEnabled(LogLevel.Debug))
                {
                    _endPoint._logger.LogDebug("Outgoing RPC invocation method '{methodName}'", method);
                }

                var message = new InvocationDescriptor
                {
                    Method = method,
                    Arguments = args
                };

                return invocationAdapter.InvokeClientMethod(connection.Channel.GetStream(), message);
            }
        }
    }
}
