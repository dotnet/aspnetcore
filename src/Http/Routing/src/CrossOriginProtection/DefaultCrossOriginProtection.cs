// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing;

internal sealed class DefaultCrossOriginProtection : ICrossOriginProtection
{
    private readonly string[] _trustedOrigins;

    public DefaultCrossOriginProtection(IOptions<CrossOriginProtectionOptions> options)
    {
        // Snapshot into immutable array to avoid observing later mutations.
        _trustedOrigins = options.Value.TrustedOrigins.ToArray();
    }

    public CrossOriginAntiforgeryResult Validate(HttpContext context)
    {
        return CrossOriginRequestValidator.IsRequestAllowed(context, _trustedOrigins)
            ? CrossOriginAntiforgeryResult.Allowed
            : CrossOriginAntiforgeryResult.Denied;
    }
}
