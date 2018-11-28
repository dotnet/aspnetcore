// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.Filters
{
    /// <summary>
    /// A delegate that asynchronously returns a <see cref="PageHandlerExecutedContext"/> indicating the page or the next
    /// page filter has executed.
    /// </summary>
    /// <returns>
    /// A <see cref="Task"/> that on completion returns an <see cref="PageHandlerExecutedContext"/>.
    /// </returns>
    public delegate Task<PageHandlerExecutedContext> PageHandlerExecutionDelegate();
}
