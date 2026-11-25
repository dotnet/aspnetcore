// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Authentication;

namespace Microsoft.AspNetCore.Security;

[MemoryDiagnoser]
public class TicketSerializerBenchmark
{
    private readonly TicketSerializer _derivedTicketSerializer = new DerivedTicketSerializer();
    private AuthenticationTicket _ticket;
    private AuthenticationProperties _properties;
    private byte[] _serializedTicket;
    private byte[] _serializedProperties;

    [GlobalSetup]
    public void GlobalSetup()
    {
        // Shape of a typical ASP.NET Core Identity application cookie.
        var identity = new ClaimsIdentity("Identity.Application");
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "6f1b6c2e-43c5-4d8f-9a55-3b7f0c1f2a9d"));
        identity.AddClaim(new Claim(ClaimTypes.Name, "someone@example.com"));
        identity.AddClaim(new Claim(ClaimTypes.Email, "someone@example.com"));
        identity.AddClaim(new Claim("AspNet.Identity.SecurityStamp", "QWERTYUIOPASDFGHJKLZXCVBNM234567"));
        identity.AddClaim(new Claim(ClaimTypes.Role, "Admin"));
        identity.AddClaim(new Claim(ClaimTypes.Role, "User"));
        identity.AddClaim(new Claim("amr", "pwd"));

        _properties = new AuthenticationProperties
        {
            IssuedUtc = new DateTimeOffset(2026, 10, 31, 0, 0, 0, TimeSpan.Zero),
            ExpiresUtc = new DateTimeOffset(2026, 11, 14, 0, 0, 0, TimeSpan.Zero),
            IsPersistent = true,
        };

        _ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), _properties, "Identity.Application");
        _serializedTicket = TicketSerializer.Default.Serialize(_ticket);
        _serializedProperties = PropertiesSerializer.Default.Serialize(_properties);
    }

    [Benchmark]
    public byte[] SerializeTicket() => TicketSerializer.Default.Serialize(_ticket);

    [Benchmark]
    public AuthenticationTicket DeserializeTicket() => TicketSerializer.Default.Deserialize(_serializedTicket);

    [Benchmark]
    public byte[] SerializeProperties() => PropertiesSerializer.Default.Serialize(_properties);

    [Benchmark]
    public AuthenticationProperties DeserializeProperties() => PropertiesSerializer.Default.Deserialize(_serializedProperties);

    // Derived serializers use the BinaryWriter/BinaryReader-based virtual methods.
    [Benchmark]
    public byte[] SerializeTicketDerived() => _derivedTicketSerializer.Serialize(_ticket);

    [Benchmark]
    public AuthenticationTicket DeserializeTicketDerived() => _derivedTicketSerializer.Deserialize(_serializedTicket);

    private sealed class DerivedTicketSerializer : TicketSerializer
    {
    }
}
