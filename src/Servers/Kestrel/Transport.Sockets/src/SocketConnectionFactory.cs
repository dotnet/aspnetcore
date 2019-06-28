using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets
{
    /// <summary>
    /// Defines a class for creating socket connections based on the specified endpoint.
    /// </summary>
    public class SocketConnectionFactory : IConnectionFactory
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Creates the <see cref="SocketConnectionFactory"/>.
        /// </summary>
        /// <param name="loggerFactory">The logger factory</param>
        public SocketConnectionFactory(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SocketConnectionFactory>();
        }

        /// <summary>
        /// Creates a new socket connection to the specified endpoint.
        /// </summary>
        /// <param name="endPoint">The <see cref="EndPoint"/> to connect to.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        /// <returns>
        /// A <see cref="ValueTask{TResult}" /> that represents the asynchronous connect, yielding the <see cref="ConnectionContext" /> for the new connection when completed.
        /// </returns>
        public async ValueTask<ConnectionContext> ConnectAsync(EndPoint endPoint, CancellationToken cancellationToken = default)
        {
            var protocolType = endPoint is UnixDomainSocketEndPoint ? ProtocolType.Unspecified : ProtocolType.Tcp;

            var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, protocolType);

            await socket.ConnectAsync(endPoint);

            var connection = new SocketConnection(socket, memoryPool: null, PipeScheduler.ThreadPool, new SocketsTrace(_logger));
            connection.Start();

            return connection;
        }
    }
}
