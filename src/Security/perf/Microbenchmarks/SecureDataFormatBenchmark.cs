// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;

namespace Microsoft.AspNetCore.Security;

[MemoryDiagnoser]
public class SecureDataFormatBenchmark
{
    private TicketDataFormat _format;
    private AuthenticationTicket _ticket;
    private string _protectedTicket;

    [Params(7, 100)]
    public int ClaimCount { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        var protector = new EphemeralDataProtectionProvider().CreateProtector("Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationMiddleware", "Cookies", "v2");
        _format = new TicketDataFormat(protector);

        var identity = new ClaimsIdentity("Identity.Application");
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "0f8fad5b-d9cb-469f-a165-70867728950e"));
        identity.AddClaim(new Claim(ClaimTypes.Name, "bob@contoso.com"));
        identity.AddClaim(new Claim("AspNet.Identity.SecurityStamp", "MUFAS2EV5RQ5ZGCCIUFQVCIPRUZQ5JWK"));
        for (var i = 3; i < ClaimCount; i++)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, "role-" + i));
        }

        var properties = new AuthenticationProperties
        {
            IssuedUtc = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            ExpiresUtc = new DateTimeOffset(2025, 1, 15, 0, 0, 0, TimeSpan.Zero),
            IsPersistent = true,
        };
        _ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), properties, "Identity.Application");
        _protectedTicket = _format.Protect(_ticket);

        if (_format.Unprotect(_protectedTicket) is null)
        {
            throw new InvalidOperationException("Round trip failed.");
        }
    }

    [Benchmark]
    public string Protect() => _format.Protect(_ticket);

    [Benchmark]
    public AuthenticationTicket Unprotect() => _format.Unprotect(_protectedTicket);
}
