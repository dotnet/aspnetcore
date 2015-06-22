// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNet.DataProtection;
using Microsoft.AspNet.WebUtilities;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Antiforgery
{
    public class AntiforgeryOptionsSetup : ConfigureOptions<AntiforgeryOptions>
    {
        public AntiforgeryOptionsSetup(IOptions<DataProtectionOptions> dataProtectionOptionsAccessor)
            : base((options) => ConfigureOptions(options, dataProtectionOptionsAccessor.Options))
        {
            // We want this to run after any user setups to compute a default name if needed.
            Order = 10000;
        }

        public static void ConfigureOptions(AntiforgeryOptions options, DataProtectionOptions dataProtectionOptions)
        {
            if (options.CookieName == null)
            {
                var applicationId = dataProtectionOptions.ApplicationDiscriminator ?? string.Empty;
                options.CookieName = ComputeCookieName(applicationId);
            }
        }

        private static string ComputeCookieName(string applicationId)
        {
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(applicationId));
                var subHash = hash.Take(8).ToArray();
                return WebEncoders.Base64UrlEncode(subHash);
            }
        }
    }
}
