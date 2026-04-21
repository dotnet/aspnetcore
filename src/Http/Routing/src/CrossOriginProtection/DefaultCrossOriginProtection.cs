// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing;

internal sealed class DefaultCrossOriginProtection : ICrossOriginProtection
{
    private readonly bool _enabled;
    private readonly string[] _trustedOrigins;

    public DefaultCrossOriginProtection(IOptions<CrossOriginProtectionOptions> options)
    {
        var opts = options.Value;
        _enabled = opts.Enabled;
        // Snapshot into immutable array to avoid observing later mutations.
        _trustedOrigins = opts.TrustedOrigins.ToArray();
    }

    public CrossOriginValidationResult Validate(HttpContext context)
    {
        if (!_enabled)
        {
            return CrossOriginValidationResult.Disabled;
        }

        return CrossOriginRequestValidator.IsRequestAllowed(context, _trustedOrigins)
            ? CrossOriginValidationResult.Allowed
            : CrossOriginValidationResult.Denied;
    }
}
