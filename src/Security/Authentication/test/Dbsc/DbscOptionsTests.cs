// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
#pragma warning disable ASP0031 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.Dbsc;

public class DbscOptionsTests
{
    private const string SourceScheme = "Source";
    private const string Scheme = DbscDefaults.AuthenticationScheme;

    [Fact]
    public void Defaults_AreValid()
    {
        var options = ResolveOptions();

        Assert.Equal(SourceScheme, options.SourceScheme);
        Assert.Equal(Scheme + ".Refresh", options.RefreshScheme);
        Assert.Equal(Scheme + ".Session", options.SessionScheme);
        Assert.Equal(DbscDefaults.RegistrationPath, options.RegistrationPath);
        Assert.Equal(DbscDefaults.RefreshPath, options.RefreshPath);
    }

    [Theory]
    [InlineData(null, "The SourceScheme for scheme 'DBSC' must be nonempty.")]
    [InlineData("", "The SourceScheme for scheme 'DBSC' must be nonempty.")]
    [InlineData(Scheme, "The SourceScheme for scheme 'DBSC' must differ from the DBSC scheme itself.")]
    [InlineData(Scheme + ".Refresh", "The SourceScheme for scheme 'DBSC' must differ from RefreshScheme.")]
    [InlineData(Scheme + ".Session", "The SourceScheme for scheme 'DBSC' must differ from SessionScheme.")]
    public void InvalidSourceScheme_ThrowsFromNamedOptionsResolution(string? sourceScheme, string expectedRuleMessage)
    {
        var exception = Assert.Throws<ArgumentException>(() => ResolveOptions(o => o.SourceScheme = sourceScheme!));

        Assert.Equal(nameof(DbscOptions.SourceScheme), exception.ParamName);
        Assert.Equal($"{expectedRuleMessage} (Parameter 'SourceScheme')", exception.Message);
    }

    [Theory]
    [InlineData(nameof(DbscOptions.ShortLivedCookieExpiration), 0)]
    [InlineData(nameof(DbscOptions.ShortLivedCookieExpiration), -1)]
    [InlineData(nameof(DbscOptions.ChallengeMaxAge), 0)]
    [InlineData(nameof(DbscOptions.ChallengeMaxAge), -1)]
    public void NonPositiveDurations_ThrowFromNamedOptionsResolution(string memberName, int seconds)
    {
        var exception = Assert.Throws<ArgumentException>(() => ResolveOptions(o =>
        {
            if (memberName == nameof(DbscOptions.ShortLivedCookieExpiration))
            {
                o.ShortLivedCookieExpiration = TimeSpan.FromSeconds(seconds);
            }
            else
            {
                o.ChallengeMaxAge = TimeSpan.FromSeconds(seconds);
            }
        }));

        Assert.Equal(memberName, exception.ParamName);
        Assert.Equal(
            $"The {memberName} for scheme 'DBSC' must be positive. (Parameter '{memberName}')",
            exception.Message);
    }

    [Theory]
    [InlineData(nameof(DbscScopeRule.Type), null)]
    [InlineData(nameof(DbscScopeRule.Type), "")]
    [InlineData(nameof(DbscScopeRule.Domain), null)]
    [InlineData(nameof(DbscScopeRule.Domain), "")]
    [InlineData(nameof(DbscScopeRule.Path), null)]
    [InlineData(nameof(DbscScopeRule.Path), "")]
    public void EmptyScopeSpecificationMembers_ThrowFromNamedOptionsResolution(string memberName, string? value)
    {
        var exception = Assert.Throws<ArgumentException>(() => ResolveOptions(o =>
        {
            var scopeSpecification = new DbscScopeRule();
            switch (memberName)
            {
                case nameof(DbscScopeRule.Type):
                    scopeSpecification.Type = value!;
                    break;
                case nameof(DbscScopeRule.Domain):
                    scopeSpecification.Domain = value!;
                    break;
                default:
                    scopeSpecification.Path = value!;
                    break;
            }
            o.ScopeSpecifications.Add(scopeSpecification);
        }));

        Assert.Equal(memberName, exception.ParamName);
        Assert.Equal(
            $"The {memberName} for scope specification at index 0 for scheme 'DBSC' must be nonempty. (Parameter '{memberName}')",
            exception.Message);
    }

    [Theory]
    [InlineData(DbscDefaults.AuthenticationScheme, "DBSC")]
    [InlineData("Custom Scheme/Tenant", "Custom%20Scheme%2FTenant")]
    public void SchemeDerivedCookieNames_EscapeAuthenticationScheme(string authenticationScheme, string escapedScheme)
    {
        var services = new ServiceCollection();
        services.AddAuthentication(SourceScheme)
            .AddDbsc(authenticationScheme, options => options.SourceScheme = SourceScheme);

        using var serviceProvider = services.BuildServiceProvider();
        var cookieOptions = serviceProvider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>();

        Assert.Equal(
            $".AspNetCore.{escapedScheme}.Refresh",
            cookieOptions.Get(authenticationScheme + ".Refresh").Cookie.Name);
        Assert.Equal(
            $".AspNetCore.{escapedScheme}.Session",
            cookieOptions.Get(authenticationScheme + ".Session").Cookie.Name);
    }

    [Theory]
    [InlineData("/custom/register", "/custom/refresh")]
    [InlineData("/", "/custom/refresh")]
    [InlineData("/custom/register", "/")]
    [InlineData("/custom/quoted\"\\café 雪", "/custom/refresh")]
    public void DistinctSupportedLocalPaths_AreValid(string registrationPath, string refreshPath)
    {
        var options = ResolveOptions(o =>
        {
            o.RegistrationPath = new PathString(registrationPath);
            o.RefreshPath = new PathString(refreshPath);
        });

        Assert.Equal(registrationPath, options.RegistrationPath.Value);
        Assert.Equal(refreshPath, options.RefreshPath.Value);
    }

    [Theory]
    [InlineData("", DbscDefaults.RefreshPath, "RegistrationPath", "The RegistrationPath for scheme 'DBSC' must be nonempty.")]
    [InlineData(DbscDefaults.RegistrationPath, "", "RefreshPath", "The RefreshPath for scheme 'DBSC' must be nonempty.")]
    [InlineData("//registration.example", DbscDefaults.RefreshPath, "RegistrationPath", "The RegistrationPath for scheme 'DBSC' must not be a network-path reference beginning with '//'.")]
    [InlineData(DbscDefaults.RegistrationPath, "//refresh.example", "RefreshPath", "The RefreshPath for scheme 'DBSC' must not be a network-path reference beginning with '//'.")]
    [InlineData("/register?tenant=1", DbscDefaults.RefreshPath, "RegistrationPath", "The RegistrationPath for scheme 'DBSC' must not contain a query string ('?').")]
    [InlineData(DbscDefaults.RegistrationPath, "/refresh?tenant=1", "RefreshPath", "The RefreshPath for scheme 'DBSC' must not contain a query string ('?').")]
    [InlineData("/register#fragment", DbscDefaults.RefreshPath, "RegistrationPath", "The RegistrationPath for scheme 'DBSC' must not contain a fragment ('#').")]
    [InlineData(DbscDefaults.RegistrationPath, "/refresh#fragment", "RefreshPath", "The RefreshPath for scheme 'DBSC' must not contain a fragment ('#').")]
    [InlineData("/same", "/same", "RefreshPath", "The RefreshPath for scheme 'DBSC' must differ from RegistrationPath.")]
    [InlineData("/same", "/SAME", "RefreshPath", "The RefreshPath for scheme 'DBSC' must differ from RegistrationPath.")]
    public void InvalidLocalPaths_ThrowFromNamedOptionsResolution(
        string registrationPath,
        string refreshPath,
        string expectedParameterName,
        string expectedRuleMessage)
    {
        var exception = Assert.Throws<ArgumentException>(() => ResolveOptions(o =>
        {
            o.RegistrationPath = new PathString(registrationPath);
            o.RefreshPath = new PathString(refreshPath);
        }));

        Assert.Equal(expectedParameterName, exception.ParamName);
        Assert.Equal($"{expectedRuleMessage} (Parameter '{expectedParameterName}')", exception.Message);
    }

    [Theory]
    [InlineData("/path?query", "/path%3Fquery")]
    [InlineData("/path#fragment", "/path%23fragment")]
    [InlineData("//host/path", "//host/path")]
    [InlineData("/path\"quoted", "/path%22quoted")]
    [InlineData("/path\\backslash", "/path%5Cbackslash")]
    [InlineData("/café/雪", "/caf%C3%A9/%E9%9B%AA")]
    [InlineData("/path with spaces", "/path%20with%20spaces")]
    public void DirectPathStringConstruction_EncodesUriComponent(string value, string expectedUriComponent)
    {
        var path = new PathString(value);

        Assert.Equal(value, path.Value);
        Assert.Equal(expectedUriComponent, path.ToUriComponent());
    }

    [Theory]
    [InlineData("/path%3Fquery", "/path?query", "/path%3Fquery")]
    [InlineData("/path%23fragment", "/path#fragment", "/path%23fragment")]
    [InlineData("/path%2Fsegment", "/path%2Fsegment", "/path%2Fsegment")]
    public void ImplicitStringConversion_DecodesAndCanonicallyReencodes(
        string value,
        string expectedValue,
        string expectedUriComponent)
    {
        PathString path = value;

        Assert.Equal(expectedValue, path.Value);
        Assert.Equal(expectedUriComponent, path.ToUriComponent());
    }

    [Theory]
    [InlineData("register")]
    [InlineData("https://example.com/register")]
    public void PathStringRejectsValuesWithoutLeadingSlash_ForConstructionAndAssignment(string value)
    {
        var constructionException = Assert.Throws<ArgumentException>(() => new PathString(value));
        var assignmentException = Assert.Throws<ArgumentException>(() => new DbscOptions
        {
            RegistrationPath = value,
        });

        Assert.Equal("value", constructionException.ParamName);
        Assert.Equal("value", assignmentException.ParamName);
    }

    private static DbscOptions ResolveOptions(Action<DbscOptions>? configure = null)
    {
        var services = new ServiceCollection();
        var builder = services.AddAuthentication(SourceScheme);
        builder.AddDbsc(Scheme, options =>
        {
            options.SourceScheme = SourceScheme;
            configure?.Invoke(options);
        });

        using var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IOptionsMonitor<DbscOptions>>().Get(Scheme);
    }
}
