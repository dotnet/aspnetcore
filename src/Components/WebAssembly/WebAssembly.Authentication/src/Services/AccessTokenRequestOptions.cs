// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication
{
    /// <summary>
    /// Represents the options for provisioning an access token on behalf of a user.
    /// </summary>
    public class AccessTokenRequestOptions
    {
        /// <summary>
        /// Gets or sets the list of scopes to request for the token.
        /// </summary>
        public IEnumerable<string> Scopes { get; set; }

        /// <summary>
        /// Gets or sets a specific return url to use for returning the user back to the application if it needs to be
        /// redirected elsewhere in order to provision the token.
        /// </summary>
        public string ReturnUrl { get; set; }
    }
}
