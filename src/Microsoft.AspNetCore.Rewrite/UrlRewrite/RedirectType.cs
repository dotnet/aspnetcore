// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Rewrite.UrlRewrite
{
    public enum RedirectType
    {
        Permanent = 301,
        Found = 302,
        SeeOther = 303,
        Temporary = 307
    }
}
