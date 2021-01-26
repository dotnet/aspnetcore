// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
