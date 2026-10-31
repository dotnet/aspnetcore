// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Authentication;

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
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(), properties, "Hello");
        ticket.Principal.AddIdentity(new ClaimsIdentity("misc") { Actor = actor });

        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream))
        using (var reader = new BinaryReader(stream))
        {
            serializer.Write(writer, ticket);
            stream.Position = 0;
            var readTicket = serializer.Read(reader);
            Assert.Single(readTicket.Principal.Identities);
            Assert.Equal("misc", readTicket.Principal.Identity.AuthenticationType);

            var identity = (ClaimsIdentity)readTicket.Principal.Identity;
            Assert.NotNull(identity.Actor);
            Assert.Equal("actor", identity.Actor.AuthenticationType);
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
        ticket.Principal.AddIdentity(new ClaimsIdentity(new[] { claim }, "misc"));

        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream))
        using (var reader = new BinaryReader(stream))
        {
            serializer.Write(writer, ticket);
            stream.Position = 0;
            var readTicket = serializer.Read(reader);
            Assert.Single(readTicket.Principal.Identities);
            Assert.Equal("misc", readTicket.Principal.Identity.AuthenticationType);

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

    [Fact]
    public void Deserialize_MatchesRead()
    {
        var ticket = CreateComplexTicket();
        var data = TicketSerializer.Default.Serialize(ticket);

        var deserialized = TicketSerializer.Default.Deserialize(data);
        var read = ReadWithBinaryReader(new TicketSerializer(), data);

        AssertTicketsEqual(ticket, deserialized);
        AssertTicketsEqual(read, deserialized);
    }

    [Fact]
    public void Deserialize_UsesDefaultValuesForPlaceholders()
    {
        var identity = new ClaimsIdentity("auth", "custom-name", "custom-role");
        identity.AddClaim(new Claim("custom-name", "name-value"));
        identity.AddClaim(new Claim("type", "value"));
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), "scheme");

        var readTicket = TicketSerializer.Default.Deserialize(TicketSerializer.Default.Serialize(ticket));

        var readIdentity = Assert.Single(readTicket.Principal.Identities);
        Assert.Equal("custom-name", readIdentity.NameClaimType);
        Assert.Equal("custom-role", readIdentity.RoleClaimType);
        var claims = readIdentity.Claims.ToArray();
        Assert.Equal("custom-name", claims[0].Type);
        Assert.Equal("name-value", claims[0].Value);
        Assert.Equal(ClaimValueTypes.String, claims[1].ValueType);
        Assert.Equal(ClaimsIdentity.DefaultIssuer, claims[1].Issuer);
        Assert.Equal(ClaimsIdentity.DefaultIssuer, claims[1].OriginalIssuer);
        Assert.Same(readIdentity, claims[1].Subject);
    }

    [Fact]
    public void Deserialize_ReturnsNullForUnknownVersion()
    {
        var data = TicketSerializer.Default.Serialize(CreateComplexTicket());
        data[0] = 4;

        Assert.Null(TicketSerializer.Default.Deserialize(data));
    }

    [Fact]
    public void Deserialize_ThrowsSameExceptionAsReadForTruncatedData()
    {
        var data = TicketSerializer.Default.Serialize(CreateComplexTicket());

        // Every truncation must fail the same way as BinaryReader-based deserialization.
        for (var length = 0; length < data.Length; length++)
        {
            var truncated = data.AsSpan(0, length).ToArray();
            var expected = Record.Exception(() => ReadWithBinaryReader(new TicketSerializer(), truncated));
            var actual = Record.Exception(() => TicketSerializer.Default.Deserialize(truncated));

            Assert.NotNull(expected);
            Assert.NotNull(actual);
            Assert.Equal(expected.GetType(), actual.GetType());
        }
    }

    [Fact]
    public void Deserialize_ThrowsForInvalidStringLength()
    {
        // Version, then a scheme whose 7-bit encoded length has too many bytes.
        var data = new byte[] { 5, 0, 0, 0, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x01 };

        var expected = Record.Exception(() => ReadWithBinaryReader(new TicketSerializer(), data));
        var actual = Record.Exception(() => TicketSerializer.Default.Deserialize(data));

        Assert.IsType<FormatException>(expected);
        Assert.IsType<FormatException>(actual);

        // Negative string length.
        data = new byte[] { 5, 0, 0, 0, 0xFF, 0xFF, 0xFF, 0xFF, 0x0F };

        expected = Record.Exception(() => ReadWithBinaryReader(new TicketSerializer(), data));
        actual = Record.Exception(() => TicketSerializer.Default.Deserialize(data));

        Assert.IsType<IOException>(expected);
        Assert.IsType<IOException>(actual);
    }

    [Fact]
    public void Deserialize_ThrowsForNullData()
    {
        Assert.Throws<ArgumentNullException>(() => TicketSerializer.Default.Deserialize(null));
        Assert.Throws<ArgumentNullException>(() => PropertiesSerializer.Default.Deserialize(null));
    }

    [Fact]
    public void Deserialize_CallsOverriddenReadMethodsOnDerivedSerializer()
    {
        var serializer = new CustomClaimSerializer();
        var data = serializer.Serialize(CreateComplexTicket());

        var readTicket = serializer.Deserialize(data);

        Assert.All(readTicket.Principal.Claims, c => Assert.Equal("custom", c.Properties["read-by"]));
    }

    [Fact]
    public void PropertiesSerializer_Deserialize_MatchesRead()
    {
        var properties = CreateComplexTicket().Properties;
        var data = PropertiesSerializer.Default.Serialize(properties);

        var deserialized = PropertiesSerializer.Default.Deserialize(data);

        using var reader = new BinaryReader(new MemoryStream(data));
        var read = PropertiesSerializer.Default.Read(reader);

        Assert.Equal(properties.Items.Select(i => KeyValuePair.Create(i.Key, i.Value ?? string.Empty)), deserialized.Items);
        Assert.Equal(read.Items, deserialized.Items);
    }

    [Fact]
    public void PropertiesSerializer_Deserialize_CallsOverriddenRead()
    {
        var serializer = new CustomPropertiesSerializer();
        var data = serializer.Serialize(new AuthenticationProperties { RedirectUri = "/" });

        var properties = serializer.Deserialize(data);

        Assert.Equal("custom", properties.Items["read-by"]);
    }

    private static AuthenticationTicket ReadWithBinaryReader(TicketSerializer serializer, byte[] data)
    {
        using var reader = new BinaryReader(new MemoryStream(data));
        return serializer.Read(reader);
    }

    private static AuthenticationTicket CreateComplexTicket()
    {
        var longValue = string.Concat(Enumerable.Repeat("long-value-\u00e9\u4e2d\U0001F600-", 40));

        var actor = new ClaimsIdentity("actor-auth");
        actor.AddClaim(new Claim("actor-claim", "actor-value"));

        var first = new ClaimsIdentity("Identity.Application") { BootstrapContext = "bootstrap", Actor = actor };
        first.AddClaim(new Claim(ClaimTypes.NameIdentifier, "6f1b6c2e-43c5-4d8f-9a55-3b7f0c1f2a9d"));
        first.AddClaim(new Claim(ClaimTypes.Name, "someone@example.com"));
        first.AddClaim(new Claim(ClaimTypes.Role, "Admin"));
        first.AddClaim(new Claim("unicode", "caf\u00e9 \u4e2d\u6587 \U0001F600"));
        first.AddClaim(new Claim("long", longValue));
        first.AddClaim(new Claim("empty", string.Empty));
        first.AddClaim(new Claim("typed", "42", ClaimValueTypes.Integer32, "issuer", "original-issuer"));
        first.AddClaim(new Claim("same-issuer", "v", ClaimValueTypes.String, "issuer"));
        var withProperties = new Claim("with-properties", "v");
        withProperties.Properties.Add("p1", "v1");
        withProperties.Properties.Add("p2", null);
        first.AddClaim(withProperties);

        var second = new ClaimsIdentity(authenticationType: null, nameType: "n", roleType: "r");
        second.AddClaim(new Claim("n", "second-name"));

        var properties = new AuthenticationProperties
        {
            IssuedUtc = new DateTimeOffset(2026, 10, 31, 0, 0, 0, TimeSpan.Zero),
            ExpiresUtc = new DateTimeOffset(2026, 11, 14, 0, 0, 0, TimeSpan.Zero),
            IsPersistent = true,
            RedirectUri = longValue,
        };
        properties.Items["empty"] = string.Empty;
        properties.Items["null"] = null;

        return new AuthenticationTicket(new ClaimsPrincipal(new[] { first, second }), properties, "Identity.Application");
    }

    private static void AssertTicketsEqual(AuthenticationTicket expected, AuthenticationTicket actual)
    {
        Assert.Equal(expected.AuthenticationScheme, actual.AuthenticationScheme);
        Assert.Equal(
            expected.Properties.Items.Select(i => KeyValuePair.Create(i.Key, i.Value ?? string.Empty)),
            actual.Properties.Items);

        var expectedIdentities = expected.Principal.Identities.ToArray();
        var actualIdentities = actual.Principal.Identities.ToArray();
        Assert.Equal(expectedIdentities.Length, actualIdentities.Length);
        for (var i = 0; i < expectedIdentities.Length; i++)
        {
            AssertIdentitiesEqual(expectedIdentities[i], actualIdentities[i]);
        }
    }

    private static void AssertIdentitiesEqual(ClaimsIdentity expected, ClaimsIdentity actual)
    {
        Assert.Equal(expected.AuthenticationType ?? string.Empty, actual.AuthenticationType);
        Assert.Equal(expected.NameClaimType, actual.NameClaimType);
        Assert.Equal(expected.RoleClaimType, actual.RoleClaimType);
        Assert.Equal(expected.BootstrapContext, actual.BootstrapContext);

        var expectedClaims = expected.Claims.ToArray();
        var actualClaims = actual.Claims.ToArray();
        Assert.Equal(expectedClaims.Length, actualClaims.Length);
        for (var i = 0; i < expectedClaims.Length; i++)
        {
            Assert.Equal(expectedClaims[i].Type, actualClaims[i].Type);
            Assert.Equal(expectedClaims[i].Value, actualClaims[i].Value);
            Assert.Equal(expectedClaims[i].ValueType, actualClaims[i].ValueType);
            Assert.Equal(expectedClaims[i].Issuer, actualClaims[i].Issuer);
            Assert.Equal(expectedClaims[i].OriginalIssuer, actualClaims[i].OriginalIssuer);
            Assert.Same(actual, actualClaims[i].Subject);
            Assert.Equal(
                expectedClaims[i].Properties.Select(p => KeyValuePair.Create(p.Key, p.Value ?? string.Empty)),
                actualClaims[i].Properties);
        }

        if (expected.Actor is null)
        {
            Assert.Null(actual.Actor);
        }
        else
        {
            AssertIdentitiesEqual(expected.Actor, actual.Actor);
        }
    }

    private sealed class CustomClaimSerializer : TicketSerializer
    {
        protected override Claim ReadClaim(BinaryReader reader, ClaimsIdentity identity)
        {
            var claim = base.ReadClaim(reader, identity);
            claim.Properties["read-by"] = "custom";
            return claim;
        }
    }

    private sealed class CustomPropertiesSerializer : PropertiesSerializer
    {
        public override AuthenticationProperties Read(BinaryReader reader)
        {
            var properties = base.Read(reader);
            properties.Items["read-by"] = "custom";
            return properties;
        }
    }
}
