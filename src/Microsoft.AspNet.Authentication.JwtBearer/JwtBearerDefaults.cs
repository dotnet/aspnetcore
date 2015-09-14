// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Authentication.JwtBearer
{
    /// <summary>
    /// Default values used by authorization server and bearer authentication.
    /// </summary>
    public static class JwtBearerDefaults
    {
        /// <summary>
        /// Default value for AuthenticationScheme property in the JwtBearerAuthenticationOptions and
        /// JwtAuthorizationServerOptions.
        /// </summary>
        public const string AuthenticationScheme = "Bearer";
    }
}
