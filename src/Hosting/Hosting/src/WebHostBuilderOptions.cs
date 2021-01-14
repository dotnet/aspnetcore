// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Builder options for use with ConfigureWebHost.
    /// </summary>
    public class WebHostBuilderOptions
    {
        /// <summary>
        /// Indicates if "ASPNETCORE_" prefixed environment variables should be added to configuration.
        /// They are added by default.
        /// </summary>
        public bool SuppressEnvironmentConfiguration { get; set; } = false;
    }
}
