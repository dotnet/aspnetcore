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
}
