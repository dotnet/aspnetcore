// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging.AzureAppServices
{
    /// <summary>
    /// Represents the default implementation of <see cref="IWebAppContext"/>.
    /// </summary>
    internal class WebAppContext : IWebAppContext
    {
        /// <summary>
        /// Gets the default instance of the WebApp context.
        /// </summary>
        public static WebAppContext Default { get; } = new WebAppContext();

        private WebAppContext() { }

        /// <inheritdoc />
        public string HomeFolder { get; } = Environment.GetEnvironmentVariable("HOME");

        /// <inheritdoc />
        public string SiteName { get; } = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME");

        /// <inheritdoc />
        public string SiteInstanceId { get; } = Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID");

        /// <inheritdoc />
        public bool IsRunningInAzureWebApp => !string.IsNullOrEmpty(HomeFolder) &&
                                              !string.IsNullOrEmpty(SiteName);
    }
}
