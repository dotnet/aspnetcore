// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication.Negotiate.Test;

public class LdapAdapterTests
{
    [Fact]
    public void DistinguishedNameWithoutCommasSuccess()
    {
        var parts = LdapAdapter.DistinguishedNameSeparatorRegex.Split("Testing group - City");

        Assert.Equal(new[] { "Testing group - City" }, parts);
    }

    [Fact]
    public void DistinguishedNameWithEscapedCommaSuccess()
    {
        var parts = LdapAdapter.DistinguishedNameSeparatorRegex.Split(@"Testing group\,City");

        Assert.Equal(new[] { @"Testing group\,City" }, parts);
    }

    [Fact]
    public void DistinguishedNameWithNotEscapedCommaSuccess()
    {
        var parts = LdapAdapter.DistinguishedNameSeparatorRegex.Split("Testing group,City");

        Assert.Equal(new[] { "Testing group", "City" }, parts);
    }

    [Fact]
    public void DistinguishedNameWithEscapedBackslashAndNotEscapedCommaSuccess()
    {
        var parts = LdapAdapter.DistinguishedNameSeparatorRegex.Split(@"Testing group\\,City");

        Assert.Equal(new[] { @"Testing group\\", "City" }, parts);
    }

    [Fact]
    public void EscapeLdapFilterValue_PlainValue_ReturnsUnchanged()
    {
        Assert.Equal("JohnDoe", LdapAdapter.EscapeLdapFilterValue("JohnDoe"));
    }

    [Fact]
    public void EscapeLdapFilterValue_Wildcard_IsEscaped()
    {
        Assert.Equal(@"\2a", LdapAdapter.EscapeLdapFilterValue("*"));
    }

    [Fact]
    public void EscapeLdapFilterValue_Parentheses_AreEscaped()
    {
        Assert.Equal(@"John\28Dev\29", LdapAdapter.EscapeLdapFilterValue("John(Dev)"));
    }

    [Fact]
    public void EscapeLdapFilterValue_Backslash_IsEscaped()
    {
        Assert.Equal(@"DOMAIN\5cUser", LdapAdapter.EscapeLdapFilterValue(@"DOMAIN\User"));
    }

    [Fact]
    public void EscapeLdapFilterValue_NullChar_IsEscaped()
    {
        Assert.Equal(@"before\00after", LdapAdapter.EscapeLdapFilterValue("before\0after"));
    }

    [Fact]
    public void EscapeLdapFilterValue_InjectionPayload_IsNeutralized()
    {
        var malicious = "x)(sAMAccountName=*";
        Assert.Equal(@"x\29\28sAMAccountName=\2a", LdapAdapter.EscapeLdapFilterValue(malicious));
    }

    [Fact]
    public void EscapeLdapFilterValue_EmptyString_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, LdapAdapter.EscapeLdapFilterValue(string.Empty));
    }
}
