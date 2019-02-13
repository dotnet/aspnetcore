// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// A <see cref="StatusCodeResult"/> that when executed will produce a Conflict (409) response.
    /// </summary>
    public class ConflictResult : StatusCodeResult
    {
        /// <summary>
        /// Creates a new <see cref="ConflictResult"/> instance.
        /// </summary>
        public ConflictResult()
            : base(StatusCodes.Status409Conflict)
        {
        }
    }
}
