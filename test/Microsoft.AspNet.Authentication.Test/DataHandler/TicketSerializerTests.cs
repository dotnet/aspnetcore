// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNet.Http.Authentication;
using Shouldly;
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
                readTicket.Principal.Identities.Count().ShouldBe(0);
                readTicket.Properties.RedirectUri.ShouldBe("bye");
                readTicket.AuthenticationScheme.ShouldBe("Hello");
            }
        }
    }
}
