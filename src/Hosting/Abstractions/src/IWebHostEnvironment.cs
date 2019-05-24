// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Hosting
{
    /// <summary>
    /// Provides information about the web hosting environment an application is running in.
    /// </summary>
    public interface IWebHostEnvironment : IHostEnvironment
    {
        /// <summary>
        /// Gets or sets the absolute path to the directory that contains the web-servable application content files.
        /// </summary>
        string WebRootPath { get; set; }

        /// <summary>
        /// Gets or sets an <see cref="IFileProvider"/> pointing at <see cref="WebRootPath"/>.
        /// </summary>
        IFileProvider WebRootFileProvider { get; set; }
    }
}
