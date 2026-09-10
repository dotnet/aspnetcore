// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.Extensions.Internal;
using Moq;

namespace Microsoft.AspNetCore.SignalR.Tests.Internal;

public class HubMethodDescriptorTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CombinedAuthorizationPoliciesRespectProviderCaching(bool allowsCaching)
    {
        var namedPolicy = new AuthorizationPolicyBuilder().RequireClaim("permission").Build();
        var defaultPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
        var provider = new Mock<IAuthorizationPolicyProvider>();
        provider.Setup(p => p.AllowsCachingPolicies).Returns(allowsCaching);
        provider.Setup(p => p.GetPolicyAsync("test")).ReturnsAsync(namedPolicy);
        provider.Setup(p => p.GetDefaultPolicyAsync()).ReturnsAsync(defaultPolicy);
        var descriptor = CreateDescriptor(new AuthorizeAttribute("test"), new AuthorizeAttribute());

        var first = await descriptor.GetAuthorizationPolicyAsync(provider.Object);
        var second = await descriptor.GetAuthorizationPolicyAsync(provider.Object);
        var third = await descriptor.GetAuthorizationPolicyAsync(provider.Object);

        Assert.NotNull(first);
        Assert.Equal(namedPolicy.Requirements.Concat(defaultPolicy.Requirements), first.Requirements);
        if (allowsCaching)
        {
            Assert.Same(first, second);
            Assert.Same(second, third);
        }
        else
        {
            Assert.NotSame(first, second);
            Assert.NotSame(second, third);
        }

        provider.Verify(p => p.GetPolicyAsync("test"), Times.Exactly(allowsCaching ? 1 : 3));
        provider.Verify(p => p.GetDefaultPolicyAsync(), Times.Exactly(allowsCaching ? 1 : 3));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CombinedAuthorizationPoliciesIncludeAllMetadata(bool allowsCaching)
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task NonAuthorizationMetadataDoesNotCacheNullPolicy(bool allowsCaching)
    {
        var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
        var provider = new Mock<IAuthorizationPolicyProvider>();
        provider.Setup(p => p.AllowsCachingPolicies).Returns(allowsCaching);
        provider.SetupSequence(p => p.GetFallbackPolicyAsync())
            .ReturnsAsync((AuthorizationPolicy?)null)
            .ReturnsAsync((AuthorizationPolicy?)null)
            .ReturnsAsync(policy);
        var descriptor = CreateDescriptor(new HubMethodNameAttribute("RenamedMethod"));

        Assert.Null(await descriptor.GetAuthorizationPolicyAsync(provider.Object));
        Assert.Null(await descriptor.GetAuthorizationPolicyAsync(provider.Object));
        Assert.Same(policy, await descriptor.GetAuthorizationPolicyAsync(provider.Object));
        if (allowsCaching)
        {
            Assert.Same(policy, await descriptor.GetAuthorizationPolicyAsync(provider.Object));
        }

        provider.Verify(p => p.GetFallbackPolicyAsync(), Times.Exactly(3));
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
    public async Task CachedPolicyIsNotUsedWhenProviderDisallowsCaching()
    {
        var allowsCaching = true;
        var firstPolicy = new AuthorizationPolicyBuilder().RequireClaim("first").Build();
        var secondPolicy = new AuthorizationPolicyBuilder().RequireClaim("second").Build();
        var provider = new Mock<IAuthorizationPolicyProvider>();
        provider.Setup(p => p.AllowsCachingPolicies).Returns(() => allowsCaching);
        provider.SetupSequence(p => p.GetPolicyAsync("test"))
            .ReturnsAsync(firstPolicy)
            .ReturnsAsync(secondPolicy);
        var descriptor = CreateDescriptor(new AuthorizeAttribute("test"));

        var first = await descriptor.GetAuthorizationPolicyAsync(provider.Object);
        Assert.Same(first, await descriptor.GetAuthorizationPolicyAsync(provider.Object));
        allowsCaching = false;
        var second = await descriptor.GetAuthorizationPolicyAsync(provider.Object);

        Assert.NotNull(second);
        Assert.Equal(secondPolicy.Requirements, second.Requirements);
        Assert.NotSame(first, second);
        provider.Verify(p => p.GetPolicyAsync("test"), Times.Exactly(2));
    }

    [Theory]
    [InlineData("exception")]
    [InlineData("cancellation")]
    [InlineData("missing")]
    public async Task FailedPolicyCombinationIsNotCached(string failure)
    {
        var policy = new AuthorizationPolicyBuilder().RequireClaim("permission").Build();
        var provider = new Mock<IAuthorizationPolicyProvider>();
        provider.Setup(p => p.AllowsCachingPolicies).Returns(true);
        var failedPolicy = failure switch
        {
            "exception" => Task.FromException<AuthorizationPolicy?>(new InvalidOperationException("Policy unavailable.")),
            "cancellation" => Task.FromCanceled<AuthorizationPolicy?>(new CancellationToken(canceled: true)),
            _ => Task.FromResult<AuthorizationPolicy?>(null),
        };
        provider.SetupSequence(p => p.GetPolicyAsync("test"))
            .Returns(failedPolicy)
            .ReturnsAsync(policy);
        var descriptor = CreateDescriptor(new AuthorizeAttribute("test"));

        if (failure == "cancellation")
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => descriptor.GetAuthorizationPolicyAsync(provider.Object).AsTask());
        }
        else
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => descriptor.GetAuthorizationPolicyAsync(provider.Object).AsTask());
        }

        var combinedPolicy = await descriptor.GetAuthorizationPolicyAsync(provider.Object);
        Assert.NotNull(combinedPolicy);
        Assert.Equal(policy.Requirements, combinedPolicy.Requirements);
        Assert.Same(combinedPolicy, await descriptor.GetAuthorizationPolicyAsync(provider.Object));
        provider.Verify(p => p.GetPolicyAsync("test"), Times.Exactly(2));
    }

    [Fact]
    public void CachedPolicyDoesNotKeepItsProviderAlive()
    {
        var descriptor = CreateDescriptor(new AuthorizeAttribute("test"));
        var provider = CachePolicyWithProviderReference(descriptor);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.False(provider.IsAlive);
        GC.KeepAlive(descriptor);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CachePolicyWithProviderReference(HubMethodDescriptor descriptor)
    {
        var provider = new Mock<IAuthorizationPolicyProvider>();
        var policy = new AuthorizationPolicyBuilder()
            .AddRequirements(new ProviderReferencingRequirement(provider.Object))
            .Build();
        provider.Setup(p => p.AllowsCachingPolicies).Returns(true);
        provider.Setup(p => p.GetPolicyAsync("test")).ReturnsAsync(policy);
        descriptor.GetAuthorizationPolicyAsync(provider.Object).GetAwaiter().GetResult();

        return new WeakReference(provider.Object);
    }

    private sealed class ProviderReferencingRequirement(IAuthorizationPolicyProvider provider) : IAuthorizationRequirement
    {
        public IAuthorizationPolicyProvider Provider { get; } = provider;
    }

    [Fact]
    public async Task ConcurrentColdPolicyCombinationsPublishASuccessfulPolicy()
    {
        const int invocationCount = 8;
        var policy = new AuthorizationPolicyBuilder().RequireClaim("permission").Build();
        var release = new TaskCompletionSource<AuthorizationPolicy?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = new Mock<IAuthorizationPolicyProvider>();
        provider.Setup(p => p.AllowsCachingPolicies).Returns(true);
        provider.Setup(p => p.GetPolicyAsync("test")).Returns(release.Task);
        var descriptor = CreateDescriptor(new AuthorizeAttribute("test"));
        var pending = Enumerable.Range(0, invocationCount)
            .Select(_ => descriptor.GetAuthorizationPolicyAsync(provider.Object).AsTask())
            .ToArray();

        try
        {
            Assert.All(pending, task => Assert.False(task.IsCompleted));
            provider.Verify(p => p.GetPolicyAsync("test"), Times.Exactly(invocationCount));
        }
        finally
        {
            release.TrySetResult(policy);
        }

        var combinedPolicies = await Task.WhenAll(pending).DefaultTimeout();
        Assert.All(combinedPolicies, combined =>
        {
            Assert.NotNull(combined);
            Assert.Equal(policy.Requirements, combined.Requirements);
        });
        var cachedPolicy = await descriptor.GetAuthorizationPolicyAsync(provider.Object);
        Assert.Contains(cachedPolicy, combinedPolicies);
        Assert.Same(cachedPolicy, await descriptor.GetAuthorizationPolicyAsync(provider.Object));
        provider.Verify(p => p.GetPolicyAsync("test"), Times.Exactly(invocationCount));
    }

    [Fact]
    public async Task FailedConcurrentCombinationDoesNotReplaceSuccessfulCachedPolicy()
    {
        var policy = new AuthorizationPolicyBuilder().RequireClaim("permission").Build();
        var releaseFailure = new TaskCompletionSource<AuthorizationPolicy?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = new Mock<IAuthorizationPolicyProvider>();
        provider.Setup(p => p.AllowsCachingPolicies).Returns(true);
        provider.SetupSequence(p => p.GetPolicyAsync("test"))
            .Returns(releaseFailure.Task)
            .ReturnsAsync(policy);
        var descriptor = CreateDescriptor(new AuthorizeAttribute("test"));

        var failingCombination = descriptor.GetAuthorizationPolicyAsync(provider.Object).AsTask();
        var combinedPolicy = await descriptor.GetAuthorizationPolicyAsync(provider.Object);
        releaseFailure.SetException(new InvalidOperationException("Policy unavailable."));

        await Assert.ThrowsAsync<InvalidOperationException>(() => failingCombination.DefaultTimeout());
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
