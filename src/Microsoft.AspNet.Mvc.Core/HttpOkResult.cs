// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// An <see cref="HttpStatusCodeResult"/> that when executed will produce an empty
    /// <see cref="StatusCodes.Status200OK"/> response.
    /// </summary>
    public class HttpOkResult : HttpStatusCodeResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpOkResult"/> class.
        /// </summary>
        public HttpOkResult()
            : base(StatusCodes.Status200OK)
        {
        }
    }
}