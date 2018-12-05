// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer
{
    /// <summary>
    /// Constants for a default API authentication handler.
    /// </summary>
    public class IdentityServerJwtConstants
    {
        /// <summary>
        /// Scheme used for the default API policy authentication scheme.
        /// </summary>
        public const string IdentityServerJwtScheme = "IdentityServerJwt";

        /// <summary>
        /// Scheme used for the underlying default API JwtBearer authentication scheme.
        /// </summary>
        public const string IdentityServerJwtBearerScheme = "IdentityServerJwtBearer";
    }
}
