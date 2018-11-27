// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// An <see cref="ObjectResult"/> that when executed will produce a Not Found (404) response.
    /// </summary>
    public class NotFoundObjectResult : ObjectResult
    {
        /// <summary>
        /// Creates a new <see cref="NotFoundObjectResult"/> instance.
        /// </summary>
        /// <param name="value">The value to format in the entity body.</param>
        public NotFoundObjectResult(object value)
            : base(value)
        {
            StatusCode = StatusCodes.Status404NotFound;
        }
    }
}