// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.Connections.Client.Internal
{
    public class LoggingHttpMessageHandler : DelegatingHandler
    {
        private readonly ILogger<LoggingHttpMessageHandler> _logger;

        public LoggingHttpMessageHandler(HttpMessageHandler inner, ILoggerFactory loggerFactory) : base(inner)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _logger = loggerFactory.CreateLogger<LoggingHttpMessageHandler>();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Log.SendingHttpRequest(_logger, request.RequestUri);

            var response = await base.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                Log.UnsuccessfulHttpResponse(_logger, request.RequestUri, response.StatusCode);
            }

            return response;
        }

        private static class Log
        {
            private static readonly Action<ILogger, Uri, Exception> _sendingHttpRequest =
                LoggerMessage.Define<Uri>(LogLevel.Trace, new EventId(1, "SendingHttpRequest"), "Sending HTTP request to '{RequestUrl}'.");

            private static readonly Action<ILogger, Uri, HttpStatusCode, Exception> _unsuccessfulHttpResponse =
                LoggerMessage.Define<Uri, HttpStatusCode>(LogLevel.Warning, new EventId(2, "UnsuccessfulHttpResponse"), "Unsuccessful HTTP response status code of {StatusCode} return from '{RequestUrl}'.");

            public static void SendingHttpRequest(ILogger logger, Uri requestUrl)
            {
                _sendingHttpRequest(logger, requestUrl, null);
            }
            public static void UnsuccessfulHttpResponse(ILogger logger, Uri requestUrl, HttpStatusCode statusCode)
            {
                _unsuccessfulHttpResponse(logger, requestUrl, statusCode, null);
            }
        }
    }
}
