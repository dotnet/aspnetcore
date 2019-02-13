// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.Filters
{
    /// <summary>
    /// A filter that runs asynchronously after an action has thrown an <see cref="System.Exception"/>.
    /// </summary>
    public interface IAsyncExceptionFilter : IFilterMetadata
    {
        /// <summary>
        /// Called after an action has thrown an <see cref="System.Exception"/>.
        /// </summary>
        /// <param name="context">The <see cref="ExceptionContext"/>.</param>
        /// <returns>A <see cref="Task"/> that on completion indicates the filter has executed.</returns>
        Task OnExceptionAsync(ExceptionContext context);
    }
}
