// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpSys.Internal;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.Server.HttpSys.FunctionalTests;

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
    [DelegateSupportedCondition(false)]
    public async Task DelegationFeaturesAreNull()
    {
        // Testing the DelegateSupportedCondition
        Assert.True(Environment.OSVersion.Version < new Version(10, 0, 22000), "This should be supported on Win 11.");

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

    [ConditionalFact]
    [DelegateSupportedCondition(true)]
    public async Task UpdateDelegationRuleTest()
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
        // Send a request to ensure the rule is fully active
        var responseString = await SendRequestAsync(delegatorAddress);
        destination?.Dispose();
        destination = delegationProperty.CreateDelegationRule(queueName, receiverAddress);
        responseString = await SendRequestAsync(delegatorAddress);
        Assert.Equal(_expectedResponseString, responseString);
        destination?.Dispose();
    }

    [ConditionalFact]
    [DelegateSupportedCondition(true)]
    public async Task DelegateAfterReceiverRestart()
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

        // Stop the receiver
        receiver?.Dispose();

        // Start the receiver again but this time we need to attach to the existing queue.
        // Due to https://github.com/dotnet/aspnetcore/issues/40359, we have to manually
        // register URL prefixes and attach the server's queue to them.
        using var receiverRestarted = (MessagePump)Utilities.CreateHttpServer(out receiverAddress, async httpContext =>
        {
            await httpContext.Response.WriteAsync(_expectedResponseString);
        },
        options =>
        {
            options.RequestQueueName = queueName;
            options.RequestQueueMode = RequestQueueMode.Attach;
            options.UrlPrefixes.Clear();
            options.UrlPrefixes.Add(receiverAddress);
        });
        AttachToUrlGroup(receiverRestarted.Listener.RequestQueue);
        receiverRestarted.Listener.Options.UrlPrefixes.RegisterAllPrefixes(receiverRestarted.Listener.UrlGroup);

        responseString = await SendRequestAsync(delegatorAddress);
        Assert.Equal(_expectedResponseString, responseString);

        destination?.Dispose();
    }

    private unsafe void AttachToUrlGroup(RequestQueue requestQueue)
    {
        var info = new HttpApiTypes.HTTP_BINDING_INFO();
        info.Flags = HttpApiTypes.HTTP_FLAGS.HTTP_PROPERTY_FLAG_PRESENT;
        info.RequestQueueHandle = requestQueue.Handle.DangerousGetHandle();

        var infoptr = new IntPtr(&info);

        requestQueue.UrlGroup.SetProperty(HttpApiTypes.HTTP_SERVER_PROPERTY.HttpServerBindingProperty,
            infoptr, (uint)Marshal.SizeOf<HttpApiTypes.HTTP_BINDING_INFO>());
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
