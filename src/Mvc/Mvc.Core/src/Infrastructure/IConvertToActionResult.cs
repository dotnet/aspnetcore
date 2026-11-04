// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

/// <summary>
/// Defines the contract to convert a type to an <see cref="IActionResult"/> during action invocation.
/// </summary>
public interface IConvertToActionResult
{
    /// <summary>
    /// Converts the current instance to an instance of <see cref="IActionResult"/>.
    /// </summary>
    /// <returns>The converted <see cref="IActionResult"/>.</returns>
    IActionResult Convert();
}
