// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Represents an <see cref="UnauthorizedResult"/> that when
    /// executed will produce an Unauthorized (401) response.
    /// </summary>
    public class UnauthorizedResult : StatusCodeResult
    {
        /// <summary>
        /// Creates a new <see cref="UnauthorizedResult"/> instance.
        /// </summary>
        public UnauthorizedResult() : base(StatusCodes.Status401Unauthorized)
        {
        }
    }
}