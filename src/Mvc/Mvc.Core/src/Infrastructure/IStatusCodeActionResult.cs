// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    /// <summary>
    /// Represents an <see cref="IActionResult"/> that when executed will
    /// produce an HTTP response with the specified <see cref="StatusCode"/>.
    /// </summary>
    public interface IStatusCodeActionResult : IActionResult
    {
        /// <summary>
        /// Gets or sets the HTTP status code.
        /// </summary>
        int? StatusCode { get; }
    }
}
