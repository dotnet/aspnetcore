// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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

