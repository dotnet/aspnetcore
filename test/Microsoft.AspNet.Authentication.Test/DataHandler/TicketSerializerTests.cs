// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNet.Http.Authentication;
using Xunit;

namespace Microsoft.AspNet.Authentication
{
    public class TicketSerializerTests
    {
        [Fact]
        public void NullPrincipalThrows()
        {
            var properties = new AuthenticationProperties();
            properties.RedirectUri = "bye";
            var ticket = new AuthenticationTicket(properties, "Hello");

            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            using (var reader = new BinaryReader(stream))
            {
                Assert.Throws<ArgumentNullException>(() => TicketSerializer.Write(writer, ticket));
            }
        }

        [Fact]
        public void CanRoundTripEmptyPrincipal()
        {
            var properties = new AuthenticationProperties();
            properties.RedirectUri = "bye";
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(), properties, "Hello");

            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            using (var reader = new BinaryReader(stream))
            {
                TicketSerializer.Write(writer, ticket);
                stream.Position = 0;
                var readTicket = TicketSerializer.Read(reader);
                Assert.Equal(0, readTicket.Principal.Identities.Count());
                Assert.Equal("bye", readTicket.Properties.RedirectUri);
                Assert.Equal("Hello", readTicket.AuthenticationScheme);
            }
        }

        [Fact]
        public void CanRoundTripBootstrapContext()
        {
            var properties = new AuthenticationProperties();
            properties.RedirectUri = "bye";
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(), properties, "Hello");
            ticket.Principal.AddIdentity(new ClaimsIdentity("misc") { BootstrapContext = "bootstrap" });

            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            using (var reader = new BinaryReader(stream))
            {
                TicketSerializer.Write(writer, ticket);
                stream.Position = 0;
                var readTicket = TicketSerializer.Read(reader);
                Assert.Equal(1, readTicket.Principal.Identities.Count());
                Assert.Equal("misc", readTicket.Principal.Identity.AuthenticationType);
                Assert.Equal("bootstrap", readTicket.Principal.Identities.First().BootstrapContext);
            }
        }
    }
}
