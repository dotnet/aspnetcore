// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.CookiePolicy
{
    public class AppendCookieContext
    {
        public AppendCookieContext(HttpContext context, CookieOptions options, string name, string value)
        {
            Context = context;
            CookieOptions = options;
            CookieName = name;
            CookieValue = value;
        }

        public HttpContext Context { get; }
        public CookieOptions CookieOptions { get; }
        public string CookieName { get; set; }
        public string CookieValue { get; set; }
    }
}