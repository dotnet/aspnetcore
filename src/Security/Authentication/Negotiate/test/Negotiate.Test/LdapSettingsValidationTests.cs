// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication.Negotiate.Test;

public class LdapSettingsValidationTests
{
    [Fact]
    public void EnabledWithoutDomainThrows()
    {
        var settings = new LdapSettings
        {
            EnableLdapClaimResolution = true
        };

        Assert.Throws<ArgumentNullException>(() => settings.Validate());
    }

    [Fact]
    public void AccountPasswordWithoutAccountNameThrows()
    {
        var settings = new LdapSettings
        {
            EnableLdapClaimResolution = true,
            MachineAccountPassword = "Passw0rd"
        };

        Assert.Throws<ArgumentNullException>(() => settings.Validate());
    }
}
