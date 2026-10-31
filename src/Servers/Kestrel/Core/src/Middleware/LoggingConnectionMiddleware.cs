// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal;

internal sealed class LoggingConnectionMiddleware
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
}
