// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Sockets.BindTests
{
    public class SocketTransportFactoryTests
    {
        [Fact]
        public async Task ThrowsNotImplementedExceptionWhenBindingToUriEndPoint()
        {
            var options = Options.Create(new SocketTransportOptions());
            var logger = Mock.Of<ILoggerFactory>();
            var connectionFactory = new SocketConnectionContextFactory(options, logger);
            var socketTransportFactory = new SocketTransportFactory(options, logger, connectionFactory);
            await Assert.ThrowsAsync<NotImplementedException>(async () => await socketTransportFactory.BindAsync(new UriEndPoint(new Uri("http://127.0.0.1:5554"))));
        }
    }
}

