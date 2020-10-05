// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Diagnostics;
#nullable enable

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Options for configuring the <see cref="ExceptionHandlerMiddleware"/>.
    /// </summary>
    public class ExceptionHandlerOptions
    {
        /// <summary>
        /// The path to the exception handling endpoint. This path will be used when executing
        /// the <see cref="ExceptionHandler"/>.
        /// </summary>
        public PathString ExceptionHandlingPath { get; set; }

        /// <summary>
        /// The <see cref="RequestDelegate" /> that will handle the exception. If this is not
        /// explicitly provided, the subsequent middleware pipeline will be used by default.
        /// </summary>
        public RequestDelegate? ExceptionHandler { get; set; }
    }
}
