// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Diagnostics;

// FIXME: A link is needed for the reference but adding it breaks Intellisense (see pull/38659)
/// <summary>
/// Provides an extensibility point for changing the behavior of the DeveloperExceptionPageMiddleware.
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
