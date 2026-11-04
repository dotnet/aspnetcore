// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authorization.Test;

public class AuthorizationPolicyFacts
{
    [Fact]
    public void RequireRoleThrowsIfEmpty()
    {
        Assert.Throws<InvalidOperationException>(() => new AuthorizationPolicyBuilder().RequireRole());
    }

    [Fact]
    public async Task CanCombineAuthorizeAttributes()
    {
        // Arrange
        var attributes = new AuthorizeAttribute[] {
                new AuthorizeAttribute(),
                new AuthorizeAttribute("1") { AuthenticationSchemes = "dupe" },
                new AuthorizeAttribute("2") { AuthenticationSchemes = "dupe" },
                new AuthorizeAttribute { Roles = "r1,r2", AuthenticationSchemes = "roles" },
            };
        var options = new AuthorizationOptions();
        options.AddPolicy("1", policy => policy.RequireClaim("1"));
        options.AddPolicy("2", policy => policy.RequireClaim("2"));

        var provider = new DefaultAuthorizationPolicyProvider(Options.Create(options));

        // Act
        var combined = await AuthorizationPolicy.CombineAsync(provider, attributes);

        // Assert
        Assert.Equal(2, combined.AuthenticationSchemes.Count());
        Assert.Contains("dupe", combined.AuthenticationSchemes);
        Assert.Contains("roles", combined.AuthenticationSchemes);
        Assert.Equal(4, combined.Requirements.Count());
        Assert.Contains(combined.Requirements, r => r is DenyAnonymousAuthorizationRequirement);
        Assert.Equal(2, combined.Requirements.OfType<ClaimsAuthorizationRequirement>().Count());
        Assert.Single(combined.Requirements.OfType<RolesAuthorizationRequirement>());
    }

    [Fact]
    public async Task CanReplaceDefaultPolicyDirectly()
    {
        // Arrange
        var attributes = new AuthorizeAttribute[] {
            new AuthorizeAttribute(),
            new AuthorizeAttribute(),
        };

        var policies = new[] { new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build() };

        var options = new AuthorizationOptions();

        var provider = new DefaultAuthorizationPolicyProvider(Options.Create(options));

        // Act
        var combined = await AuthorizationPolicy.CombineAsync(provider, attributes, policies);

        // Assert
        Assert.Single(combined.Requirements);
        Assert.Empty(combined.Requirements.OfType<DenyAnonymousAuthorizationRequirement>());
    }

    [Fact]
    public async Task CanReplaceDefaultPolicy()
    {
        // Arrange
        var attributes = new AuthorizeAttribute[] {
                new AuthorizeAttribute(),
                new AuthorizeAttribute("2") { AuthenticationSchemes = "dupe" }
            };
        var options = new AuthorizationOptions();
        options.DefaultPolicy = new AuthorizationPolicyBuilder("default").RequireClaim("default").Build();
        options.AddPolicy("2", policy => policy.RequireClaim("2"));

        var provider = new DefaultAuthorizationPolicyProvider(Options.Create(options));

        // Act
        var combined = await AuthorizationPolicy.CombineAsync(provider, attributes);

        // Assert
        Assert.Equal(2, combined.AuthenticationSchemes.Count());
        Assert.Contains("dupe", combined.AuthenticationSchemes);
        Assert.Contains("default", combined.AuthenticationSchemes);
        Assert.Equal(2, combined.Requirements.Count());
        Assert.DoesNotContain(combined.Requirements, r => r is DenyAnonymousAuthorizationRequirement);
        Assert.Equal(2, combined.Requirements.OfType<ClaimsAuthorizationRequirement>().Count());
    }

    [Fact]
    public async Task CombineMustTrimRoles()
    {
        // Arrange
        var attributes = new AuthorizeAttribute[] {
                new AuthorizeAttribute() { Roles = "r1 , r2" }
            };
        var options = new AuthorizationOptions();
        var provider = new DefaultAuthorizationPolicyProvider(Options.Create(options));

        // Act
        var combined = await AuthorizationPolicy.CombineAsync(provider, attributes);

        // Assert
        Assert.Contains(combined.Requirements, r => r is RolesAuthorizationRequirement);
        var rolesAuthorizationRequirement = combined.Requirements.OfType<RolesAuthorizationRequirement>().First();
        Assert.Equal(2, rolesAuthorizationRequirement.AllowedRoles.Count());
        Assert.Contains(rolesAuthorizationRequirement.AllowedRoles, r => r.Equals("r1"));
        Assert.Contains(rolesAuthorizationRequirement.AllowedRoles, r => r.Equals("r2"));
    }

    [Fact]
    public async Task CombineMustTrimAuthenticationScheme()
    {
        // Arrange
        var attributes = new AuthorizeAttribute[] {
                new AuthorizeAttribute() { AuthenticationSchemes = "a1 , a2" }
            };
        var options = new AuthorizationOptions();

        var provider = new DefaultAuthorizationPolicyProvider(Options.Create(options));

        // Act
        var combined = await AuthorizationPolicy.CombineAsync(provider, attributes);

        // Assert
        Assert.Equal(2, combined.AuthenticationSchemes.Count());
        Assert.Contains(combined.AuthenticationSchemes, a => a.Equals("a1"));
        Assert.Contains(combined.AuthenticationSchemes, a => a.Equals("a2"));
    }

    [Fact]
    public async Task CombineMustIgnoreEmptyAuthenticationScheme()
    {
        // Arrange
        var attributes = new AuthorizeAttribute[] {
                new AuthorizeAttribute() { AuthenticationSchemes = "a1 , , ,,, a2" }
            };
        var options = new AuthorizationOptions();

        var provider = new DefaultAuthorizationPolicyProvider(Options.Create(options));

        // Act
        var combined = await AuthorizationPolicy.CombineAsync(provider, attributes);

        // Assert
        Assert.Equal(2, combined.AuthenticationSchemes.Count());
        Assert.Contains(combined.AuthenticationSchemes, a => a.Equals("a1"));
        Assert.Contains(combined.AuthenticationSchemes, a => a.Equals("a2"));
    }

    [Fact]
    public async Task CombineMustIgnoreEmptyRoles()
    {
        // Arrange
        var attributes = new AuthorizeAttribute[] {
                new AuthorizeAttribute() { Roles = "r1 , ,, , r2" }
            };
        var options = new AuthorizationOptions();
        var provider = new DefaultAuthorizationPolicyProvider(Options.Create(options));

        // Act
        var combined = await AuthorizationPolicy.CombineAsync(provider, attributes);

        // Assert
        Assert.Contains(combined.Requirements, r => r is RolesAuthorizationRequirement);
        var rolesAuthorizationRequirement = combined.Requirements.OfType<RolesAuthorizationRequirement>().First();
        Assert.Equal(2, rolesAuthorizationRequirement.AllowedRoles.Count());
        Assert.Contains(rolesAuthorizationRequirement.AllowedRoles, r => r.Equals("r1"));
        Assert.Contains(rolesAuthorizationRequirement.AllowedRoles, r => r.Equals("r2"));
    }

    [Fact]
    public async Task CombineAsync_MetadataWithRequirementDataOnly_BuildsPolicyFromRequirements()
    {
        var requirement = new TestRequirement();
        var metadata = new object[]
        {
            new RequirementDataAttribute(requirement),
        };
        var provider = new DefaultAuthorizationPolicyProvider(Options.Create(new AuthorizationOptions()));

        var combined = await AuthorizationPolicy.CombineAsync(provider, metadata);

        Assert.NotNull(combined);
        Assert.Same(requirement, Assert.Single(combined.Requirements));
        Assert.DoesNotContain(combined.Requirements, r => r is DenyAnonymousAuthorizationRequirement);
    }

    [Fact]
    public async Task CombineAsync_MetadataWithRequirementDataAndAuthorizeData_CombinesBoth()
    {
        var requirement = new TestRequirement();
        var metadata = new object[]
        {
            new AuthorizeAttribute(),
            new RequirementDataAttribute(requirement),
        };
        var provider = new DefaultAuthorizationPolicyProvider(Options.Create(new AuthorizationOptions()));

        var combined = await AuthorizationPolicy.CombineAsync(provider, metadata);

        Assert.NotNull(combined);
        Assert.Contains(requirement, combined.Requirements);
        Assert.Contains(combined.Requirements, r => r is DenyAnonymousAuthorizationRequirement);
    }

    [Fact]
    public async Task CombineAsync_MetadataWithAttributeImplementingBothInterfaces_CombinesBoth()
    {
        var requirement = new TestRequirement();
        var metadata = new object[]
        {
            new AuthorizeAndRequirementDataAttribute(requirement),
        };
        var provider = new DefaultAuthorizationPolicyProvider(Options.Create(new AuthorizationOptions()));

        var combined = await AuthorizationPolicy.CombineAsync(provider, metadata);

        Assert.NotNull(combined);
        Assert.Contains(requirement, combined.Requirements);
        Assert.Contains(combined.Requirements, r => r is DenyAnonymousAuthorizationRequirement);
    }

    [Fact]
    public async Task CombineAsync_MetadataWithPolicyInstance_CombinesPolicy()
    {
        var policy = new AuthorizationPolicyBuilder().RequireClaim("claim").Build();
        var metadata = new object[] { policy };
        var provider = new DefaultAuthorizationPolicyProvider(Options.Create(new AuthorizationOptions()));

        var combined = await AuthorizationPolicy.CombineAsync(provider, metadata);

        Assert.NotNull(combined);
        Assert.Single(combined.Requirements.OfType<ClaimsAuthorizationRequirement>());
        Assert.DoesNotContain(combined.Requirements, r => r is DenyAnonymousAuthorizationRequirement);
    }

    [Fact]
    public async Task CombineAsync_MetadataWithoutAuthorizationData_ReturnsNull()
    {
        var metadata = new object[] { new object(), "not authorization metadata" };
        var provider = new DefaultAuthorizationPolicyProvider(Options.Create(new AuthorizationOptions()));

        var combined = await AuthorizationPolicy.CombineAsync(provider, metadata);

        Assert.Null(combined);
    }

    private sealed class TestRequirement : IAuthorizationRequirement
    {
    }

    private sealed class RequirementDataAttribute : Attribute, IAuthorizationRequirementData
    {
        private readonly IAuthorizationRequirement[] _requirements;

        public RequirementDataAttribute(params IAuthorizationRequirement[] requirements)
        {
            _requirements = requirements;
        }

        public IEnumerable<IAuthorizationRequirement> GetRequirements() => _requirements;
    }

    private sealed class AuthorizeAndRequirementDataAttribute : AuthorizeAttribute, IAuthorizationRequirementData
    {
        private readonly IAuthorizationRequirement[] _requirements;

        public AuthorizeAndRequirementDataAttribute(params IAuthorizationRequirement[] requirements)
        {
            _requirements = requirements;
        }

        public IEnumerable<IAuthorizationRequirement> GetRequirements() => _requirements;
    }
}
