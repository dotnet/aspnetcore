// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Identity;

internal sealed class HttpPasskeyRequestContextProvider(IHttpContextAccessor httpContextAccessor, IOptions<IdentityOptions> options) : IPasskeyRequestContextProvider
{
    private PasskeyRequestContext? _context;

    public PasskeyRequestContext Context => _context ??= GetPasskeyRequestContext();

    private PasskeyRequestContext GetPasskeyRequestContext()
    {
        var passkeyOptions = options.Value.Passkey;
        var httpContext = httpContextAccessor.HttpContext;
        return new()
        {
            Domain = passkeyOptions.ServerDomain ?? httpContext?.Request.Host.Host,
            Origin = httpContext?.Request.Headers.Origin,
        };
    }
}
