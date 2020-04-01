// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Represents an <see cref="UnauthorizedResult"/> that when
    /// executed will produce an Unauthorized (401) response.
    /// </summary>
    [DefaultStatusCode(DefaultStatusCode)]
    public class UnauthorizedResult : StatusCodeResult
    {
        private const int DefaultStatusCode = StatusCodes.Status401Unauthorized;

        /// <summary>
        /// Creates a new <see cref="UnauthorizedResult"/> instance.
        /// </summary>
        public UnauthorizedResult() : base(DefaultStatusCode)
        {
        }
    }
}