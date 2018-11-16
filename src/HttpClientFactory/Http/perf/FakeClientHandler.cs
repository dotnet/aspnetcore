// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Http.Performance
{
    internal class FakeClientHandler : HttpMessageHandler
    {
        public TimeSpan Latency { get; set; } = TimeSpan.FromMilliseconds(10);

        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Yield();

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.RequestMessage = request;
            return response;
        }
    }
}
