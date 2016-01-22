// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.Cookies
{
    public class BaseCookieContext : BaseContext
    {
        public BaseCookieContext(
            HttpContext context,
            CookieAuthenticationOptions options)
            : base(context)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            Options = options;
        }

        public CookieAuthenticationOptions Options { get; }
    }
}
