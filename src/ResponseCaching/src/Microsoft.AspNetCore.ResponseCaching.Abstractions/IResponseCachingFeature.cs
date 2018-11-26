// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.ResponseCaching
{
    /// <summary>
    /// A feature for configuring additional response cache options on the HTTP response.
    /// </summary>
    public interface IResponseCachingFeature
    {
        /// <summary>
        /// Gets or sets the query keys used by the response cache middleware for calculating secondary vary keys.
        /// </summary>
        string[] VaryByQueryKeys { get; set; }
    }
}
