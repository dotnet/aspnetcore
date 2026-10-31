// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// An <see cref="ObjectResult"/> that when executed will produce a Bad Request (400) response.
/// </summary>
[DefaultStatusCode(DefaultStatusCode)]
public class BadRequestObjectResult : ObjectResult
{
    private const int DefaultStatusCode = StatusCodes.Status400BadRequest;

    /// <summary>
    /// Creates a new <see cref="BadRequestObjectResult"/> instance.
    /// </summary>
    /// <param name="error">Contains the errors to be returned to the client.</param>
    public BadRequestObjectResult([ActionResultObjectValue] object? error)
        : base(error)
    {
        StatusCode = DefaultStatusCode;
    }

    /// <summary>
    /// Creates a new <see cref="BadRequestObjectResult"/> instance.
    /// </summary>
    /// <param name="modelState"><see cref="ModelStateDictionary"/> containing the validation errors.</param>
    public BadRequestObjectResult([ActionResultObjectValue] ModelStateDictionary modelState)
        : base(new SerializableError(modelState))
    {
        ArgumentNullException.ThrowIfNull(modelState);

        StatusCode = DefaultStatusCode;
    }
}
