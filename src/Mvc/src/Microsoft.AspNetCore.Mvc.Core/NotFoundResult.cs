// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Represents an <see cref="StatusCodeResult"/> that when
    /// executed will produce a Not Found (404) response.
    /// </summary>
    [DefaultStatusCode(DefaultStatusCode)]
    public class NotFoundResult : StatusCodeResult
    {
        private const int DefaultStatusCode = StatusCodes.Status404NotFound;

        /// <summary>
        /// Creates a new <see cref="NotFoundResult"/> instance.
        /// </summary>
        public NotFoundResult() : base(DefaultStatusCode)
        {
        }
    }
}