// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Authentication.Cookies
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
