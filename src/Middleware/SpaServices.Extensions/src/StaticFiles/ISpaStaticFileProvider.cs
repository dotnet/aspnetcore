// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.SpaServices.StaticFiles
{
    /// <summary>
    /// Represents a service that can provide static files to be served for a Single Page
    /// Application (SPA).
    /// </summary>
    public interface ISpaStaticFileProvider
    {
        /// <summary>
        /// Gets the file provider, if available, that supplies the static files for the SPA.
        /// The value is <c>null</c> if no file provider is available.
        /// </summary>
        IFileProvider FileProvider { get; }
    }
}
