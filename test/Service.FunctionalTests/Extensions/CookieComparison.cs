// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.Testing
{
    [Flags]
    public enum CookieComparison
    {
        NameStartsWith = 1,
        NameEquals = 2,
        DomainEquals = 4,
        PathEquals = 8,
        ExpiresEquals = 16,
        MaxAgeEquals = 32,
        HttpOnly = 64,
        SameSite = 128,
        Secure = 256,
        ValueStartsWith = 512,
        Default = NameStartsWith | DomainEquals | PathEquals | ExpiresEquals | MaxAgeEquals | HttpOnly | SameSite | Secure | ValueStartsWith,
        ValueEquals = 1024,
        StrictNoTime = NameEquals | DomainEquals | PathEquals | HttpOnly | SameSite | Secure | ValueEquals,
        Strict = NameEquals | DomainEquals | PathEquals | ExpiresEquals | MaxAgeEquals | HttpOnly | SameSite | Secure | ValueEquals,
        Delete = NameStartsWith | DomainEquals | PathEquals | ExpiresEquals | MaxAgeEquals,
        DeleteStrict = NameEquals | DomainEquals | PathEquals | ExpiresEquals | MaxAgeEquals,
    }
}
