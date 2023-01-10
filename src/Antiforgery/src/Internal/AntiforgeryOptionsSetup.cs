// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Antiforgery;

internal sealed class AntiforgeryOptionsSetup : IConfigureOptions<AntiforgeryOptions>
{
    private readonly DataProtectionOptions _dataProtectionOptions;

    public AntiforgeryOptionsSetup(IOptions<DataProtectionOptions> dataProtectionOptions)
    {
        _dataProtectionOptions = dataProtectionOptions.Value;
    }

    public void Configure(AntiforgeryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.Cookie.Name == null)
        {
            var applicationId = _dataProtectionOptions.ApplicationDiscriminator ?? string.Empty;
            options.Cookie.Name = AntiforgeryOptions.DefaultCookiePrefix + ComputeCookieName(applicationId);
        }
    }

    private static string ComputeCookieName(string applicationId)
    {
        byte[] fullHash = SHA256.HashData(Encoding.UTF8.GetBytes(applicationId));
        return WebEncoders.Base64UrlEncode(fullHash, 0, 8);
    }
}
