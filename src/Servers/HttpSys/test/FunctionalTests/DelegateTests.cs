using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys.FunctionalTests
{
    public class DelegateTests
    {
        [ConditionalFact]
        [DelegateSupportedCondition]
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

            RequestQueueWrapper wrapper = default;

            using var delegator = Utilities.CreateHttpServer(out var delegatorAddress, httpContext =>
            {
                var transferFeature = httpContext.Features.Get<IHttpSysRequestTransferFeature>();
                transferFeature.TransferRequest(wrapper);
                return Task.FromResult(0);
            });

            var delegationProperty = delegator.Features.Get<IServerDelegationPropertyFeature>();
            wrapper = delegationProperty.SetDelegationProperty(queueName, receiverAddress);

            var responseString = await SendRequestAsync(delegatorAddress);
            Assert.Equal(expectedResponseString, responseString);
        }

        [ConditionalFact]
        [DelegateSupportedCondition]
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

            RequestQueueWrapper wrapper = default;

            using var delegator = Utilities.CreateHttpServer(out var delegatorAddress, async httpContext =>
            {
                await httpContext.Response.WriteAsync(expectedResponseString);
                var transferFeature = httpContext.Features.Get<IHttpSysRequestTransferFeature>();
                Assert.Throws<InvalidOperationException>(() => transferFeature.TransferRequest(wrapper));
            });

            var delegationProperty = delegator.Features.Get<IServerDelegationPropertyFeature>();
            wrapper = delegationProperty.SetDelegationProperty(queueName, receiverAddress);

            var responseString = await SendRequestAsync(delegatorAddress);
            Assert.Equal(expectedResponseString, responseString);
        }

        [ConditionalFact]
        [DelegateSupportedCondition]
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

            RequestQueueWrapper wrapper = default;

            using var delegator = Utilities.CreateHttpServer(out var delegatorAddress, async httpContext =>
            {
                var transferFeature = httpContext.Features.Get<IHttpSysRequestTransferFeature>();
                transferFeature.TransferRequest(wrapper);
                await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                {
                    await httpContext.Response.WriteAsync(expectedResponseString);
                });
            });

            var delegationProperty = delegator.Features.Get<IServerDelegationPropertyFeature>();
            wrapper = delegationProperty.SetDelegationProperty(queueName, receiverAddress);

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
