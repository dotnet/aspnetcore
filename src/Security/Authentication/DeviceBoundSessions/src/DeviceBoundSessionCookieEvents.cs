// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

/// <summary>
/// Cookie authentication events that emit the <c>Secure-Session-Registration</c> header on sign-in.
/// Wire this into the source cookie scheme to trigger DBSC registration.
/// </summary>
public class DeviceBoundSessionCookieEvents : CookieAuthenticationEvents
{
    private readonly DeviceBoundSessionOptions _dbscOptions;

    /// <summary>
    /// Initializes a new instance of <see cref="DeviceBoundSessionCookieEvents"/>.
    /// </summary>
    /// <param name="dbscOptions">The DBSC options.</param>
    public DeviceBoundSessionCookieEvents(DeviceBoundSessionOptions dbscOptions)
    {
        _dbscOptions = dbscOptions;
    }

    /// <inheritdoc/>
    public override Task SigningIn(CookieSigningInContext context)
    {
        EmitRegistrationHeader(context.HttpContext);
        return base.SigningIn(context);
    }

    private void EmitRegistrationHeader(HttpContext httpContext)
    {
        var dataProtectionProvider = httpContext.RequestServices.GetRequiredService<IDataProtectionProvider>();
        var protector = dataProtectionProvider.CreateProtector("Microsoft.AspNetCore.Authentication.DeviceBoundSessions.Challenge.v1");

        var challenge = protector.Protect($"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}|{Guid.NewGuid()}|registration");

        var headerValue = $"(ES256 RS256);path=\"{_dbscOptions.RegistrationPath.Value}\";challenge=\"{challenge}\"";
        httpContext.Response.Headers.Append("Secure-Session-Registration", headerValue);
    }
}
