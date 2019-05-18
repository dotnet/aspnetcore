using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    public partial class HubConnectionTests
    {
        private static HubConnection CreateHubConnection(TestConnection connection, IHubProtocol protocol = null, ILoggerFactory loggerFactory = null, IRetryPolicy reconnectPolicy = null)
        {
            var builder = new HubConnectionBuilder();

            var delegateConnectionFactory = new DelegateConnectionFactory(
                connection.StartAsync,
                c => ((TestConnection)c).DisposeAsync());

            builder.Services.AddSingleton<IConnectionFactory>(delegateConnectionFactory);

            if (loggerFactory != null)
            {
                builder.WithLoggerFactory(loggerFactory);
            }

            if (protocol != null)
            {
                builder.Services.AddSingleton(protocol);
            }

            if (reconnectPolicy != null)
            {
                builder.WithAutomaticReconnect(reconnectPolicy);
            }

            return builder.Build();
        }
    }
}
