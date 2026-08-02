// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.Connections.Client.Internal;

internal sealed partial class LoggingHttpMessageHandler : DelegatingHandler
{
    private readonly ILogger _logger;

    public LoggingHttpMessageHandler(HttpMessageHandler inner, ILoggerFactory loggerFactory) : base(inner)
    {
        ArgumentNullThrowHelper.ThrowIfNull(loggerFactory);

        _logger = loggerFactory.CreateLogger(typeof(LoggingHttpMessageHandler));
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Log.SendingHttpRequest(_logger, request.Method, request.RequestUri!);

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.SwitchingProtocols)
        {
            Log.UnsuccessfulHttpResponse(_logger, response.StatusCode, request.Method, request.RequestUri!);
        }

        return response;
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Trace, "Sending HTTP request {RequestMethod} '{RequestUrl}'.", EventName = "SendingHttpRequest")]
        public static partial void SendingHttpRequest(ILogger logger, HttpMethod requestMethod, Uri requestUrl);

        [LoggerMessage(2, LogLevel.Warning, "Unsuccessful HTTP response {StatusCode} return from {RequestMethod} '{RequestUrl}'.", EventName = "UnsuccessfulHttpResponse")]
        public static partial void UnsuccessfulHttpResponse(ILogger logger, HttpStatusCode statusCode, HttpMethod requestMethod, Uri requestUrl);
    }
}
