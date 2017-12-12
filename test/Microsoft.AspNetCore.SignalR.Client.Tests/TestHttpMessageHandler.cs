using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Client.Tests;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    public class TestHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (ResponseUtils.IsNegotiateRequest(request))
            {
                return Task.FromResult(ResponseUtils.CreateResponse(HttpStatusCode.OK,
                    ResponseUtils.CreateNegotiationResponse()));
            }
            else
            {
                return Task.FromException<HttpResponseMessage>(new InvalidOperationException($"Http endpoint not implemented: {request.RequestUri}"));
            }
        }
    }
}
