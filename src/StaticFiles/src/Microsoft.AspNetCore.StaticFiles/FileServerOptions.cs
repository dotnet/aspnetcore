// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.StaticFiles.Infrastructure;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Options for all of the static file middleware components
    /// </summary>
    public class FileServerOptions : SharedOptionsBase
    {
        /// <summary>
        /// Creates a combined options class for all of the static file middleware components.
        /// </summary>
        public FileServerOptions()
            : base(new SharedOptions())
        {
            StaticFileOptions = new StaticFileOptions(SharedOptions);
            DirectoryBrowserOptions = new DirectoryBrowserOptions(SharedOptions);
            DefaultFilesOptions = new DefaultFilesOptions(SharedOptions);
            EnableDefaultFiles = true;
        }

        /// <summary>
        /// Options for configuring the StaticFileMiddleware.
        /// </summary>
        public StaticFileOptions StaticFileOptions { get; private set; }

        /// <summary>
        /// Options for configuring the DirectoryBrowserMiddleware.
        /// </summary>
        public DirectoryBrowserOptions DirectoryBrowserOptions { get; private set; }

        /// <summary>
        /// Options for configuring the DefaultFilesMiddleware.
        /// </summary>
        public DefaultFilesOptions DefaultFilesOptions { get; private set; }

        /// <summary>
        /// Directory browsing is disabled by default.
        /// </summary>
        public bool EnableDirectoryBrowsing { get; set; }

        /// <summary>
        /// Default files are enabled by default.
        /// </summary>
        public bool EnableDefaultFiles { get; set; }
    }
}
