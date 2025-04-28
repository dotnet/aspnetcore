// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;

namespace Microsoft.AspNetCore.Server.HttpSys.FunctionalTests;

public class DelegateTests : LoggedTest
{
    private static readonly string _expectedResponseString = "Hello from delegatee";

    [ConditionalFact]
    [DelegateSupportedCondition(true)]
    public void IServerDelegationFeature_IsAvailableFromServices()
    {
        var builder = new HostBuilder();
        builder.ConfigureWebHost(webHost =>
        {
            webHost.UseHttpSys();
        });
        using var host = builder.Build();
        var server = host.Services.GetRequiredService<IServer>();
        var delegationFeature = host.Services.GetRequiredService<IServerDelegationFeature>();
        Assert.Same(server, delegationFeature);
    }

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
        }, LoggerFactory);

        DelegationRule destination = default;

        using var delegator = Utilities.CreateHttpServer(out var delegatorAddress, httpContext =>
        {
            var delegateFeature = httpContext.Features.Get<IHttpSysRequestDelegationFeature>();
            delegateFeature.DelegateRequest(destination);
            return Task.CompletedTask;
        }, LoggerFactory);

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
        }, LoggerFactory);

        DelegationRule destination = default;

        using var delegator = Utilities.CreateHttpServer(out var delegatorAddress, async httpContext =>
        {
            await httpContext.Response.WriteAsync(_expectedResponseString);
            var delegateFeature = httpContext.Features.Get<IHttpSysRequestDelegationFeature>();
            Assert.False(delegateFeature.CanDelegate);
            Assert.Throws<InvalidOperationException>(() => delegateFeature.DelegateRequest(destination));
        }, LoggerFactory);

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
        }, LoggerFactory);

        DelegationRule destination = default;

        using var delegator = Utilities.CreateHttpServer(out var delegatorAddress, httpContext =>
        {
            var delegateFeature = httpContext.Features.Get<IHttpSysRequestDelegationFeature>();
            delegateFeature.DelegateRequest(destination);
            Assert.False(delegateFeature.CanDelegate);
            httpContext.Response.WriteAsync(_expectedResponseString);
            return Task.CompletedTask;
        }, LoggerFactory);

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
       }, LoggerFactory);

        DelegationRule destination = default;

        using var delegator = Utilities.CreateHttpServer(out var delegatorAddress, async httpContext =>
        {
            var memoryStream = new MemoryStream();
            await httpContext.Request.Body.CopyToAsync(memoryStream);
            var delegateFeature = httpContext.Features.Get<IHttpSysRequestDelegationFeature>();
            Assert.Throws<InvalidOperationException>(() => delegateFeature.DelegateRequest(destination));
        }, LoggerFactory);

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
        }, LoggerFactory);

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
       }, LoggerFactory);

        DelegationRule destination = default;

        using var delegator = Utilities.CreateHttpServer(out var delegatorAddress, httpContext =>
        {
            var delegateFeature = httpContext.Features.Get<IHttpSysRequestDelegationFeature>();
            delegateFeature.DelegateRequest(destination);
            return Task.CompletedTask;
        }, LoggerFactory);

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
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/60141")]
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
        }, LoggerFactory);

        DelegationRule destination = default;
        using var delegator = Utilities.CreateHttpServer(out var delegatorAddress, httpContext =>
        {
            var delegateFeature = httpContext.Features.Get<IHttpSysRequestDelegationFeature>();
            delegateFeature.DelegateRequest(destination);
            return Task.CompletedTask;
        }, LoggerFactory);

        var delegationProperty = delegator.Features.Get<IServerDelegationFeature>();
        destination = delegationProperty.CreateDelegationRule(queueName, receiverAddress);

        var responseString = await SendRequestAsync(delegatorAddress);
        Assert.Equal(_expectedResponseString, responseString);

        // Stop the receiver
        receiver?.Dispose();

        // Start the receiver again but this time we need to use CreateOrAttach to attach to the existing queue and setup the UrlPrefixes
        using var receiverRestarted = (MessagePump)Utilities.CreateHttpServer(out receiverAddress, async httpContext =>
        {
            await httpContext.Response.WriteAsync(_expectedResponseString);
        },
        options =>
        {
            options.RequestQueueName = queueName;
            options.RequestQueueMode = RequestQueueMode.CreateOrAttach;
            options.UrlPrefixes.Clear();
            options.UrlPrefixes.Add(receiverAddress);
        }, LoggerFactory);

        responseString = await SendRequestAsync(delegatorAddress);
        Assert.Equal(_expectedResponseString, responseString);

        destination?.Dispose();
    }

    [ConditionalFact]
    [DelegateSupportedCondition(true)]
    public async Task DelegateRequestTestCanSetSecurityDescriptor()
    {
        // Create a new security descriptor
        CommonSecurityDescriptor securityDescriptor = new CommonSecurityDescriptor(false, false, string.Empty);

        // Create a discretionary access control list (DACL)
        DiscretionaryAcl dacl = new DiscretionaryAcl(false, false, 2);
        dacl.AddAccess(AccessControlType.Allow, new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null), -1, InheritanceFlags.None, PropagationFlags.None);
        dacl.AddAccess(AccessControlType.Deny, new SecurityIdentifier(WellKnownSidType.BuiltinGuestsSid, null), -1, InheritanceFlags.None, PropagationFlags.None);

        // Assign the DACL to the security descriptor
        securityDescriptor.DiscretionaryAcl = dacl;

        var queueName = Guid.NewGuid().ToString();
        using var receiver = Utilities.CreateHttpServer(out var receiverAddress, async httpContext =>
        {
            await httpContext.Response.WriteAsync(_expectedResponseString);
        },
        options =>
        {
            options.RequestQueueName = queueName;
            options.RequestQueueSecurityDescriptor = securityDescriptor;
        }, LoggerFactory);

        DelegationRule destination = default;

        using var delegator = Utilities.CreateHttpServer(out var delegatorAddress, httpContext =>
        {
            var delegateFeature = httpContext.Features.Get<IHttpSysRequestDelegationFeature>();
            delegateFeature.DelegateRequest(destination);
            return Task.CompletedTask;
        }, LoggerFactory);

        var delegationProperty = delegator.Features.Get<IServerDelegationFeature>();
        destination = delegationProperty.CreateDelegationRule(queueName, receiverAddress);

        AssertPermissions(destination.Queue.Handle);
        unsafe void AssertPermissions(SafeHandle handle)
        {
            PSECURITY_DESCRIPTOR pSecurityDescriptor = new();

            WIN32_ERROR result = PInvoke.GetSecurityInfo(
                handle,
                Windows.Win32.Security.Authorization.SE_OBJECT_TYPE.SE_KERNEL_OBJECT,
                4, // DACL_SECURITY_INFORMATION
                null,
                null,
                null,
                null,
                &pSecurityDescriptor);

            var length = (int)PInvoke.GetSecurityDescriptorLength(pSecurityDescriptor);

            // Copy the security descriptor to a managed byte array
            byte[] securityDescriptorBytes = new byte[length];
            Marshal.Copy(new IntPtr(pSecurityDescriptor.Value), securityDescriptorBytes, 0, length);

            // Convert the byte array to a RawSecurityDescriptor
            var securityDescriptor = new RawSecurityDescriptor(securityDescriptorBytes, 0);

            var checkedAllowUser = false;
            var checkedDenyGuest = false;

            foreach (CommonAce ace in securityDescriptor.DiscretionaryAcl)
            {
                if (ace.SecurityIdentifier.IsWellKnown(WellKnownSidType.BuiltinGuestsSid))
                {
                    Assert.Equal(AceType.AccessDenied, ace.AceType);
                    checkedDenyGuest = true;
                }
                else if (ace.SecurityIdentifier.IsWellKnown(WellKnownSidType.BuiltinUsersSid))
                {
                    Assert.Equal(AceType.AccessAllowed, ace.AceType);
                    checkedAllowUser = true;
                }
            }

            PInvoke.LocalFree((HLOCAL)pSecurityDescriptor.Value);

            Assert.True(checkedDenyGuest && checkedAllowUser, "DACL does not contain the expected ACEs");
        }

        var responseString = await SendRequestAsync(delegatorAddress);
        Assert.Equal(_expectedResponseString, responseString);
        destination?.Dispose();
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
