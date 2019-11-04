// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        public async Task ThrowsNotSupportedExceptionWhenBindingToFileHandleEndPoint()
        {
            var socketTransportFactory = new SocketTransportFactory(Options.Create(new SocketTransportOptions()), Mock.Of<ILoggerFactory>());
            await Assert.ThrowsAsync<NotSupportedException>(async () => await socketTransportFactory.BindAsync(new FileHandleEndPoint(0, FileHandleType.Auto)));
        }
    }
}

