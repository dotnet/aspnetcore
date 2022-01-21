// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// A delegate that asynchronously returns an <see cref="ActionExecutedContext"/> indicating the action or the next
/// action filter has executed.
/// </summary>
/// <returns>
/// A <see cref="Task"/> that on completion returns an <see cref="ActionExecutedContext"/>.
/// </returns>
public delegate Task<ActionExecutedContext> ActionExecutionDelegate();
