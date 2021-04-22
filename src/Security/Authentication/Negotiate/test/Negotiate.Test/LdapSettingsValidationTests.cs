// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
