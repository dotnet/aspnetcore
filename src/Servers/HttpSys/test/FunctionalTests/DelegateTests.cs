using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys.FunctionalTests
{
    public class DelegateTests
    {
        [Fact]
        public async Task DelegateRequestTest()
        {
            var queueName = Guid.NewGuid().ToString();
            var expectedResponseString = "Hello from delegatee";
            using var receiver = Utilities.CreateHttpServer(out var receiverAddress, async httpContext =>
            {
                await httpContext.Response.WriteAsync(expectedResponseString);
            },
            options =>
            {
                options.RequestQueueName = queueName;
            });

            var requestQueue = new RequestQueue(null, queueName, RequestQueueMode.Receiver, NullLogger.Instance);
            requestQueue.UrlGroup = new UrlGroup(requestQueue, UrlPrefix.Create(receiverAddress));

            using var delegator = Utilities.CreateHttpServer(out var delegatorAddress, httpContext =>
            {
                var request = httpContext.Request;
                var transferFeature = httpContext.Features.Get<IHttpSysRequestTransferFeature>();
                transferFeature.TransferRequest(requestQueue);
                return Task.FromResult(0);
            },
            options =>
            {
                options.RequestQueueMode = RequestQueueMode.Delegator;
            });


            var responseString = await SendRequestAsync(delegatorAddress);
            Assert.Equal(expectedResponseString, responseString);
        }

        private async Task<string> SendRequestAsync(string uri)
        {
            using var client = new HttpClient();
            return await client.GetStringAsync(uri);
        }
    }
}
