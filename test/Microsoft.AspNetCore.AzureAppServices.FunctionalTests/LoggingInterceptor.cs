// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;

namespace Microsoft.AspNetCore.AzureAppServices.FunctionalTests
{
    public class LoggingInterceptor : IServiceClientTracingInterceptor
    {
        private readonly ILogger _logger;

        public LoggingInterceptor(ILogger logger)
        {
            _logger = logger;
        }

        public void Information(string message)
        {
            _logger.LogInformation(message);
        }

        public void TraceError(string invocationId, Exception exception)
        {
            _logger.LogInformation(exception, "Exception in {invocationId}", invocationId);
        }

        public void ReceiveResponse(string invocationId, HttpResponseMessage response)
        {
            _logger.LogInformation(response.AsFormattedString());
        }

        public void SendRequest(string invocationId, HttpRequestMessage request)
        {
            _logger.LogInformation(request.AsFormattedString());
        }

        public void Configuration(string source, string name, string value) { }

        public void EnterMethod(string invocationId, object instance, string method, IDictionary<string, object> parameters) { }

        public void ExitMethod(string invocationId, object returnValue) { }
    }
}