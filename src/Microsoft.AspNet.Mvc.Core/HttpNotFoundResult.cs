// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Represents an <see cref="HttpStatusCodeResult"/> that when
    /// executed will produce a Not Found (404) response.
    /// </summary>
    public class HttpNotFoundResult : HttpStatusCodeResult
    {
        /// <summary>
        /// Creates a new <see cref="HttpNotFoundResult"/> instance.
        /// </summary>
        public HttpNotFoundResult() : base(StatusCodes.Status404NotFound)
        {
        }
    }
}