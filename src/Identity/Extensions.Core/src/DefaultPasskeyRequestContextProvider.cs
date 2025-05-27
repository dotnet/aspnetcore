// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Identity;

internal sealed class DefaultPasskeyRequestContextProvider(IOptions<IdentityOptions> options) : IPasskeyRequestContextProvider
{
    private PasskeyRequestContext? _context;

    public PasskeyRequestContext Context => _context ??= GetPasskeyRequestContext();

    private PasskeyRequestContext GetPasskeyRequestContext()
    {
        var passkeyOptions = options.Value.Passkey;
        return new()
        {
            Domain = passkeyOptions.ServerDomain,
            Origin = null,
        };
    }
}
