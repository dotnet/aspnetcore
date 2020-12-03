// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Testing;
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

            DelegationRule destination = default;

            using var delegator = Utilities.CreateHttpServer(out var delegatorAddress, httpContext =>
            {
                var delegateFeature = httpContext.Features.Get<IHttpSysRequestDelegationFeature>();
                delegateFeature.DelegateRequest(destination);
                return Task.CompletedTask;
            });

            var delegationProperty = delegator.Features.Get<IServerDelegationFeature>();
            destination = delegationProperty.CreateDelegationRule(queueName, receiverAddress);

            var responseString = await SendRequestAsync(delegatorAddress);
            Assert.Equal(_expectedResponseString, responseString);
            destination?.Dispose();
        }

        [ConditionalFact]
        [DelegateSupportedCondition(true)]
        public async Task DelegateAfterWriteToResponseBodyShouldThrowTest()
        {
            var queueName = Guid.NewGuid().ToString();
            using var receiver = Utilities.CreateHttpServer(out var receiverAddress, httpContext =>
            {
                httpContext.Response.StatusCode = StatusCodes.Status418ImATeapot;
                return Task.CompletedTask;
            },
            options =>
            {
                options.RequestQueueName = queueName;
            });

            DelegationRule destination = default;

            using var delegator = Utilities.CreateHttpServer(out var delegatorAddress, async httpContext =>
            {
                await httpContext.Response.WriteAsync(_expectedResponseString);
                var delegateFeature = httpContext.Features.Get<IHttpSysRequestDelegationFeature>();
                Assert.False(delegateFeature.CanDelegate);
                Assert.Throws<InvalidOperationException>(() => delegateFeature.DelegateRequest(destination));
            });

            var delegationProperty = delegator.Features.Get<IServerDelegationFeature>();
            destination = delegationProperty.CreateDelegationRule(queueName, receiverAddress);

            var responseString = await SendRequestAsync(delegatorAddress);
            Assert.Equal(_expectedResponseString, responseString);
            destination?.Dispose();
        }

        [ConditionalFact]
        [DelegateSupportedCondition(true)]
        public async Task WriteToBodyAfterDelegateShouldNoOp()
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

            DelegationRule destination = default;

            using var delegator = Utilities.CreateHttpServer(out var delegatorAddress, httpContext =>
            {
                var delegateFeature = httpContext.Features.Get<IHttpSysRequestDelegationFeature>();
                delegateFeature.DelegateRequest(destination);
                Assert.False(delegateFeature.CanDelegate);
                httpContext.Response.WriteAsync(_expectedResponseString);
                return Task.CompletedTask;
            });

            var delegationProperty = delegator.Features.Get<IServerDelegationFeature>();
            destination = delegationProperty.CreateDelegationRule(queueName, receiverAddress);

            var responseString = await SendRequestAsync(delegatorAddress);
            Assert.Equal(_expectedResponseString, responseString);
            destination?.Dispose();
        }

        [ConditionalFact]
        [DelegateSupportedCondition(true)]
        public async Task DelegateAfterRequestBodyReadShouldThrow()
        {
            var queueName = Guid.NewGuid().ToString();
            using var receiver = Utilities.CreateHttpServer(out var receiverAddress, httpContext =>
            {
                httpContext.Response.StatusCode = StatusCodes.Status418ImATeapot;
                return Task.CompletedTask;
            },
           options =>
           {
               options.RequestQueueName = queueName;
           });

            DelegationRule destination = default;

            using var delegator = Utilities.CreateHttpServer(out var delegatorAddress, async httpContext =>
            {
                var memoryStream = new MemoryStream();
                await httpContext.Request.Body.CopyToAsync(memoryStream);
                var delegateFeature = httpContext.Features.Get<IHttpSysRequestDelegationFeature>();
                Assert.Throws<InvalidOperationException>(() => delegateFeature.DelegateRequest(destination));
            });

            var delegationProperty = delegator.Features.Get<IServerDelegationFeature>();
            destination = delegationProperty.CreateDelegationRule(queueName, receiverAddress);

            _ = await SendRequestWithBodyAsync(delegatorAddress);
            destination?.Dispose();
        }

        [ConditionalFact]
        [DelegateSupportedCondition(true)]
        public async Task DuplicateDelegationRuleTest()
        {
            var queueName = Guid.NewGuid().ToString();
            using var receiver = Utilities.CreateHttpServer(out _, async httpContext =>
            {
                await httpContext.Response.WriteAsync(_expectedResponseString);
            },
           options =>
           {
               options.RequestQueueName = queueName;
               options.UrlPrefixes.Add("http://localhost:0");
               options.UrlPrefixes.Add("http://localhost:0");
           });

            var receiverAddresses = receiver.Features.Get<IServerAddressesFeature>().Addresses.ToList();

            DelegationRule destination0 = default;
            DelegationRule destination1 = default;

            using var delegator = Utilities.CreateHttpServer(out var delegatorAddress, httpContext =>
            {
                var delegateFeature = httpContext.Features.Get<IHttpSysRequestDelegationFeature>();
                // Let's pick the rule we didn't set the delegation property on
                delegateFeature.DelegateRequest(destination1);
                return Task.CompletedTask;
            });

            var delegationProperty = delegator.Features.Get<IServerDelegationFeature>();
            destination0 = delegationProperty.CreateDelegationRule(queueName, receiverAddresses[0]);
            destination1 = delegationProperty.CreateDelegationRule(queueName, receiverAddresses[1]);

            var responseString = await SendRequestAsync(delegatorAddress);
            Assert.Equal(_expectedResponseString, responseString);
            destination0?.Dispose();
            destination1?.Dispose();
        }

        [ConditionalFact]
        [DelegateSupportedCondition(false)]
        public async Task DelegationFeaturesAreNull()
        {
            using var delegator = Utilities.CreateHttpServer(out var delegatorAddress, httpContext =>
            {
                var delegateFeature = httpContext.Features.Get<IHttpSysRequestDelegationFeature>();
                Assert.Null(delegateFeature);
                return Task.CompletedTask;
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
            var content = new StringContent("Sample request body");
            var response = await client.PostAsync(uri, content);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}
