// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// A <see cref="StatusCodeResult"/> that when executed will produce a 204 No Content response.
    /// </summary>
    [DefaultStatusCode(DefaultStatusCode)]
    public class NoContentResult : StatusCodeResult
    {
        private const int DefaultStatusCode = StatusCodes.Status204NoContent;

        /// <summary>
        /// Initializes a new <see cref="NoContentResult"/> instance.
        /// </summary>
        public NoContentResult()
            : base(DefaultStatusCode)
        {
        }
    }
}
