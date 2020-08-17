// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
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
                var transferFeature = httpContext.Features.Get<IHttpSysRequestTransferFeature>();
                transferFeature.TransferRequest(destination);
                return Task.FromResult(0);
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
                var transferFeature = httpContext.Features.Get<IHttpSysRequestTransferFeature>();
                Assert.False(transferFeature.IsTransferable);
                Assert.Throws<InvalidOperationException>(() => transferFeature.TransferRequest(destination));
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
                var transferFeature = httpContext.Features.Get<IHttpSysRequestTransferFeature>();
                transferFeature.TransferRequest(destination);
                Assert.False(transferFeature.IsTransferable);
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
                var transferFeature = httpContext.Features.Get<IHttpSysRequestTransferFeature>();
                Assert.Throws<InvalidOperationException>(() => transferFeature.TransferRequest(destination));
            });

            var delegationProperty = delegator.Features.Get<IServerDelegationFeature>();
            destination = delegationProperty.CreateDelegationRule(queueName, receiverAddress);

            _ = await SendRequestWithBodyAsync(delegatorAddress);
            destination?.Dispose();
        }

        [ConditionalFact]
        [DelegateSupportedCondition(false)]
        public async Task DelegationFeaturesAreNull()
        {
            using var delegator = Utilities.CreateHttpServer(out var delegatorAddress, httpContext =>
            {
                var transferFeature = httpContext.Features.Get<IHttpSysRequestTransferFeature>();
                Assert.Null(transferFeature);
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
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, uri);
            requestMessage.Content = new StringContent("Sample request body");
            var response = await client.SendAsync(requestMessage);
            return await response.Content.ReadAsStringAsync();
        }
    }
}
