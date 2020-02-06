// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication
{
    /// <summary>
    /// Represents options for applications relying on a server for configuration.
    /// </summary>
    public class ApiAuthorizationProviderOptions
    {
        /// <summary>
        /// Gets or sets the endpoint to call to retrieve the authentication settings for the application.
        /// </summary>
        public string ConfigurationEndpoint { get; set; }
    }
}
