// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Accessor to the <see cref="HttpContext"/>
    /// </summary>
    public interface IHttpContextAccessor
    {
        /// <summary>
        /// Gets or sets the associated <see cref="HttpContext"/>.
        /// </summary>
        HttpContext HttpContext { get; set; }
    }
}