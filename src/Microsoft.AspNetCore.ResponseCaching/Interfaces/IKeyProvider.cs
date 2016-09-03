// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.ResponseCaching
{
    public interface IKeyProvider
    {
        /// <summary>
        /// Create a key using the HTTP request.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
        /// <returns>The created base key.</returns>
        string CreateBaseKey(HttpContext httpContext);

        /// <summary>
        /// Create a key using the HTTP context and vary rules.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
        /// <param name="varyRules">The <see cref="VaryRules"/>.</param>
        /// <returns>The created base key.</returns>
        string CreateVaryKey(HttpContext httpContext, VaryRules varyRules);
    }
}
