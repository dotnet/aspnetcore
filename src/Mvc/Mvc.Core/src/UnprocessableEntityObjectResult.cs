// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// An <see cref="ObjectResult"/> that when executed will produce a Unprocessable Entity (422) response.
/// </summary>
[DefaultStatusCode(DefaultStatusCode)]
public class UnprocessableEntityObjectResult : ObjectResult
{
    private const int DefaultStatusCode = StatusCodes.Status422UnprocessableEntity;

    /// <summary>
    /// Creates a new <see cref="UnprocessableEntityObjectResult"/> instance.
    /// </summary>
    /// <param name="modelState"><see cref="ModelStateDictionary"/> containing the validation errors.</param>
    public UnprocessableEntityObjectResult([ActionResultObjectValue] ModelStateDictionary modelState)
        : this(new SerializableError(modelState))
    {
    }

    /// <summary>
    /// Creates a new <see cref="UnprocessableEntityObjectResult"/> instance.
    /// </summary>
    /// <param name="error">Contains errors to be returned to the client.</param>
    public UnprocessableEntityObjectResult([ActionResultObjectValue] object? error)
        : base(error)
    {
        StatusCode = DefaultStatusCode;
    }
}
