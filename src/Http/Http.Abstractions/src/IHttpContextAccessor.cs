// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Provides access to the current <see cref="HttpContext"/>, if one is available.
    /// </summary>
    /// <remarks>
    /// This interface should be used with caution. It relies on <see cref="System.Threading.AsyncLocal{T}" /> which can have a negative performance impact on async calls.
    /// It also creates a dependency on "ambient state" which can make testing more difficult.
    /// </remarks>
    public interface IHttpContextAccessor
    {
        /// <summary>
        /// Gets or sets the current <see cref="HttpContext"/>. Returns <see langword="null" /> if there is no active <see cref="HttpContext" />.
        /// </summary>
        HttpContext HttpContext { get; set; }
    }
}
