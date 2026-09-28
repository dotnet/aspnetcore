// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.Extensions.Internal;
using Moq;

namespace Microsoft.AspNetCore.SignalR.Tests.Internal;

public class HubMethodDescriptorTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CombinedAuthorizationPoliciesRespectCachingAndIncludeAllMetadata(bool allowsCaching)
    {
        var namedPolicy = new AuthorizationPolicyBuilder().RequireClaim("named").Build();
        var explicitPolicy = new AuthorizationPolicyBuilder().RequireClaim("explicit").Build();
        var requirement = new AuthorizationPolicyBuilder().RequireClaim("requirement").Build().Requirements[0];
        var provider = CreateCachingProvider(namedPolicy);
        provider.Setup(p => p.AllowsCachingPolicies).Returns(allowsCaching);
        var descriptor = CreateDescriptor(
            new AuthorizeAttribute("test"),
            explicitPolicy,
            new RequirementMetadata(requirement));

        var first = await descriptor.GetAuthorizationPolicyAsync(provider.Object);
        var second = await descriptor.GetAuthorizationPolicyAsync(provider.Object);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(namedPolicy.Requirements.Concat(explicitPolicy.Requirements).Append(requirement), first.Requirements);
        Assert.Equal(first.Requirements, second.Requirements);
        if (allowsCaching)
        {
            Assert.Same(first, second);
        }
        else
        {
            Assert.NotSame(first, second);
        }

        provider.Verify(p => p.GetPolicyAsync("test"), Times.Exactly(allowsCaching ? 1 : 2));
    }

    [Fact]
    public async Task NonAuthorizationMetadataDoesNotCacheNullPolicy()
    {
        var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
        var provider = new Mock<IAuthorizationPolicyProvider>();
        provider.Setup(p => p.AllowsCachingPolicies).Returns(true);
        provider.SetupSequence(p => p.GetFallbackPolicyAsync())
            .ReturnsAsync((AuthorizationPolicy?)null)
            .ReturnsAsync(policy);
        var descriptor = CreateDescriptor(new HubMethodNameAttribute("RenamedMethod"));

        Assert.Null(await descriptor.GetAuthorizationPolicyAsync(provider.Object));
        Assert.Same(policy, await descriptor.GetAuthorizationPolicyAsync(provider.Object));
        Assert.Same(policy, await descriptor.GetAuthorizationPolicyAsync(provider.Object));
        provider.Verify(p => p.GetFallbackPolicyAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task CachedAuthorizationPoliciesAreSeparateForEachProviderInstance()
    {
        var descriptor = CreateDescriptor(new AuthorizeAttribute("test"));
        var firstPolicy = new AuthorizationPolicyBuilder().RequireClaim("first").Build();
        var secondPolicy = new AuthorizationPolicyBuilder().RequireClaim("second").Build();
        var firstProvider = CreateCachingProvider(firstPolicy);
        var secondProvider = CreateCachingProvider(secondPolicy);

        var first = await descriptor.GetAuthorizationPolicyAsync(firstProvider.Object);
        var second = await descriptor.GetAuthorizationPolicyAsync(secondProvider.Object);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(firstPolicy.Requirements, first.Requirements);
        Assert.Equal(secondPolicy.Requirements, second.Requirements);
        Assert.NotSame(first, second);
        Assert.Same(first, await descriptor.GetAuthorizationPolicyAsync(firstProvider.Object));
        Assert.Same(second, await descriptor.GetAuthorizationPolicyAsync(secondProvider.Object));
        firstProvider.Verify(p => p.GetPolicyAsync("test"), Times.Once);
        secondProvider.Verify(p => p.GetPolicyAsync("test"), Times.Once);
    }

    [Fact]
    public async Task CachedAuthorizationPoliciesAreSeparateForEachMethod()
    {
        var provider = new Mock<IAuthorizationPolicyProvider>();
        provider.Setup(p => p.AllowsCachingPolicies).Returns(true);
        var firstPolicy = new AuthorizationPolicyBuilder().RequireClaim("first").Build();
        var secondPolicy = new AuthorizationPolicyBuilder().RequireClaim("second").Build();
        provider.Setup(p => p.GetPolicyAsync("first")).ReturnsAsync(firstPolicy);
        provider.Setup(p => p.GetPolicyAsync("second")).ReturnsAsync(secondPolicy);
        var firstDescriptor = CreateDescriptor(new AuthorizeAttribute("first"));
        var secondDescriptor = CreateDescriptor(new AuthorizeAttribute("second"));

        var first = await firstDescriptor.GetAuthorizationPolicyAsync(provider.Object);
        var second = await secondDescriptor.GetAuthorizationPolicyAsync(provider.Object);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(firstPolicy.Requirements, first.Requirements);
        Assert.Equal(secondPolicy.Requirements, second.Requirements);
        Assert.Same(first, await firstDescriptor.GetAuthorizationPolicyAsync(provider.Object));
        Assert.Same(second, await secondDescriptor.GetAuthorizationPolicyAsync(provider.Object));
        provider.Verify(p => p.GetPolicyAsync("first"), Times.Once);
        provider.Verify(p => p.GetPolicyAsync("second"), Times.Once);
    }

    [Fact]
    public async Task FailedPolicyCombinationIsNotCached()
    {
        var policy = new AuthorizationPolicyBuilder().RequireClaim("permission").Build();
        var provider = CreateCachingProvider(policy);
        provider.SetupSequence(p => p.GetPolicyAsync("test"))
            .ThrowsAsync(new InvalidOperationException("Policy unavailable."))
            .ReturnsAsync(policy);
        var descriptor = CreateDescriptor(new AuthorizeAttribute("test"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => descriptor.GetAuthorizationPolicyAsync(provider.Object).AsTask());

        var combinedPolicy = await descriptor.GetAuthorizationPolicyAsync(provider.Object);
        Assert.NotNull(combinedPolicy);
        Assert.Equal(policy.Requirements, combinedPolicy.Requirements);
        Assert.Same(combinedPolicy, await descriptor.GetAuthorizationPolicyAsync(provider.Object));
        provider.Verify(p => p.GetPolicyAsync("test"), Times.Exactly(2));
    }

    private static Mock<IAuthorizationPolicyProvider> CreateCachingProvider(AuthorizationPolicy policy)
    {
        var provider = new Mock<IAuthorizationPolicyProvider>();
        provider.Setup(p => p.AllowsCachingPolicies).Returns(true);
        provider.Setup(p => p.GetPolicyAsync("test")).ReturnsAsync(policy);
        return provider;
    }

    private sealed class RequirementMetadata(IAuthorizationRequirement requirement) : IAuthorizationRequirementData
    {
        public IEnumerable<IAuthorizationRequirement> GetRequirements() => [requirement];
    }

    private static HubMethodDescriptor CreateDescriptor(params object[] authorizationMetadata)
    {
        var executor = ObjectMethodExecutor.Create(
            typeof(MethodHub).GetMethod(nameof(MethodHub.AuthMethod))!,
            typeof(MethodHub).GetTypeInfo());
        return new HubMethodDescriptor(executor, serviceProviderIsService: null, authorizationMetadata);
    }
}
