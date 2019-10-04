// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Provides methods to create <see cref="HttpContext"/>
    /// </summary>
    public interface IHttpContextFactory
    {
        /// <summary>
        /// Creates a <see cref="HttpContext"/> instance for each request.
        /// </summary>
        /// <param name="featureCollection">The collection of HTTP features to set on the created instance.</param>
        /// <returns>The <see cref="HttpContext"/> instance.</returns>
        HttpContext Create(IFeatureCollection featureCollection);

        /// <summary>
        /// Releases the unmanaged resources and disposes of the managed resources used by the <see cref="HttpContext"/>.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/> to dispose.</param>
        void Dispose(HttpContext httpContext);
    }
}
