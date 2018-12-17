// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Authentication.WsFederation
{
    /// <summary>
    /// Default values related to WsFederation authentication handler
    /// </summary>
    public static class WsFederationDefaults
    {
        /// <summary>
        /// The default authentication type used when registering the WsFederationHandler.
        /// </summary>
        public const string AuthenticationScheme = "WsFederation";
        
        /// <summary>
        /// The default display name used when registering the WsFederationHandler.
        /// </summary>
        public const string DisplayName = "WsFederation";

        /// <summary>
        /// Constant used to identify userstate inside AuthenticationProperties that have been serialized in the 'wctx' parameter.
        /// </summary>
        public static readonly string UserstatePropertiesKey = "WsFederation.Userstate";
    }
}
