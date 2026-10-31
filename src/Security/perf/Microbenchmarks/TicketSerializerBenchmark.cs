// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Authentication;

namespace Microsoft.AspNetCore.Security;

[MemoryDiagnoser]
public class TicketSerializerBenchmark
{
    private AuthenticationTicket _ticket;
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

        var properties = new AuthenticationProperties
        {
            IssuedUtc = new DateTimeOffset(2026, 10, 31, 0, 0, 0, TimeSpan.Zero),
            ExpiresUtc = new DateTimeOffset(2026, 11, 14, 0, 0, 0, TimeSpan.Zero),
            IsPersistent = true,
        };

        _ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), properties, "Identity.Application");
        _serializedTicket = TicketSerializer.Default.Serialize(_ticket);
        _serializedProperties = PropertiesSerializer.Default.Serialize(properties);
    }

    [Benchmark]
    public AuthenticationTicket DeserializeTicket() => TicketSerializer.Default.Deserialize(_serializedTicket);

    [Benchmark]
    public byte[] SerializeTicket() => TicketSerializer.Default.Serialize(_ticket);

    [Benchmark]
    public AuthenticationProperties DeserializeProperties() => PropertiesSerializer.Default.Deserialize(_serializedProperties);
}
