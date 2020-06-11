// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// An <see cref="ObjectResult"/> that when executed will produce a Not Found (404) response.
    /// </summary>
    [DefaultStatusCode(DefaultStatusCode)]
    public class NotFoundObjectResult : ObjectResult
    {
        private const int DefaultStatusCode = StatusCodes.Status404NotFound;

        /// <summary>
        /// Creates a new <see cref="NotFoundObjectResult"/> instance.
        /// </summary>
        /// <param name="value">The value to format in the entity body.</param>
        public NotFoundObjectResult([ActionResultObjectValue] object value)
            : base(value)
        {
            StatusCode = DefaultStatusCode;
        }
    }
}