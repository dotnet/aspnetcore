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

            using var delegator = Utilities.CreateHttpServer(out var delegatorAddress, httpContext =>
            {
                var transferFeature = httpContext.Features.Get<IHttpSysRequestTransferFeature>();
                transferFeature.TransferRequest(queueName, receiverAddress);
                return Task.FromResult(0);
            });


            var responseString = await SendRequestAsync(delegatorAddress);
            Assert.Equal(expectedResponseString, responseString);
        }

        [Fact]
        public async Task DelegateAfterWriteToBodyShouldThrowTest()
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

            using var delegator = Utilities.CreateHttpServer(out var delegatorAddress, async httpContext =>
            {
                await httpContext.Response.WriteAsync(expectedResponseString);
                var transferFeature = httpContext.Features.Get<IHttpSysRequestTransferFeature>();
                Assert.Throws<InvalidOperationException>(() => transferFeature.TransferRequest(queueName, receiverAddress));
            });


            var responseString = await SendRequestAsync(delegatorAddress);
            Assert.Equal(expectedResponseString, responseString);
        }

        [Fact]
        public async Task WriteToBodyAfterDelegateShouldThrowTest()
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

            using var delegator = Utilities.CreateHttpServer(out var delegatorAddress, async httpContext =>
            {
                var transferFeature = httpContext.Features.Get<IHttpSysRequestTransferFeature>();
                transferFeature.TransferRequest(queueName, receiverAddress);
                await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                {
                    await httpContext.Response.WriteAsync(expectedResponseString);
                });
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
