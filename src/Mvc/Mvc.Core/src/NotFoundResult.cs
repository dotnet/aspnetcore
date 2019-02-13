// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Represents an <see cref="StatusCodeResult"/> that when
    /// executed will produce a Not Found (404) response.
    /// </summary>
    public class NotFoundResult : StatusCodeResult
    {
        /// <summary>
        /// Creates a new <see cref="NotFoundResult"/> instance.
        /// </summary>
        public NotFoundResult() : base(StatusCodes.Status404NotFound)
        {
        }
    }
}