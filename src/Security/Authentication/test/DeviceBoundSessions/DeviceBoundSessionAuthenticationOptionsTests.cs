// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

public class DeviceBoundSessionAuthenticationOptionsTests
{
    private const string SourceScheme = "Source";
    private const string PolicyScheme = "Source.Dbsc";

    [Fact]
    public void Upgrades_DefaultAuthenticateScheme_WhenItIsAWrappedSourceScheme()
    {
        var sut = CreateSut((SourceScheme, PolicyScheme));
        var options = new AuthenticationOptions { DefaultAuthenticateScheme = SourceScheme };

        sut.PostConfigure(Options.DefaultName, options);

        Assert.Equal(PolicyScheme, options.DefaultAuthenticateScheme);
    }

    [Fact]
    public void Upgrades_EffectiveDefault_WhenOnlyDefaultSchemeIsWrappedSource()
    {
        var sut = CreateSut((SourceScheme, PolicyScheme));
        var options = new AuthenticationOptions { DefaultScheme = SourceScheme };

        sut.PostConfigure(Options.DefaultName, options);

        // The authenticate default is redirected to the policy scheme...
        Assert.Equal(PolicyScheme, options.DefaultAuthenticateScheme);
        // ...but DefaultScheme (which sign-in/out/challenge fall back to) is left on the source scheme.
        Assert.Equal(SourceScheme, options.DefaultScheme);
    }

    [Fact]
    public void ExplicitAuthenticateScheme_TakesPrecedence_OverDefaultScheme()
    {
        var sut = CreateSut((SourceScheme, PolicyScheme));
        var options = new AuthenticationOptions
        {
            DefaultScheme = SourceScheme,
            DefaultAuthenticateScheme = "Other",
        };

        // The effective authenticate scheme is "Other", not the wrapped source, so nothing changes.
        sut.PostConfigure(Options.DefaultName, options);

        Assert.Equal("Other", options.DefaultAuthenticateScheme);
    }

    [Fact]
    public void DoesNotChange_WhenDefaultIsUnrelatedScheme()
    {
        var sut = CreateSut((SourceScheme, PolicyScheme));
        var options = new AuthenticationOptions { DefaultAuthenticateScheme = "Other" };

        sut.PostConfigure(Options.DefaultName, options);

        Assert.Equal("Other", options.DefaultAuthenticateScheme);
    }

    [Fact]
    public void DoesNotInventADefault_WhenNoneConfigured()
    {
        var sut = CreateSut((SourceScheme, PolicyScheme));
        var options = new AuthenticationOptions();

        sut.PostConfigure(Options.DefaultName, options);

        Assert.Null(options.DefaultAuthenticateScheme);
        Assert.Null(options.DefaultScheme);
    }

    [Fact]
    public void DoesNotChange_SignInOrSignOutDefaults()
    {
        var sut = CreateSut((SourceScheme, PolicyScheme));
        var options = new AuthenticationOptions
        {
            DefaultScheme = SourceScheme,
            DefaultSignInScheme = SourceScheme,
            DefaultSignOutScheme = SourceScheme,
        };

        sut.PostConfigure(Options.DefaultName, options);

        Assert.Equal(PolicyScheme, options.DefaultAuthenticateScheme);
        Assert.Equal(SourceScheme, options.DefaultSignInScheme);
        Assert.Equal(SourceScheme, options.DefaultSignOutScheme);
    }

    [Fact]
    public void Upgrades_ToMatchingPolicy_WithMultipleRegistrations()
    {
        var sut = CreateSut(("A", "A.Dbsc"), ("B", "B.Dbsc"));
        var options = new AuthenticationOptions { DefaultAuthenticateScheme = "B" };

        sut.PostConfigure(Options.DefaultName, options);

        Assert.Equal("B.Dbsc", options.DefaultAuthenticateScheme);
    }

    private static PostConfigureDeviceBoundSessionAuthenticationOptions CreateSut(
        params (string source, string policy)[] mappings)
    {
        var schemes = new DeviceBoundSessionSourceSchemes();
        foreach (var (source, policy) in mappings)
        {
            schemes.PolicySchemes[source] = policy;
        }

        return new PostConfigureDeviceBoundSessionAuthenticationOptions(Options.Create(schemes));
    }
}
