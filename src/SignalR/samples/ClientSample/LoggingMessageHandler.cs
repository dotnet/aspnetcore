// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ClientSample;

internal class LoggingMessageHandler : DelegatingHandler
{
    private readonly ILogger<LoggingMessageHandler> _logger;

    public LoggingMessageHandler(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<LoggingMessageHandler>();
    }

    public LoggingMessageHandler(ILoggerFactory loggerFactory, HttpMessageHandler innerHandler) : base(innerHandler)
    {
        _logger = loggerFactory.CreateLogger<LoggingMessageHandler>();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Send: {0} {1}", request.Method, request.RequestUri);
        var result = await base.SendAsync(request, cancellationToken);
        _logger.LogDebug("Recv: {0} {1}", (int)result.StatusCode, request.RequestUri);
        return result;
    }
}
