// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Represents an <see cref="HttpUnauthorizedResult"/> that when
    /// executed will produce an Unauthorized (401) response.
    /// </summary>
    public class HttpUnauthorizedResult : HttpStatusCodeResult
    {
        /// <summary>
        /// Creates a new <see cref="HttpUnauthorizedResult"/> instance.
        /// </summary>
        public HttpUnauthorizedResult() : base(StatusCodes.Status401Unauthorized)
        {
        }
    }
}