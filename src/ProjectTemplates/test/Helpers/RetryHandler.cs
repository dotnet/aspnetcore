// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Templates.Test.Helpers
{
    internal class RetryHandler : DelegatingHandler
    {
        private readonly ITestOutputHelper _output;
        private readonly int _maxRetries;
        private TimeSpan _waitIntervalBeforeRetry;

        public RetryHandler(
            HttpClientHandler httpClientHandler,
            ITestOutputHelper output,
            TimeSpan initialWaitTime,
            int maxAttempts) : base(httpClientHandler)
        {
            _waitIntervalBeforeRetry = initialWaitTime;
            _output = output;
            _maxRetries = maxAttempts;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri;
            var method = request.Method;

            HttpResponseMessage result = null;
            for (var i = 0; i < _maxRetries; i++)
            {
                try
                {
                    _output.WriteLine($"Sending request '{method} - {url}' {i + 1} attempt.");
                    result = await base.SendAsync(request, cancellationToken);
                    _output.WriteLine($"Request '{method} - {url}' ended with {result.StatusCode}.");
                    return result;
                }
                catch (Exception e)
                {
                    _output.WriteLine($"Request '{method} - {url}' failed with {e.ToString()}");
                    result?.Dispose();
                }
                finally
                {
                    await Task.Delay(_waitIntervalBeforeRetry, cancellationToken);
                    _waitIntervalBeforeRetry = _waitIntervalBeforeRetry * 2;
                }
            }

            _output.WriteLine($"Sending request '{method} - {url}' {_maxRetries + 1} attempt.");
            result = await base.SendAsync(request, cancellationToken);
            _output.WriteLine($"Request '{method} - {url}' ended with {result.StatusCode}.");
            return result;
        }
    }
}
