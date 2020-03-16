// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// An <see cref="ObjectResult"/> that when executed will produce a <see cref="StatusCodes.Status206PartialContent"/> response.
    /// </summary>
    [DefaultStatusCode(DefaultStatusCode)]
    public class PartialContentObjectResult : ObjectResult
    {
        private const int DefaultStatusCode = StatusCodes.Status206PartialContent;

        /// <summary>
        /// Creates a new <see cref="PartialContentObjectResult"/> instance.
        /// </summary>
        /// <param name="value">The value to format in the entity body.</param>
        public PartialContentObjectResult([ActionResultObjectValue]object value)
            : base(value)
        {
            StatusCode = DefaultStatusCode;
        }
    }
}