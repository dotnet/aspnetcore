// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Antiforgery.Internal
{
    /// <summary>
    /// Used to hold per-request state.
    /// </summary>
    public class AntiforgeryFeature : IAntiforgeryFeature
    {
        public bool HaveDeserializedCookieToken { get; set; }

        public AntiforgeryToken CookieToken { get; set; }

        public bool HaveDeserializedRequestToken { get; set; }

        public AntiforgeryToken RequestToken { get; set; }

        public bool HaveGeneratedNewCookieToken { get; set; }

        // After HaveGeneratedNewCookieToken is true, remains null if CookieToken is valid.
        public AntiforgeryToken NewCookieToken { get; set; }

        // After HaveGeneratedNewCookieToken is true, remains null if CookieToken is valid.
        public string NewCookieTokenString { get; set; }

        public AntiforgeryToken NewRequestToken { get; set; }

        public string NewRequestTokenString { get; set; }

        // Always false if NewCookieToken is null. Never store null cookie token or re-store cookie token from request.
        public bool HaveStoredNewCookieToken { get; set; }
    }
}