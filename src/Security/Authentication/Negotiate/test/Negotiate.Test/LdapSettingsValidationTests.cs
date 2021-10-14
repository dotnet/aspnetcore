// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.Negotiate.Test
{
    public class LdapSettingsValidationTests
    {
        [Fact]
        public void EnabledWithoutDomainThrows()
        {
            var settings = new LdapSettings
            {
                EnableLdapClaimResolution = true
            };

            Assert.Throws<ArgumentException>(() => settings.Validate());
        }

        [Fact]
        public void AccountPasswordWithoutAccountNameThrows()
        {
            var settings = new LdapSettings
            {
                EnableLdapClaimResolution = true,
                MachineAccountPassword = "Passw0rd"
            };

            Assert.Throws<ArgumentException>(() => settings.Validate());
        }
    }
}
