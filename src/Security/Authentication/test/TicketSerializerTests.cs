// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Authentication
{
    public class TicketSerializerTests
    {
        [Fact]
        public void CanRoundTripEmptyPrincipal()
        {
            var serializer = new TicketSerializer();
            var properties = new AuthenticationProperties();
            properties.RedirectUri = "bye";
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(), properties, "Hello");

            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            using (var reader = new BinaryReader(stream))
            {
                serializer.Write(writer, ticket);
                stream.Position = 0;
                var readTicket = serializer.Read(reader);
                Assert.Empty(readTicket.Principal.Identities);
                Assert.Equal("bye", readTicket.Properties.RedirectUri);
                Assert.Equal("Hello", readTicket.AuthenticationScheme);
            }
        }

        [Fact]
        public void CanRoundTripBootstrapContext()
        {
            var serializer = new TicketSerializer();
            var properties = new AuthenticationProperties();

            var ticket = new AuthenticationTicket(new ClaimsPrincipal(), properties, "Hello");
            ticket.Principal.AddIdentity(new ClaimsIdentity("misc") { BootstrapContext = "bootstrap" });

            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            using (var reader = new BinaryReader(stream))
            {
                serializer.Write(writer, ticket);
                stream.Position = 0;
                var readTicket = serializer.Read(reader);
                Assert.Single(readTicket.Principal.Identities);
                Assert.Equal("misc", readTicket.Principal.Identity.AuthenticationType);
                Assert.Equal("bootstrap", readTicket.Principal.Identities.First().BootstrapContext);
            }
        }

        [Fact]
        public void CanRoundTripActorIdentity()
        {
            var serializer = new TicketSerializer();
            var properties = new AuthenticationProperties();

            var actor = new ClaimsIdentity("actor");
            actor.Label = "Famous";
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(), properties, "Hello");
            ticket.Principal.AddIdentity(new ClaimsIdentity("misc") { Actor = actor, Label = "Top" });

            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            using (var reader = new BinaryReader(stream))
            {
                serializer.Write(writer, ticket);
                stream.Position = 0;
                var readTicket = serializer.Read(reader);
                Assert.Single(readTicket.Principal.Identities);
                Assert.Equal("misc", readTicket.Principal.Identity.AuthenticationType);
                Assert.Equal("Top", readTicket.Principal.Identities.First().Label);

                var identity = (ClaimsIdentity)readTicket.Principal.Identity;
                Assert.NotNull(identity.Actor);
                Assert.Equal("actor", identity.Actor.AuthenticationType);
                Assert.Equal("Famous", identity.Actor.Label);
            }
        }

        [ConditionalFact]
        [FrameworkSkipCondition(
            RuntimeFrameworks.Mono,
            SkipReason = "Test fails with Mono 4.0.4. Build rarely reaches testing with Mono 4.2.1")]
        public void CanRoundTripClaimProperties()
        {
            var serializer = new TicketSerializer();
            var properties = new AuthenticationProperties();

            var claim = new Claim("type", "value", "valueType", "issuer", "original-issuer");
            claim.Properties.Add("property-1", "property-value");

            // Note: a null value MUST NOT result in a crash
            // and MUST instead be treated like an empty string.
            claim.Properties.Add("property-2", null);

            var ticket = new AuthenticationTicket(new ClaimsPrincipal(), properties, "Hello");
            var id = new ClaimsIdentity(new[] { claim }, "misc");
            id.Label = "Label";
            ticket.Principal.AddIdentity(id);

            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            using (var reader = new BinaryReader(stream))
            {
                serializer.Write(writer, ticket);
                stream.Position = 0;
                var readTicket = serializer.Read(reader);
                Assert.Single(readTicket.Principal.Identities);
                Assert.Equal("misc", readTicket.Principal.Identity.AuthenticationType);
                Assert.Equal("Label", readTicket.Principal.Identities.First().Label);

                var readClaim = readTicket.Principal.FindFirst("type");
                Assert.NotNull(claim);
                Assert.Equal("type", claim.Type);
                Assert.Equal("value", claim.Value);
                Assert.Equal("valueType", claim.ValueType);
                Assert.Equal("issuer", claim.Issuer);
                Assert.Equal("original-issuer", claim.OriginalIssuer);

                var property1 = readClaim.Properties["property-1"];
                Assert.Equal("property-value", property1);

                var property2 = readClaim.Properties["property-2"];
                Assert.Equal(string.Empty, property2);
            }
        }
    }
}
