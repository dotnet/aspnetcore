// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.CookiePolicy
{
    public class CookiePolicyOptions
    {
        public HttpOnlyPolicy HttpOnly { get; set; } = HttpOnlyPolicy.None;
        public SecurePolicy Secure { get; set; } = SecurePolicy.None;

        public Action<AppendCookieContext> OnAppendCookie { get; set; }
        public Action<DeleteCookieContext> OnDeleteCookie { get; set; }
    }
}