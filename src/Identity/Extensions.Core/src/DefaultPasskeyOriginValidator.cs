// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// The default passkey origin validator.
/// </summary>
public sealed class DefaultPasskeyOriginValidator : IPasskeyOriginValidator
{
    private readonly IPasskeyRequestContextProvider _requestContextProvider;
    private readonly PasskeyOptions _options;

    /// <summary>
    /// Constructs a new <see cref="DefaultPasskeyOriginValidator"/>.
    /// </summary>
    public DefaultPasskeyOriginValidator(
        IPasskeyRequestContextProvider requestContextProvider,
        IOptions<IdentityOptions> options)
    {
        _requestContextProvider = requestContextProvider;
        _options = options.Value.Passkey;
    }

    /// <inheritdoc/>
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

            if (_options.AllowCurrentOrigin)
            {
                var context = _requestContextProvider.Context;

                // Uri.Equals correctly handles string comparands.
                if (originUri.Equals(context.Origin))
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
