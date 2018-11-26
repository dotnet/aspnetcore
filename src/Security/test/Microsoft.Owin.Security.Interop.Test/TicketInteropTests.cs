// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

/*

using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Xunit;

namespace Microsoft.Owin.Security.Interop.Test
{
    public class TicketInteropTests
    {
        [Fact]
        public void NewSerializerCanReadInteropTicket()
        {
            var identity = new ClaimsIdentity("scheme");
            identity.AddClaim(new Claim("Test", "Value"));

            var expires = DateTime.Today;
            var issued = new DateTime(1979, 11, 11);
            var properties = new Owin.Security.AuthenticationProperties();
            properties.IsPersistent = true;
            properties.RedirectUri = "/redirect";
            properties.Dictionary["key"] = "value";
            properties.ExpiresUtc = expires;
            properties.IssuedUtc = issued;

            var interopTicket = new Owin.Security.AuthenticationTicket(identity, properties);
            var interopSerializer = new AspNetTicketSerializer();

            var bytes = interopSerializer.Serialize(interopTicket);

            var newSerializer = new TicketSerializer();
            var newTicket = newSerializer.Deserialize(bytes);

            Assert.NotNull(newTicket);
            Assert.Single(newTicket.Principal.Identities);
            var newIdentity = newTicket.Principal.Identity as ClaimsIdentity;
            Assert.NotNull(newIdentity);
            Assert.Equal("scheme", newIdentity.AuthenticationType);
            Assert.True(newIdentity.HasClaim(c => c.Type == "Test" && c.Value == "Value"));
            Assert.NotNull(newTicket.Properties);
            Assert.True(newTicket.Properties.IsPersistent);
            Assert.Equal("/redirect", newTicket.Properties.RedirectUri);
            Assert.Equal("value", newTicket.Properties.Items["key"]);
            Assert.Equal(expires, newTicket.Properties.ExpiresUtc);
            Assert.Equal(issued, newTicket.Properties.IssuedUtc);
        }

        [Fact]
        public void InteropSerializerCanReadNewTicket()
        {
            var user = new ClaimsPrincipal();
            var identity = new ClaimsIdentity("scheme");
            identity.AddClaim(new Claim("Test", "Value"));
            user.AddIdentity(identity);

            var expires = DateTime.Today;
            var issued = new DateTime(1979, 11, 11);
            var properties = new AspNetCore.Authentication.AuthenticationProperties();
            properties.IsPersistent = true;
            properties.RedirectUri = "/redirect";
            properties.Items["key"] = "value";
            properties.ExpiresUtc = expires;
            properties.IssuedUtc = issued;

            var newTicket = new AspNetCore.Authentication.AuthenticationTicket(user, properties, "scheme");
            var newSerializer = new TicketSerializer();

            var bytes = newSerializer.Serialize(newTicket);

            var interopSerializer = new AspNetTicketSerializer();
            var interopTicket = interopSerializer.Deserialize(bytes);

            Assert.NotNull(interopTicket);
            var newIdentity = interopTicket.Identity;
            Assert.NotNull(newIdentity);
            Assert.Equal("scheme", newIdentity.AuthenticationType);
            Assert.True(newIdentity.HasClaim(c => c.Type == "Test" && c.Value == "Value"));
            Assert.NotNull(interopTicket.Properties);
            Assert.True(interopTicket.Properties.IsPersistent);
            Assert.Equal("/redirect", interopTicket.Properties.RedirectUri);
            Assert.Equal("value", interopTicket.Properties.Dictionary["key"]);
            Assert.Equal(expires, interopTicket.Properties.ExpiresUtc);
            Assert.Equal(issued, interopTicket.Properties.IssuedUtc);
        }
    }
}
*/


