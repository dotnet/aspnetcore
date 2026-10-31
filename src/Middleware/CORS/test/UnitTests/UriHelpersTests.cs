// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Cors.Infrastructure;

public sealed class UriHelpersTests
{
    [Theory]
    [MemberData(nameof(IsSubdomainOfTestData))]
    public void TestIsSubdomainOf(Uri subdomain, Uri domain)
    {
        // Act
        var isSubdomain = UriHelpers.IsSubdomainOf(subdomain, domain);

        // Assert
        Assert.True(isSubdomain);
    }

    [Theory]
    [MemberData(nameof(IsNotSubdomainOfTestData))]
    public void TestIsSubdomainOf_ReturnsFalse_WhenNotSubdomain(Uri subdomain, Uri domain)
    {
        // Act
        var isSubdomain = UriHelpers.IsSubdomainOf(subdomain, domain);

        // Assert
        Assert.False(isSubdomain);
    }

    public static IEnumerable<object[]> IsSubdomainOfTestData
    {
        get
        {
            return new[]
            {
                    new object[] {new Uri("http://sub.domain"), new Uri("http://domain")},
                    new object[] {new Uri("https://sub.domain"), new Uri("https://domain")},
                    new object[] {new Uri("https://sub.domain:5678"), new Uri("https://domain:5678")},
                    new object[] {new Uri("http://sub.sub.domain"), new Uri("http://domain")},
                    new object[] {new Uri("http://sub.sub.domain"), new Uri("http://sub.domain")}
                };
        }
    }

    public static IEnumerable<object[]> IsNotSubdomainOfTestData
    {
        get
        {
            return new[]
            {
                    new object[] {new Uri("http://subdomain"), new Uri("http://domain")},
                    new object[] {new Uri("https://sub.domain"), new Uri("http://domain")},
                    new object[] {new Uri("https://sub.domain:1234"), new Uri("https://domain:5678")},
                    new object[] {new Uri("http://domain.tld"), new Uri("http://domain")},
                    new object[] {new Uri("http://sub.domain.tld"), new Uri("http://domain")},
                    new object[] {new Uri("/relativeUri", UriKind.Relative), new Uri("http://domain")},
                    new object[] {new Uri("http://sub.domain"), new Uri("/relative", UriKind.Relative)}
                };
        }
    }
}
