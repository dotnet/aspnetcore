// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Diagnostics
{
    /// <summary>
    /// A feature containing the path and error of the original request for examination by an exception handler.
    /// </summary>
    public class ExceptionHandlerFeature : IExceptionHandlerPathFeature
    {
        /// <inheritdoc/>
        public Exception Error { get; set; } = default!;

        /// <inheritdoc/>
        public string Path { get; set; } = default!;

        /// <inheritdoc/>
        public Endpoint? Endpoint { get; set; }

        /// <inheritdoc/>
        public RouteValueDictionary? RouteValues { get; set; }
    }
}
