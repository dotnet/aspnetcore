// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Filters
{
    /// <summary>
    /// A filter that runs after an action has thrown an <see cref="System.Exception"/>.
    /// </summary>
    public interface IExceptionFilter : IFilterMetadata
    {
        /// <summary>
        /// Called after an action has thrown an <see cref="System.Exception"/>.
        /// </summary>
        /// <param name="context">The <see cref="ExceptionContext"/>.</param>
        void OnException(ExceptionContext context);
    }
}
