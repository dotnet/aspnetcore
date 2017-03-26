// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Defines middleware that can be added to the application's request pipeline.
    /// </summary>
    public interface IMiddleware
    {
        /// <summary>
        /// Request handling method.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> for the current request.</param>
        /// <param name="next">The delegate representing the remaining middleware in the request pipeline.</param>
        /// <returns>A <see cref="Task"/> that represents the execution of this middleware.</returns>
        Task InvokeAsync(HttpContext context, RequestDelegate next);
    }
}
