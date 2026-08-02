// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Defines a type that provide functionality to
/// create a <see cref="Mvc.ProblemDetails"/> response.
/// </summary>
public interface IProblemDetailsService
{
    /// <summary>
    /// Write a <see cref="Mvc.ProblemDetails"/> response to the current context,
    /// using the registered <see cref="IProblemDetailsWriter"/> services.
    /// </summary>
    /// <param name="context">The <see cref="ProblemDetailsContext"/> associated with the current request/response.</param>
    /// <remarks>The <see cref="IProblemDetailsWriter"/> registered services
    /// are processed in sequence and the processing is completed when:
    /// <list type="bullet">
    /// <item><description>One of them reports that the response was written successfully, or.</description></item>
    /// <item><description>All <see cref="IProblemDetailsWriter"/> were executed and none of them was able to write the response successfully.</description></item>
    /// </list>
    /// </remarks>
    /// <exception cref="InvalidOperationException">If no <see cref="IProblemDetailsWriter"/> can write to the given context.</exception>
    ValueTask WriteAsync(ProblemDetailsContext context);

    /// <summary>
    /// Try to write a <see cref="Mvc.ProblemDetails"/> response to the current context,
    /// using the registered <see cref="IProblemDetailsWriter"/> services.
    /// </summary>
    /// <param name="context">The <see cref="ProblemDetailsContext"/> associated with the current request/response.</param>
    /// <remarks>The <see cref="IProblemDetailsWriter"/> registered services
    /// are processed in sequence and the processing is completed when:
    /// <list type="bullet">
    /// <item><description>One of them reports that the response was written successfully, or.</description></item>
    /// <item><description>All <see cref="IProblemDetailsWriter"/> were executed and none of them was able to write the response successfully.</description></item>
    /// </list>
    /// </remarks>
    async ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
    {
        await WriteAsync(context);
        return context.HttpContext.Response.HasStarted;
    }
}
