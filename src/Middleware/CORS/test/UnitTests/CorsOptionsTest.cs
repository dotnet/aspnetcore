// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Cors.Infrastructure;

public class CorsOptionsTest
{
    [Fact]
    public void AddDefaultPolicy_SetsDefaultPolicyName()
    {
        // Arrange
        var corsOptions = new CorsOptions();
        var expectedPolicy = new CorsPolicy();

        // Act
        corsOptions.AddPolicy("policy1", new CorsPolicy());
        corsOptions.AddDefaultPolicy(expectedPolicy);
        corsOptions.AddPolicy("policy3", new CorsPolicy());

        // Assert
        var actualPolicy = corsOptions.GetPolicy(corsOptions.DefaultPolicyName);
        Assert.Same(expectedPolicy, actualPolicy);
    }

    [Fact]
    public void AddDefaultPolicy_OverridesDefaultPolicyName()
    {
        // Arrange
        var corsOptions = new CorsOptions();
        var expectedPolicy = new CorsPolicy();

        // Act
        corsOptions.AddDefaultPolicy(new CorsPolicy());
        corsOptions.AddDefaultPolicy(expectedPolicy);

        // Assert
        var actualPolicy = corsOptions.GetPolicy(corsOptions.DefaultPolicyName);
        Assert.Same(expectedPolicy, actualPolicy);
    }

    [Fact]
    public void AddDefaultPolicy_UsingPolicyBuilder_SetsDefaultPolicyName()
    {
        // Arrange
        var corsOptions = new CorsOptions();
        CorsPolicy expectedPolicy = null;

        // Act
        corsOptions.AddPolicy("policy1", policyBuilder =>
        {
            policyBuilder.AllowAnyOrigin().Build();
        });
        corsOptions.AddDefaultPolicy(policyBuilder =>
        {
            expectedPolicy = policyBuilder.AllowAnyOrigin().Build();
        });
        corsOptions.AddPolicy("policy3", new CorsPolicy());

        // Assert
        var actualPolicy = corsOptions.GetPolicy(corsOptions.DefaultPolicyName);
        Assert.Same(expectedPolicy, actualPolicy);
    }
}
