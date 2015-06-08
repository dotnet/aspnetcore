// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Antiforgery
{
    // Provides an abstraction around how tokens are persisted and retrieved for a request
    public interface IAntiforgeryTokenStore
    {
        AntiforgeryToken GetCookieToken(HttpContext httpContext);
        Task<AntiforgeryToken> GetFormTokenAsync(HttpContext httpContext);
        void SaveCookieToken(HttpContext httpContext, AntiforgeryToken token);
    }
}