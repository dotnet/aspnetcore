// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;

namespace Microsoft.AspNetCore.Authentication;

public class TicketSerializerWriteTests
{
    // Subclasses always use the BinaryWriter-based virtual Write methods, so they produce the reference payload.
    private sealed class ReferenceTicketSerializer : TicketSerializer { }

    private sealed class ReferencePropertiesSerializer : PropertiesSerializer { }

    private static byte[] SerializeWithBinaryWriter(AuthenticationTicket ticket)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream))
        {
            TicketSerializer.Default.Write(writer, ticket);
        }
        return stream.ToArray();
    }

    private static AuthenticationTicket CreateComplexTicket(int claimCount)
    {
        var identity = new ClaimsIdentity("Cookies", "custom-name", "custom-role");
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "user-id"));
        identity.AddClaim(new Claim("custom-name", "Ünïcödé 名前 😀"));
        identity.AddClaim(new Claim("custom-role", "admin", ClaimValueTypes.String, "issuer", "original-issuer"));
        identity.AddClaim(new Claim(ClaimTypes.Email, "user@example.com", ClaimValueTypes.Email, "issuer"));
        identity.AddClaim(new Claim("long", new string('x', 300)));
        identity.AddClaim(new Claim("verylong", new string('y', 20_000)));
        identity.AddClaim(new Claim("empty", string.Empty));
        identity.AddClaim(new Claim(ClaimsIdentity.DefaultNameClaimType, "default-name-type"));
        for (var i = 0; i < claimCount; i++)
        {
            identity.AddClaim(new Claim("claim" + i, "value" + i));
        }

        var withProperties = new Claim("with-props", "v");
        withProperties.Properties["p1"] = "pv1";
        withProperties.Properties["ü"] = "é";
        identity.AddClaim(withProperties);

        identity.BootstrapContext = "bootstrap-token";
        identity.Actor = new ClaimsIdentity("actor-auth");
        identity.Actor.AddClaim(new Claim("actor-claim", "actor-value"));
        identity.Actor.Actor = new ClaimsIdentity();

        var second = new ClaimsIdentity();
        second.AddClaim(new Claim(ClaimTypes.Role, "r"));

        var properties = new AuthenticationProperties { RedirectUri = "/return?x=ü", IsPersistent = true };
        properties.Items["custom"] = new string('z', 500);
        properties.ExpiresUtc = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

        return new AuthenticationTicket(new ClaimsPrincipal(new[] { identity, second }), properties, "Scheme");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(500)]
    public void Serialize_ProducesSameBytesAsBinaryWriter(int claimCount)
    {
        var ticket = CreateComplexTicket(claimCount);

        var expected = SerializeWithBinaryWriter(ticket);

        Assert.Equal(expected, TicketSerializer.Default.Serialize(ticket));
        Assert.Equal(expected, new ReferenceTicketSerializer().Serialize(ticket));
    }

    [Fact]
    public void Serialize_EmptyPrincipal_ProducesSameBytesAsBinaryWriter()
    {
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(), new AuthenticationProperties(), "Scheme");

        Assert.Equal(SerializeWithBinaryWriter(ticket), TicketSerializer.Default.Serialize(ticket));
    }

    [Fact]
    public void Serialize_RoundTrips()
    {
        var ticket = CreateComplexTicket(10);

        var result = TicketSerializer.Default.Deserialize(TicketSerializer.Default.Serialize(ticket));

        Assert.NotNull(result);
        Assert.Equal("Scheme", result.AuthenticationScheme);
        Assert.Equal(2, result.Principal.Identities.Count());
        var identity = result.Principal.Identities.First();
        Assert.Equal(ticket.Principal.Identities.First().Claims.Select(c => (c.Type, c.Value, c.ValueType, c.Issuer, c.OriginalIssuer)),
            identity.Claims.Select(c => (c.Type, c.Value, c.ValueType, c.Issuer, c.OriginalIssuer)));
        Assert.Equal("bootstrap-token", identity.BootstrapContext);
        Assert.Equal("actor-claim", identity.Actor!.Claims.Single().Type);
        Assert.Equal(ticket.Properties.Items, result.Properties.Items);
    }

    [Fact]
    public void Serialize_NullTicket_Throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => TicketSerializer.Default.Serialize(null!));
        Assert.Equal("ticket", ex.ParamName);
    }

    [Fact]
    public void Serialize_InvalidSurrogate_ProducesSameBytesAsBinaryWriter()
    {
        var identity = new ClaimsIdentity("auth");
        identity.AddClaim(new Claim("bad", "\uD800"));
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), "Scheme");

        Assert.Equal(new ReferenceTicketSerializer().Serialize(ticket), TicketSerializer.Default.Serialize(ticket));
    }

    [Fact]
    public void SerializeProperties_ProducesSameBytesAsBinaryWriter()
    {
        var properties = new AuthenticationProperties { RedirectUri = "/ü", IsPersistent = true };
        properties.Items["key"] = new string('k', 1000);
        properties.Items["null"] = null;
        properties.Items[".xsrf"] = "abc";

        Assert.Equal(new ReferencePropertiesSerializer().Serialize(properties), PropertiesSerializer.Default.Serialize(properties));
        Assert.Equal(new ReferencePropertiesSerializer().Serialize(new AuthenticationProperties()), PropertiesSerializer.Default.Serialize(new AuthenticationProperties()));
    }

    [Fact]
    public void SerializeProperties_NullProperties_Throws()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => PropertiesSerializer.Default.Serialize(null!));
        Assert.Equal("properties", ex.ParamName);
    }
}
