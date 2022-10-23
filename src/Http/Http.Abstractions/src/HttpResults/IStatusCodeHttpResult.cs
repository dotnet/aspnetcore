// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Defines a contract that represents the result of an HTTP endpoint
/// that contains a <see cref="StatusCode"/>.
/// </summary>
public interface IStatusCodeHttpResult
{
    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    int? StatusCode { get; }
}
