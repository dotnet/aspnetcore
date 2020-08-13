// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// A <see cref="StatusCodeResult"/> that when
    /// executed will produce a UnsupportedMediaType (415) response.
    /// </summary>
    [DefaultStatusCode(DefaultStatusCode)]
    public class UnsupportedMediaTypeResult : StatusCodeResult
    {
        private const int DefaultStatusCode = StatusCodes.Status415UnsupportedMediaType;

        /// <summary>
        /// Creates a new instance of <see cref="UnsupportedMediaTypeResult"/>.
        /// </summary>
        public UnsupportedMediaTypeResult() : base(DefaultStatusCode)
        {
        }
    }
}
