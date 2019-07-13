// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// An <see cref="ObjectResult"/> that when executed will produce a Forbidden (403) response.
    /// </summary>
    [DefaultStatusCode(DefaultStatusCode)]
    public class ForbiddenObjectResult : ObjectResult
    {
        private const int DefaultStatusCode = StatusCodes.Status403Forbidden;

        /// <summary>
        /// Creates a new <see cref="ForbiddenObjectResult"/> instance.
        /// </summary>
        public ForbiddenObjectResult([ActionResultObjectValue] object value) : base(value)
        {
            StatusCode = DefaultStatusCode;
        }
    }
}