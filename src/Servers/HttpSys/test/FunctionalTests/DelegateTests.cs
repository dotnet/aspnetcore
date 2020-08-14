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
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys.FunctionalTests
{
    public class DelegateTests
    {
        private static readonly string _expectedResponseString = "Hello from delegatee";

        [ConditionalFact]
        [DelegateSupportedCondition(true)]
        public async Task DelegateRequestTest()
        {
            var queueName = Guid.NewGuid().ToString();
             using var receiver = Utilities.CreateHttpServer(out var receiverAddress, async httpContext =>
            {
                await httpContext.Response.WriteAsync(_expectedResponseString);
            },
            options =>
            {
                options.RequestQueueName = queueName;
            });

            DelegationRule wrapper = default;

            using var delegator = Utilities.CreateHttpServer(out var delegatorAddress, httpContext =>
            {
                var transferFeature = httpContext.Features.Get<IHttpSysRequestTransferFeature>();
                transferFeature.TransferRequest(wrapper);
                return Task.FromResult(0);
            });

            var delegationProperty = delegator.Features.Get<IServerDelegationFeature>();
            wrapper = delegationProperty.CreateDelegationRule(queueName, receiverAddress);

            var responseString = await SendRequestAsync(delegatorAddress);
            Assert.Equal(_expectedResponseString, responseString);
        }

        [ConditionalFact]
        [DelegateSupportedCondition(true)]
        public async Task DelegateAfterWriteToResponseBodyShouldThrowTest()
        {
            var queueName = Guid.NewGuid().ToString();
            using var receiver = Utilities.CreateHttpServer(out var receiverAddress, async httpContext =>
            {
                await httpContext.Response.WriteAsync(_expectedResponseString);
            },
            options =>
            {
                options.RequestQueueName = queueName;
            });

            DelegationRule wrapper = default;

            using var delegator = Utilities.CreateHttpServer(out var delegatorAddress, async httpContext =>
            {
                await httpContext.Response.WriteAsync(_expectedResponseString);
                var transferFeature = httpContext.Features.Get<IHttpSysRequestTransferFeature>();
                Assert.Throws<InvalidOperationException>(() => transferFeature.TransferRequest(wrapper));
            });

            var delegationProperty = delegator.Features.Get<IServerDelegationFeature>();
            wrapper = delegationProperty.CreateDelegationRule(queueName, receiverAddress);

            var responseString = await SendRequestAsync(delegatorAddress);
            Assert.Equal(_expectedResponseString, responseString);
        }

        [ConditionalFact]
        [DelegateSupportedCondition(true)]
        public async Task WriteToBodyAfterDelegateShouldThrowTest()
        {
            var queueName = Guid.NewGuid().ToString();
            using var receiver = Utilities.CreateHttpServer(out var receiverAddress, async httpContext =>
            {
                await httpContext.Response.WriteAsync(_expectedResponseString);
            },
            options =>
            {
                options.RequestQueueName = queueName;
            });

            DelegationRule wrapper = default;

            using var delegator = Utilities.CreateHttpServer(out var delegatorAddress, async httpContext =>
            {
                var transferFeature = httpContext.Features.Get<IHttpSysRequestTransferFeature>();
                transferFeature.TransferRequest(wrapper);
                await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                {
                    await httpContext.Response.WriteAsync(_expectedResponseString);
                });
            });

            var delegationProperty = delegator.Features.Get<IServerDelegationFeature>();
            wrapper = delegationProperty.CreateDelegationRule(queueName, receiverAddress);

            var responseString = await SendRequestAsync(delegatorAddress);
            Assert.Equal(_expectedResponseString, responseString);
        }

        [ConditionalFact]
        [DelegateSupportedCondition(true)]
        public async Task DelegateAfterRequestBodyReadShouldThrow()
        {
            var queueName = Guid.NewGuid().ToString();
            using var receiver = Utilities.CreateHttpServer(out var receiverAddress, async httpContext =>
            {
                await httpContext.Response.WriteAsync(_expectedResponseString);
            },
           options =>
           {
               options.RequestQueueName = queueName;
           });

            DelegationRule wrapper = default;

            using var delegator = Utilities.CreateHttpServer(out var delegatorAddress, async httpContext =>
            {
                var memoryStream = new MemoryStream();
                await httpContext.Request.Body.CopyToAsync(memoryStream);
                var transferFeature = httpContext.Features.Get<IHttpSysRequestTransferFeature>();
                Assert.Throws<InvalidOperationException>(() => transferFeature.TransferRequest(wrapper));
            });

            var delegationProperty = delegator.Features.Get<IServerDelegationFeature>();
            wrapper = delegationProperty.CreateDelegationRule(queueName, receiverAddress);

            _ = await SendRequestWithBodyAsync(delegatorAddress);
        }

        [ConditionalFact]
        [DelegateSupportedCondition(false)]
        public async Task DelegationFeaturesAreNull()
        {
            var queueName = Guid.NewGuid().ToString();
            using var receiver = Utilities.CreateHttpServer(out var receiverAddress, async httpContext =>
            {
                await httpContext.Response.WriteAsync(_expectedResponseString);
            },
           options =>
           {
               options.RequestQueueName = queueName;
           });

            using var delegator = Utilities.CreateHttpServer(out var delegatorAddress, async httpContext =>
            {
                var transferFeature = httpContext.Features.Get<IHttpSysRequestTransferFeature>();
                Assert.Null(transferFeature);
                await httpContext.Response.WriteAsync(_expectedResponseString);
            });

            var delegationProperty = delegator.Features.Get<IServerDelegationFeature>();
            Assert.Null(delegationProperty);

            _ = await SendRequestAsync(delegatorAddress);
        }

        private async Task<string> SendRequestAsync(string uri)
        {
            using var client = new HttpClient();
            return await client.GetStringAsync(uri);
        }

        private async Task<string> SendRequestWithBodyAsync(string uri)
        {
            using var client = new HttpClient();
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, uri);
            requestMessage.Content = new StringContent("Sample request body");
            var response = await client.SendAsync(requestMessage);
            return await response.Content.ReadAsStringAsync();
        }
    }
}
