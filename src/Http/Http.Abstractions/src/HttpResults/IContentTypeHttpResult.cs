// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Defines a contract that represents the result of an HTTP endpoint
/// that contains a <see cref="ContentType"/>.
/// </summary>
public interface IContentTypeHttpResult
{
    /// <summary>
    /// Gets the Content-Type header for the response.
    /// </summary>
    string? ContentType { get; }
}
