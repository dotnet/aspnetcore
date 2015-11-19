// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.FileProviders;
using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNet.Hosting
{
    /// <summary>
    /// Provides information about the web hosting environment an application is running in.
    /// </summary>
    public interface IHostingEnvironment
    {
        /// <summary>
        /// Gets or sets the name of the environment. This property is automatically set by the host to the value
        /// of the "Hosting:Environment" (on Windows) or "Hosting__Environment" (on Linux &amp; OS X) environment variable.
        /// </summary>
        // This must be settable!
        string EnvironmentName { get; set;  }

        /// <summary>
        /// Gets or sets the absolute path to the directory that contains the web-servable application content files.
        /// </summary>
        // This must be settable!
        string WebRootPath { get; set; }

        /// <summary>
        /// Gets or sets an <see cref="IFileProvider"/> pointing at <see cref="WebRootPath"/>.
        /// </summary>
        // This must be settable!
        IFileProvider WebRootFileProvider { get; set; }

        /// <summary>
        /// Gets or sets the configuration object used by hosting environment.
        /// </summary>
        IConfiguration Configuration { get; set; }
    }
}