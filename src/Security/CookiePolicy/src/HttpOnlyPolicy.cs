// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.CookiePolicy
{
    /// <summary>
    /// Describes the HttpOnly behavior for cookies.
    /// </summary>
    public enum HttpOnlyPolicy
    {
        /// <summary>
        /// The cookie does not have a configured HttpOnly behavior. This cookie can be accessed by
        /// JavaScript <c>document.cookie</c> API.
        /// </summary>
        None,

        /// <summary>
        /// The cookie is configured with a HttpOnly attribute. This cookie inaccessible to the
        /// JavaScript <c>document.cookie</c> API.
        /// </summary>
        Always
    }
}
