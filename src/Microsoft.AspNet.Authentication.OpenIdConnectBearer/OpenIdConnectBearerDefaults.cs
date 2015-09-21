// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Authentication.OpenIdConnectBearer
{
    /// <summary>
    /// Default values used by authorization server and bearer authentication.
    /// </summary>
    public static class OpenIdConnectBearerDefaults
    {
        /// <summary>
        /// Default value for AuthenticationScheme property in the OpenIdConnectBearerAuthenticationOptions and
        /// OpenIdConnectAuthorizationServerOptions.
        /// </summary>
        public const string AuthenticationScheme = "Bearer";
    }
}
