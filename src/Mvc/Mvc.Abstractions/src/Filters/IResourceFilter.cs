// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// A filter that surrounds execution of model binding, the action (and filters) and the action result
/// (and filters).
/// </summary>
public interface IResourceFilter : IFilterMetadata
{
    /// <summary>
    /// Executes the resource filter. Called before execution of the remainder of the pipeline.
    /// </summary>
    /// <param name="context">The <see cref="ResourceExecutingContext"/>.</param>
    void OnResourceExecuting(ResourceExecutingContext context);

    /// <summary>
    /// Executes the resource filter. Called after execution of the remainder of the pipeline.
    /// </summary>
    /// <param name="context">The <see cref="ResourceExecutedContext"/>.</param>
    void OnResourceExecuted(ResourceExecutedContext context);
}
