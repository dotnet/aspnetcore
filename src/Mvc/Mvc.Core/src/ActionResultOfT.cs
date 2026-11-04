// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// A type that wraps either an <typeparamref name="TValue"/> instance or an <see cref="ActionResult"/>.
/// </summary>
/// <typeparam name="TValue">The type of the result.</typeparam>
public sealed class ActionResult<TValue> : IConvertToActionResult
{
    private const int DefaultStatusCode = StatusCodes.Status200OK;

    /// <summary>
    /// Initializes a new instance of <see cref="ActionResult{TValue}"/> using the specified <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    public ActionResult(TValue value)
    {
        if (typeof(IActionResult).IsAssignableFrom(typeof(TValue)) ||
            typeof(IResult).IsAssignableFrom(typeof(TValue)))
        {
            var error = Resources.FormatInvalidTypeTForActionResultOfT(typeof(TValue), "ActionResult<T>");
            throw new ArgumentException(error);
        }

        Value = value;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ActionResult{TValue}"/> using the specified <see cref="ActionResult"/>.
    /// </summary>
    /// <param name="result">The <see cref="ActionResult"/>.</param>
    public ActionResult(ActionResult result)
    {
        if (typeof(IActionResult).IsAssignableFrom(typeof(TValue)) ||
            typeof(IResult).IsAssignableFrom(typeof(TValue)))
        {
            var error = Resources.FormatInvalidTypeTForActionResultOfT(typeof(TValue), "ActionResult<T>");
            throw new ArgumentException(error);
        }

        Result = result ?? throw new ArgumentNullException(nameof(result));
    }

    /// <summary>
    /// Gets the <see cref="ActionResult"/>.
    /// </summary>
    public ActionResult? Result { get; }

    /// <summary>
    /// Gets the value.
    /// </summary>
    public TValue? Value { get; }

    /// <summary>
    /// Implicitly converts the specified <paramref name="value"/> to an <see cref="ActionResult{TValue}"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator ActionResult<TValue>(TValue value)
    {
        return new ActionResult<TValue>(value);
    }

    /// <summary>
    /// Implicitly converts the specified <paramref name="result"/> to an <see cref="ActionResult{TValue}"/>.
    /// </summary>
    /// <param name="result">The <see cref="ActionResult"/>.</param>
    public static implicit operator ActionResult<TValue>(ActionResult result)
    {
        return new ActionResult<TValue>(result);
    }

    IActionResult IConvertToActionResult.Convert()
    {
        if (Result != null)
        {
            return Result;
        }

        int statusCode;
        if (Value is ProblemDetails problemDetails && problemDetails.Status != null)
        {
            statusCode = problemDetails.Status.Value;
        }
        else
        {
            statusCode = DefaultStatusCode;
        }

        return new ObjectResult(Value)
        {
            DeclaredType = typeof(TValue),
            StatusCode = statusCode
        };
    }
}
