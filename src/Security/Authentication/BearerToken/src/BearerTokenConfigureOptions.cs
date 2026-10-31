// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.BearerToken;

internal sealed class BearerTokenConfigureOptions(IDataProtectionProvider dp) : IConfigureNamedOptions<BearerTokenOptions>
{
    private const string _primaryPurpose = "Microsoft.AspNetCore.Authentication.BearerToken";

    public void Configure(string? schemeName, BearerTokenOptions options)
    {
        if (schemeName is null)
        {
            return;
        }

        options.BearerTokenProtector = new TicketDataFormat(dp.CreateProtector(_primaryPurpose, schemeName, "BearerToken"));
        options.RefreshTokenProtector = new TicketDataFormat(dp.CreateProtector(_primaryPurpose, schemeName, "RefreshToken"));
    }

    public void Configure(BearerTokenOptions options)
    {
        throw new NotImplementedException();
    }
}
