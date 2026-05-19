// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// An <see cref="ObjectResult"/> that when executed will produce a Conflict (409) response.
/// </summary>
[DefaultStatusCode(DefaultStatusCode)]
public class ConflictObjectResult : ObjectResult
{
    private const int DefaultStatusCode = StatusCodes.Status409Conflict;

    /// <summary>
    /// Creates a new <see cref="ConflictObjectResult"/> instance.
    /// </summary>
    /// <param name="error">Contains the errors to be returned to the client.</param>
    public ConflictObjectResult([ActionResultObjectValue] object? error)
        : base(error)
    {
        StatusCode = DefaultStatusCode;
    }

    /// <summary>
    /// Creates a new <see cref="ConflictObjectResult"/> instance.
    /// </summary>
    /// <param name="modelState"><see cref="ModelStateDictionary"/> containing the validation errors.</param>
    public ConflictObjectResult([ActionResultObjectValue] ModelStateDictionary modelState)
        : base(new SerializableError(modelState))
    {
        ArgumentNullException.ThrowIfNull(modelState);

        StatusCode = DefaultStatusCode;
    }
}
