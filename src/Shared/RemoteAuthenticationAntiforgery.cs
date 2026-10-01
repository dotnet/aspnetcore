// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Antiforgery;

// Shared between Microsoft.AspNetCore.Authentication (RemoteAuthenticationHandler) and the remote handlers
// that own additional callback paths of their own (e.g. Microsoft.AspNetCore.Authentication.OpenIdConnect).
//
// A remote provider's callback is a cross-site request by protocol design: OpenID Connect
// response_mode=form_post and WS-Federation both deliver the response as a top-level form POST from the
// identity provider's origin. Cross-origin CSRF protection therefore records an invalid
// IAntiforgeryValidationFeature verdict for it, and the handler then fails on its very first action - reading
// the callback body - before any of its events can run, so an application has no way to opt out.
//
// These callbacks carry their own forgery protection: the state parameter round-trips a protected
// AuthenticationProperties payload whose correlation id must match the correlation cookie, which the handler
// validates. The verdict is therefore suppressed while the handler owns the request, and restored when the
// handler declines it so the rest of the pipeline still sees the original verdict.
internal static class RemoteAuthenticationAntiforgery
{
    public static async Task<bool> HandleWithoutAntiforgeryVerdictAsync(HttpContext context, Func<Task<bool>> handler)
    {
        var suppressedVerdict = context.Features.Get<IAntiforgeryValidationFeature>();
        if (suppressedVerdict is { IsValid: false })
        {
            context.Features.Set<IAntiforgeryValidationFeature?>(null);
        }

        var handled = false;
        try
        {
            handled = await handler();
            return handled;
        }
        finally
        {
            if (!handled && suppressedVerdict is { IsValid: false })
            {
                context.Features.Set(suppressedVerdict);
            }
        }
    }
}
