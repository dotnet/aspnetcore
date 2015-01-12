// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Security.OAuthBearer
{
    /// <summary>
    /// Default values used by authorization server and bearer authentication.
    /// </summary>
    public static class OAuthBearerAuthenticationDefaults
    {
        /// <summary>
        /// Default value for AuthenticationType property in the OAuthBearerAuthenticationOptions and
        /// OAuthAuthorizationServerOptions.
        /// </summary>
        public const string AuthenticationType = "Bearer";
    }
}
