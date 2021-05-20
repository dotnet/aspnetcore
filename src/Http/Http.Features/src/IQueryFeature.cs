// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Http.Features
{
    /// <summary>
    /// Provides access to the <see cref="IQueryCollection"/> associated with the HTTP request.
    /// </summary>
    public interface IQueryFeature
    {
        /// <summary>
        /// Gets or sets the <see cref="IQueryCollection"/>.
        /// </summary>
        IQueryCollection Query { get; set; }
    }
}
