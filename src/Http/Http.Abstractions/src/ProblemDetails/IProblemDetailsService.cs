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
    /// Write a <see cref="Mvc.ProblemDetails"/> response to the current context
    /// </summary>
    /// <param name="context">The <see cref="ProblemDetailsContext"/> associated with the current request/response.</param>
    ValueTask WriteAsync(ProblemDetailsContext context);
}
