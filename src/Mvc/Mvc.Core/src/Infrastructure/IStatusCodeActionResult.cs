// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

/// <summary>
/// Represents an <see cref="IActionResult"/> that when executed will
/// produce an HTTP response with the specified <see cref="StatusCode"/>.
/// </summary>
public interface IStatusCodeActionResult : IActionResult
{
    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    int? StatusCode { get; }
}
