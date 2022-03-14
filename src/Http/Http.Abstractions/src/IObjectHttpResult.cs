// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Defines a contract that represents the result of an HTTP endpoint
/// that contains an object <see cref="Value"/> and <see cref="StatusCode"/>.
/// </summary>
public interface IObjectHttpResult
{
    /// <summary>
    /// Gets or sets the object result.
    /// </summary>
    object? Value { get; }

    /// <summary>
    /// Gets or sets the HTTP status code.
    /// </summary>
    int? StatusCode { get; }
}
