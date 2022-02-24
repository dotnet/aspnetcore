// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

/// <summary>
/// Provides a mapping from the return value of an action to an <see cref="IActionResult"/>
/// for request processing.
/// </summary>
/// <remarks>
/// The default implementation of this service handles the conversion of
/// <see cref="ActionResult{TValue}"/> to an <see cref="IActionResult"/> during request
/// processing as well as the mapping of <see cref="ActionResult{TValue}"/> to <c>TValue</c>
/// during API Explorer processing.
/// </remarks>
public interface IActionResultTypeMapper
{
    /// <summary>
    /// Gets the result data type that corresponds to <paramref name="returnType"/>. This
    /// method will not be called for actions that return <c>void</c> or an <see cref="IActionResult"/>
    /// type.
    /// </summary>
    /// <param name="returnType">The declared return type of an action.</param>
    /// <returns>A <see cref="Type"/> that represents the response data.</returns>
    /// <remarks>
    /// Prior to calling this method, the infrastructure will unwrap <see cref="Task{TResult}"/> or
    /// other task-like types.
    /// </remarks>
    Type GetResultDataType(Type returnType);

    /// <summary>
    /// Converts the result of an action to an <see cref="IActionResult"/> for response processing.
    /// This method will be not be called when a method returns <c>void</c> or an
    /// <see cref="IActionResult"/> value.
    /// </summary>
    /// <param name="value">The action return value. May be <c>null</c>.</param>
    /// <param name="returnType">The declared return type.</param>
    /// <returns>An <see cref="IActionResult"/> for response processing.</returns>
    /// <remarks>
    /// Prior to calling this method, the infrastructure will unwrap <see cref="Task{TResult}"/> or
    /// other task-like types.
    /// </remarks>
    IActionResult Convert(object? value, Type returnType);
}
