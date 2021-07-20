// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Diagnostics
{
    /// <summary>
    /// Provides an extensiblity point for changing the behavior of the DeveloperExceptionPageMiddleware.
    /// </summary>
    public interface IDeveloperPageExceptionFilter
    {
        /// <summary>
        /// An exception handling method that is used to either format the exception or delegate to the next handler in the chain.
        /// </summary>
        /// <param name="errorContext">The error context.</param>
        /// <param name="next">The next filter in the pipeline.</param>
        /// <returns>A task the completes when the handler is done executing.</returns>
        Task HandleExceptionAsync(ErrorContext errorContext, Func<ErrorContext, Task> next);
    }
}
