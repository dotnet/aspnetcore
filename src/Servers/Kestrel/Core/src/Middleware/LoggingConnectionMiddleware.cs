// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    internal class LoggingConnectionMiddleware
    {
        private readonly ConnectionDelegate _next;
        private readonly ILogger _logger;

        public LoggingConnectionMiddleware(ConnectionDelegate next, ILogger logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task OnConnectionAsync(ConnectionContext context)
        {
            var oldTransport = context.Transport;

            try
            {
                await using (var loggingDuplexPipe = new LoggingDuplexPipe(context.Transport, _logger))
                {
                    context.Transport = loggingDuplexPipe;

                    await _next(context);
                }
            }
            finally
            {
                context.Transport = oldTransport;
            }
        }

        private class LoggingDuplexPipe : DuplexPipeStreamAdapter<LoggingStream>
        {
            public LoggingDuplexPipe(IDuplexPipe transport, ILogger logger) :
                base(transport, stream => new LoggingStream(stream, logger))
            {
            }
        }
    }
}
