using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Diagnostics
{
    /// <summary>
    /// Provides an extensiblity point for changing the behavior of the <see cref="DeveloperExceptionPageMiddleware"/>.
    /// </summary>
    public interface IDeveloperPageExceptionFilter
    {
        /// <summary>
        /// An exception handling method.
        /// </summary>
        /// <param name="errorContext">The error context</param>
        /// <param name="next">The next filter in the pipeline</param>
        /// <returns>A task the completes when the handler is done executing.</returns>
        Task HandleExceptionAsync(ErrorContext errorContext, Func<ErrorContext, Task> next);
    }
}
