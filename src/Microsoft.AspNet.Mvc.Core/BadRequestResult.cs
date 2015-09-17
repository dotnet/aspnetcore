// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// A <see cref="HttpStatusCodeResult"/> that when
    /// executed will produce a Bad Request (400) response.
    /// </summary>
    public class BadRequestResult : HttpStatusCodeResult
    {
        /// <summary>
        /// Creates a new <see cref="BadRequestResult"/> instance.
        /// </summary>
        public BadRequestResult()
            : base(StatusCodes.Status400BadRequest)
        {
        }
    }
}