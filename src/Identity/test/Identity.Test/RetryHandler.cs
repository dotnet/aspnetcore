// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Identity.Test
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
                HttpResponseMessage result = null;
                var url = request.RequestUri;
                var method = request.Method;

                for (var i = 0; i < _maxRetries; i++)
                {
                    try
                    {
                        _output.WriteLine($"Sending request '{method} - {url}' {i+1} attempt.");
                        result = await base.SendAsync(request, cancellationToken);
                        if (result.IsSuccessStatusCode)
                        {
                            return result;
                        }
                        else
                        {
                            _output.WriteLine($"Request '{method} - {url}' failed with {result.StatusCode}.");
                        }
                    }
                    catch (Exception e)
                    {
                        _output.WriteLine($"Request '{method} - {url}' failed with {e.ToString()}");
                    }
                    finally
                    {
                        await Task.Delay(_waitIntervalBeforeRetry, cancellationToken);
                        _waitIntervalBeforeRetry = _waitIntervalBeforeRetry * 2;
                    }
                }
                
                // Try one last time to show the actual error.
                return await base.SendAsync(request, cancellationToken);
            }
        }
}
