// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
#pragma warning disable ASP0031 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

public class DeviceBoundSessionOptionsTests
{
    private const string SourceScheme = "Source";
    private const string Scheme = DeviceBoundSessionDefaults.AuthenticationScheme;

    [Fact]
    public void Defaults_AreValid()
    {
        var options = ResolveOptions();

        Assert.Equal(DeviceBoundSessionDefaults.RegistrationPath, options.RegistrationPath);
        Assert.Equal(DeviceBoundSessionDefaults.RefreshPath, options.RefreshPath);
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
    [InlineData("", DeviceBoundSessionDefaults.RefreshPath, "RegistrationPath", "The RegistrationPath for scheme 'DeviceBoundSession' must be nonempty.")]
    [InlineData(DeviceBoundSessionDefaults.RegistrationPath, "", "RefreshPath", "The RefreshPath for scheme 'DeviceBoundSession' must be nonempty.")]
    [InlineData("//registration.example", DeviceBoundSessionDefaults.RefreshPath, "RegistrationPath", "The RegistrationPath for scheme 'DeviceBoundSession' must not be a network-path reference beginning with '//'.")]
    [InlineData(DeviceBoundSessionDefaults.RegistrationPath, "//refresh.example", "RefreshPath", "The RefreshPath for scheme 'DeviceBoundSession' must not be a network-path reference beginning with '//'.")]
    [InlineData("/register?tenant=1", DeviceBoundSessionDefaults.RefreshPath, "RegistrationPath", "The RegistrationPath for scheme 'DeviceBoundSession' must not contain a query string ('?').")]
    [InlineData(DeviceBoundSessionDefaults.RegistrationPath, "/refresh?tenant=1", "RefreshPath", "The RefreshPath for scheme 'DeviceBoundSession' must not contain a query string ('?').")]
    [InlineData("/register#fragment", DeviceBoundSessionDefaults.RefreshPath, "RegistrationPath", "The RegistrationPath for scheme 'DeviceBoundSession' must not contain a fragment ('#').")]
    [InlineData(DeviceBoundSessionDefaults.RegistrationPath, "/refresh#fragment", "RefreshPath", "The RefreshPath for scheme 'DeviceBoundSession' must not contain a fragment ('#').")]
    [InlineData("/same", "/same", "RefreshPath", "The RefreshPath for scheme 'DeviceBoundSession' must differ from RegistrationPath.")]
    [InlineData("/same", "/SAME", "RefreshPath", "The RefreshPath for scheme 'DeviceBoundSession' must differ from RegistrationPath.")]
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
        var assignmentException = Assert.Throws<ArgumentException>(() => new DeviceBoundSessionOptions
        {
            RegistrationPath = value,
        });

        Assert.Equal("value", constructionException.ParamName);
        Assert.Equal("value", assignmentException.ParamName);
    }

    private static DeviceBoundSessionOptions ResolveOptions(Action<DeviceBoundSessionOptions>? configure = null)
    {
        var services = new ServiceCollection();
        var builder = services.AddAuthentication(SourceScheme);
        if (configure is null)
        {
            builder.AddDeviceBoundSession(SourceScheme);
        }
        else
        {
            builder.AddDeviceBoundSession(SourceScheme, configure);
        }

        using var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IOptionsMonitor<DeviceBoundSessionOptions>>().Get(Scheme);
    }
}
