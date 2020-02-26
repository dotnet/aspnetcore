// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
