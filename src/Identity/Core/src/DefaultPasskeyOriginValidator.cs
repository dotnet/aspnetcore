// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Identity;

internal class DefaultPasskeyOriginValidator : IPasskeyOriginValidator
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly PasskeyOptions _options;

    public DefaultPasskeyOriginValidator(
        IHttpContextAccessor httpContextAccessor,
        IOptions<IdentityOptions> options)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        ArgumentNullException.ThrowIfNull(options);

        _httpContextAccessor = httpContextAccessor;
        _options = options.Value.Passkey;
    }

    public bool IsValidOrigin(PasskeyOriginInfo originInfo)
    {
        if (string.IsNullOrEmpty(originInfo.Origin))
        {
            return false;
        }

        if (originInfo.CrossOrigin == true && !_options.AllowCrossOriginIframes)
        {
            return false;
        }

        try
        {
            var originUri = new Uri(originInfo.Origin);

            if (_options.AllowedOrigins.Count > 0)
            {
                foreach (var allowedOrigin in _options.AllowedOrigins)
                {
                    // Uri.Equals correctly handles string comparands.
                    if (originUri.Equals(allowedOrigin))
                    {
                        return true;
                    }
                }
            }

            if (_options.AllowCurrentOrigin && _httpContextAccessor.HttpContext?.Request.Headers.Origin is [var origin])
            {
                // Uri.Equals correctly handles string comparands.
                if (originUri.Equals(origin))
                {
                    return true;
                }
            }

            return false;
        }
        catch (UriFormatException)
        {
            return false;
        }
    }
}
