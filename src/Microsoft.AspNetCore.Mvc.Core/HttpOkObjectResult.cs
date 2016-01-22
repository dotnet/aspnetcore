// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// An <see cref="ObjectResult"/> that when executed performs content negotiation, formats the entity body, and
    /// will produce a <see cref="StatusCodes.Status200OK"/> response if negotiation and formatting succeed.
    /// </summary>
    public class HttpOkObjectResult : ObjectResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpOkObjectResult"/> class.
        /// </summary>
        /// <param name="value">The content to format into the entity body.</param>
        public HttpOkObjectResult(object value)
            : base(value)
        {
            StatusCode = StatusCodes.Status200OK;
        }
    }
}