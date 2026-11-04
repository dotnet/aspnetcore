// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// A delegate that asynchronously returns a <see cref="PageHandlerExecutedContext"/> indicating the page or the next
/// page filter has executed.
/// </summary>
/// <returns>
/// A <see cref="Task"/> that on completion returns an <see cref="PageHandlerExecutedContext"/>.
/// </returns>
public delegate Task<PageHandlerExecutedContext> PageHandlerExecutionDelegate();
