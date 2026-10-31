// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.OutputCaching.Tests;

public class DefaultOutputCachePolicyProviderTests
{
    [Fact]
    public void GetBasePolicies_WithBasePolicies_ReturnsConfiguredPolicies()
    {
        // Arrange
        var options = new OutputCacheOptions();
        var policy1 = new OutputCachePolicyBuilder().Expire(TimeSpan.FromMinutes(5)).Build();
        var policy2 = new OutputCachePolicyBuilder().Expire(TimeSpan.FromMinutes(10)).Build();
        
        options.AddBasePolicy(policy1);
        options.AddBasePolicy(policy2);

        var outputCacheOptions = Options.Create(options);
        var policyProvider = new DefaultOutputCachePolicyProvider(outputCacheOptions);

        // Act
        var actualPolicies = policyProvider.GetBasePolicies();

        // Assert
        Assert.Equal(2, actualPolicies.Count);
        Assert.Same(policy1, actualPolicies[0]);
        Assert.Same(policy2, actualPolicies[1]);
    }

    [Fact]
    public void GetBasePolicies_WithoutBasePolicies_ReturnsEmptyList()
    {
        // Arrange
        var options = new OutputCacheOptions();
        var outputCacheOptions = Options.Create(options);
        var policyProvider = new DefaultOutputCachePolicyProvider(outputCacheOptions);

        // Act
        var actualPolicies = policyProvider.GetBasePolicies();

        // Assert
        Assert.NotNull(actualPolicies);
        Assert.Empty(actualPolicies);
    }

    [Fact]
    public async Task GetPolicyAsync_WithNamedPolicy_ReturnsPolicy()
    {
        // Arrange
        var options = new OutputCacheOptions();
        var policy = new OutputCachePolicyBuilder().Expire(TimeSpan.FromMinutes(15)).Build();
        options.AddPolicy("TestPolicy", policy);

        var outputCacheOptions = Options.Create(options);
        var policyProvider = new DefaultOutputCachePolicyProvider(outputCacheOptions);

        // Act
        var actualPolicy = await policyProvider.GetPolicyAsync("TestPolicy");

        // Assert
        Assert.NotNull(actualPolicy);
        Assert.Same(policy, actualPolicy);
    }

    [Theory]
    [InlineData("")]
    [InlineData("PolicyName")]
    [InlineData("AnotherPolicy")]
    public async Task GetPolicyAsync_GetsNamedPolicy(string policyName)
    {
        // Arrange
        var options = new OutputCacheOptions();
        var policy = new OutputCachePolicyBuilder().Expire(TimeSpan.FromSeconds(30)).Build();
        options.AddPolicy(policyName, policy);

        var outputCacheOptions = Options.Create(options);
        var policyProvider = new DefaultOutputCachePolicyProvider(outputCacheOptions);

        // Act
        var actualPolicy = await policyProvider.GetPolicyAsync(policyName);

        // Assert
        Assert.NotNull(actualPolicy);
        Assert.Same(policy, actualPolicy);
    }

    [Fact]
    public async Task GetPolicyAsync_WithNonExistentPolicy_ReturnsNull()
    {
        // Arrange
        var options = new OutputCacheOptions();
        var outputCacheOptions = Options.Create(options);
        var policyProvider = new DefaultOutputCachePolicyProvider(outputCacheOptions);

        // Act
        var actualPolicy = await policyProvider.GetPolicyAsync("NonExistentPolicy");

        // Assert
        Assert.Null(actualPolicy);
    }

    [Fact]
    public async Task GetPolicyAsync_WithMultipleNamedPolicies_ReturnsCorrectPolicy()
    {
        // Arrange
        var options = new OutputCacheOptions();
        var policy1 = new OutputCachePolicyBuilder().Expire(TimeSpan.FromMinutes(5)).Build();
        var policy2 = new OutputCachePolicyBuilder().Expire(TimeSpan.FromMinutes(10)).Build();
        var policy3 = new OutputCachePolicyBuilder().Expire(TimeSpan.FromMinutes(15)).Build();
        
        options.AddPolicy("Fast", policy1);
        options.AddPolicy("Medium", policy2);
        options.AddPolicy("Slow", policy3);

        var outputCacheOptions = Options.Create(options);
        var policyProvider = new DefaultOutputCachePolicyProvider(outputCacheOptions);

        // Act
        var actualPolicy1 = await policyProvider.GetPolicyAsync("Fast");
        var actualPolicy2 = await policyProvider.GetPolicyAsync("Medium");
        var actualPolicy3 = await policyProvider.GetPolicyAsync("Slow");

        // Assert
        Assert.Same(policy1, actualPolicy1);
        Assert.Same(policy2, actualPolicy2);
        Assert.Same(policy3, actualPolicy3);
    }

    [Fact]
    public async Task GetPolicyAsync_CaseInsensitivePolicyName()
    {
        // Arrange
        var options = new OutputCacheOptions();
        var policy = new OutputCachePolicyBuilder().Expire(TimeSpan.FromMinutes(20)).Build();
        options.AddPolicy("TestPolicy", policy);

        var outputCacheOptions = Options.Create(options);
        var policyProvider = new DefaultOutputCachePolicyProvider(outputCacheOptions);

        // Act
        var actualPolicyLower = await policyProvider.GetPolicyAsync("testpolicy");
        var actualPolicyUpper = await policyProvider.GetPolicyAsync("TESTPOLICY");
        var actualPolicyMixed = await policyProvider.GetPolicyAsync("TeStPoLiCy");

        // Assert
        Assert.Same(policy, actualPolicyLower);
        Assert.Same(policy, actualPolicyUpper);
        Assert.Same(policy, actualPolicyMixed);
    }

    [Fact]
    public void GetBasePolicies_WithBuilderAction_ReturnsPolicy()
    {
        // Arrange
        var options = new OutputCacheOptions();
        options.AddBasePolicy(builder => builder.Expire(TimeSpan.FromMinutes(30)));

        var outputCacheOptions = Options.Create(options);
        var policyProvider = new DefaultOutputCachePolicyProvider(outputCacheOptions);

        // Act
        var actualPolicies = policyProvider.GetBasePolicies();

        // Assert
        Assert.Single(actualPolicies);
        Assert.NotNull(actualPolicies[0]);
    }

    [Fact]
    public async Task GetPolicyAsync_WithBuilderAction_ReturnsPolicy()
    {
        // Arrange
        var options = new OutputCacheOptions();
        options.AddPolicy("BuilderPolicy", builder => builder
            .Expire(TimeSpan.FromMinutes(45))
            .SetVaryByQuery("version"));

        var outputCacheOptions = Options.Create(options);
        var policyProvider = new DefaultOutputCachePolicyProvider(outputCacheOptions);

        // Act
        var actualPolicy = await policyProvider.GetPolicyAsync("BuilderPolicy");

        // Assert
        Assert.NotNull(actualPolicy);
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DefaultOutputCachePolicyProvider(null!));
    }

    [Fact]
    public async Task GetPolicyAsync_WithNullPolicyName_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new OutputCacheOptions();
        var outputCacheOptions = Options.Create(options);
        var policyProvider = new DefaultOutputCachePolicyProvider(outputCacheOptions);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await policyProvider.GetPolicyAsync(null!));
    }
}

