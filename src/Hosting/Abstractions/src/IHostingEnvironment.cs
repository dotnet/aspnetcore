// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Hosting
{
    /// <summary>
    /// Provides information about the web hosting environment an application is running in.
    /// </summary>
    public interface IHostingEnvironment
    {
        /// <summary>
        /// Gets or sets the name of the environment. The host automatically sets this property to the value
        /// of the "ASPNETCORE_ENVIRONMENT" environment variable, or "environment" as specified in any other configuration source.
        /// </summary>
        string EnvironmentName { get; set; }

        /// <summary>
        /// Gets or sets the name of the application. This property is automatically set by the host to the assembly containing
        /// the application entry point.
        /// </summary>
        string ApplicationName { get; set; }

        /// <summary>
        /// Gets or sets the absolute path to the directory that contains the web-servable application content files.
        /// </summary>
        string WebRootPath { get; set; }

        /// <summary>
        /// Gets or sets an <see cref="IFileProvider"/> pointing at <see cref="WebRootPath"/>.
        /// </summary>
        IFileProvider WebRootFileProvider { get; set; }

        /// <summary>
        /// Gets or sets the absolute path to the directory that contains the application content files.
        /// </summary>
        string ContentRootPath { get; set; }

        /// <summary>
        /// Gets or sets an <see cref="IFileProvider"/> pointing at <see cref="ContentRootPath"/>.
        /// </summary>
        IFileProvider ContentRootFileProvider { get; set; }
    }
}
