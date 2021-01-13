// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Antiforgery
{
    internal class AntiforgeryOptionsSetup : IConfigureOptions<AntiforgeryOptions>
    {
        private readonly DataProtectionOptions _dataProtectionOptions;

        public AntiforgeryOptionsSetup(IOptions<DataProtectionOptions> dataProtectionOptions)
        {
            _dataProtectionOptions = dataProtectionOptions.Value;
        }

        public void Configure(AntiforgeryOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.Cookie.Name == null)
            {
                var applicationId = _dataProtectionOptions.ApplicationDiscriminator ?? string.Empty;
                options.Cookie.Name = AntiforgeryOptions.DefaultCookiePrefix + ComputeCookieName(applicationId);
            }
        }

        private static string ComputeCookieName(string applicationId)
        {
            using (var sha256 = CryptographyAlgorithms.CreateSHA256())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(applicationId));
                var subHash = hash.Take(8).ToArray();
                return WebEncoders.Base64UrlEncode(subHash);
            }
        }
    }
}
