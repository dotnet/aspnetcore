// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.Cookies
{
    public class BaseCookieContext : BaseAuthenticationContext
    {
        public BaseCookieContext(
            HttpContext context,
            AuthenticationScheme scheme,
            CookieAuthenticationOptions options,
            AuthenticationProperties properties)
            : base(context, scheme.Name, properties)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            Options = options;
        }

        public CookieAuthenticationOptions Options { get; }

        public AuthenticationScheme Scheme { get; }
    }
}
