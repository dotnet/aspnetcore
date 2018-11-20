// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// A function that can process an HTTP request.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> for the request.</param>
    /// <returns>A task that represents the completion of request processing.</returns>
    public delegate Task RequestDelegate(HttpContext context);
}