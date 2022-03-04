// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.Filters
{
    /// <summary>
    /// A delegate that asynchronously returns a <see cref="ResourceExecutedContext"/> indicating model binding, the
    /// action, the action's result, result filters, and exception filters have executed.
    /// </summary>
    /// <returns>A <see cref="Task"/> that on completion returns a <see cref="ResourceExecutedContext"/>.</returns>
    public delegate Task<ResourceExecutedContext> ResourceExecutionDelegate();
}