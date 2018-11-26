// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.SpaServices
{
    /// <summary>
    /// Defines a class that provides mechanisms for configuring the hosting
    /// of a Single Page Application (SPA) and attaching middleware.
    /// </summary>
    public interface ISpaBuilder
    {
        /// <summary>
        /// The <see cref="IApplicationBuilder"/> representing the middleware pipeline
        /// in which the SPA is being hosted.
        /// </summary>
        IApplicationBuilder ApplicationBuilder { get; }

        /// <summary>
        /// Describes configuration options for hosting a SPA.
        /// </summary>
        SpaOptions Options { get; }
    }
}
