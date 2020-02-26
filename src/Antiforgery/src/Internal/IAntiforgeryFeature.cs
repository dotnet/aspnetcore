// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Antiforgery
{
    internal interface IAntiforgeryFeature
    {
        AntiforgeryToken CookieToken { get; set; }

        bool HaveDeserializedCookieToken { get; set; }

        bool HaveDeserializedRequestToken { get; set; }

        bool HaveGeneratedNewCookieToken { get; set; }

        bool HaveStoredNewCookieToken { get; set; }

        AntiforgeryToken NewCookieToken { get; set; }

        string NewCookieTokenString { get; set; }

        AntiforgeryToken NewRequestToken { get; set; }

        string NewRequestTokenString { get; set; }

        AntiforgeryToken RequestToken { get; set; }
    }
}
