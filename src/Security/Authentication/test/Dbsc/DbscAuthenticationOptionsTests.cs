// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
#pragma warning disable ASP0031 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.Dbsc;

public class DbscAuthenticationOptionsTests
{
    private const string SourceScheme = "Source";
    private const string DbscScheme = "Source.Dbsc";

    [Fact]
    public void Upgrades_DefaultAuthenticateScheme_WhenItIsAWrappedSourceScheme()
    {
        var sut = CreateSut((SourceScheme, DbscScheme));
        var options = new AuthenticationOptions { DefaultAuthenticateScheme = SourceScheme };

        sut.PostConfigure(Options.DefaultName, options);

        Assert.Equal(DbscScheme, options.DefaultAuthenticateScheme);
    }

    [Fact]
    public void Upgrades_EffectiveDefault_WhenOnlyDefaultSchemeIsWrappedSource()
    {
        var sut = CreateSut((SourceScheme, DbscScheme));
        var options = new AuthenticationOptions { DefaultScheme = SourceScheme };

        sut.PostConfigure(Options.DefaultName, options);

        // The authenticate default is redirected to the DBSC scheme...
        Assert.Equal(DbscScheme, options.DefaultAuthenticateScheme);
        // ...but DefaultScheme (which sign-in/out/challenge fall back to) is left on the source scheme.
        Assert.Equal(SourceScheme, options.DefaultScheme);
    }

    [Fact]
    public void ExplicitAuthenticateScheme_TakesPrecedence_OverDefaultScheme()
    {
        var sut = CreateSut((SourceScheme, DbscScheme));
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
        var sut = CreateSut((SourceScheme, DbscScheme));
        var options = new AuthenticationOptions { DefaultAuthenticateScheme = "Other" };

        sut.PostConfigure(Options.DefaultName, options);

        Assert.Equal("Other", options.DefaultAuthenticateScheme);
    }

    [Fact]
    public void DoesNotInventADefault_WhenNoneConfigured()
    {
        var sut = CreateSut((SourceScheme, DbscScheme));
        var options = new AuthenticationOptions();

        sut.PostConfigure(Options.DefaultName, options);

        Assert.Null(options.DefaultAuthenticateScheme);
        Assert.Null(options.DefaultScheme);
    }

    [Fact]
    public void DoesNotChange_SignInOrSignOutDefaults()
    {
        var sut = CreateSut((SourceScheme, DbscScheme));
        var options = new AuthenticationOptions
        {
            DefaultScheme = SourceScheme,
            DefaultSignInScheme = SourceScheme,
            DefaultSignOutScheme = SourceScheme,
        };

        sut.PostConfigure(Options.DefaultName, options);

        Assert.Equal(DbscScheme, options.DefaultAuthenticateScheme);
        Assert.Equal(SourceScheme, options.DefaultSignInScheme);
        Assert.Equal(SourceScheme, options.DefaultSignOutScheme);
    }

    [Fact]
    public void Upgrades_ToMatchingDbscScheme_WithMultipleRegistrations()
    {
        var sut = CreateSut(("A", "A.Dbsc"), ("B", "B.Dbsc"));
        var options = new AuthenticationOptions { DefaultAuthenticateScheme = "B" };

        sut.PostConfigure(Options.DefaultName, options);

        Assert.Equal("B.Dbsc", options.DefaultAuthenticateScheme);
    }

    [Fact]
    public void MultipleDbscSchemes_WithSameSourceScheme_ThrowWhenOptionsMaterialize()
    {
        const string firstDbscScheme = "FirstDbsc";
        const string secondDbscScheme = "SecondDbsc";
        using var serviceProvider = CreateServiceProvider(
            (firstDbscScheme, SourceScheme),
            (secondDbscScheme, SourceScheme));
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<DbscOptions>>();

        _ = options.Get(firstDbscScheme);
        var exception = Assert.Throws<InvalidOperationException>(() => options.Get(secondDbscScheme));

        Assert.Contains(firstDbscScheme, exception.Message);
        Assert.Contains(secondDbscScheme, exception.Message);
        Assert.Contains(SourceScheme, exception.Message);
    }

    [Fact]
    public void MultipleDbscSchemes_WithDifferentSourceSchemes_MaterializeSuccessfully()
    {
        using var serviceProvider = CreateServiceProvider(
            ("FirstDbsc", "FirstSource"),
            ("SecondDbsc", "SecondSource"));
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<DbscOptions>>();

        Assert.Equal("FirstSource", options.Get("FirstDbsc").SourceScheme);
        Assert.Equal("SecondSource", options.Get("SecondDbsc").SourceScheme);
    }

    [Fact]
    public void SameDbscScheme_CanClaimSourceSchemeRepeatedly()
    {
        var sourceSchemes = new DbscSourceSchemes();
        sourceSchemes.DbscSchemes.Add(DbscScheme);
        var sut = new PostConfigureDbscOptions(Options.Create(sourceSchemes));

        sut.PostConfigure(DbscScheme, new DbscOptions { SourceScheme = SourceScheme });
        sut.PostConfigure(DbscScheme, new DbscOptions { SourceScheme = SourceScheme });
    }

    private static PostConfigureDbscAuthenticationOptions CreateSut(
        params (string source, string dbsc)[] mappings)
    {
        var schemes = new DbscSourceSchemes();
        var optionsByScheme = new Dictionary<string, DbscOptions>(StringComparer.Ordinal);
        foreach (var (source, dbsc) in mappings)
        {
            schemes.DbscSchemes.Add(dbsc);
            schemes.ClaimSourceScheme(dbsc, source);
            optionsByScheme[dbsc] = new DbscOptions { SourceScheme = source };
        }

        var optionsMonitor = new Mock<IOptionsMonitor<DbscOptions>>();
        optionsMonitor
            .Setup(monitor => monitor.Get(It.IsAny<string>()))
            .Returns((string? name) => optionsByScheme[name!]);

        return new PostConfigureDbscAuthenticationOptions(Options.Create(schemes), optionsMonitor.Object);
    }

    private static ServiceProvider CreateServiceProvider(params (string Dbsc, string Source)[] mappings)
    {
        var services = new ServiceCollection();
        var builder = services.AddAuthentication();
        foreach (var (dbsc, source) in mappings)
        {
            builder.AddDbsc(dbsc, options => options.SourceScheme = source);
        }

        return services.BuildServiceProvider();
    }
}
