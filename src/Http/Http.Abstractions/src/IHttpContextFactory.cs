// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Provides methods to create and dispose of <see cref="HttpContext"/> instances.
    /// </summary>
    public interface IHttpContextFactory
    {
        /// <summary>
        /// Creates an <see cref="HttpContext"/> instance for the specified set of HTTP features.
        /// </summary>
        /// <param name="featureCollection">The collection of HTTP features to set on the created instance.</param>
        /// <returns>The <see cref="HttpContext"/> instance.</returns>
        HttpContext Create(IFeatureCollection featureCollection);

        /// <summary>
        /// Releases resources held by the <see cref="HttpContext"/>.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/> to dispose.</param>
        void Dispose(HttpContext httpContext);
    }
}
